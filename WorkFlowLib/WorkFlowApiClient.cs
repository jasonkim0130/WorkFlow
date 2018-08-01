using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Dreamlab.Core;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using WorkFlow.Service;
using System.Configuration;
using System.Web;

namespace WorkFlowLib
{
    public class WorkFlowApiClient : IDisposable
    {
        public HttpClient Client { get; }
        public int DefaultTryTimes = 1;
        public int DefaultTryTimeInterval = 100;
        public static string Url;

        private void InitUrl()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                Url = ConfigurationManager.AppSettings["WorkFlowApiHost"];
                if (string.IsNullOrWhiteSpace(Url))
                {
                    if (Codehelper.IsUat)
                    {
                        Url = "http://twnapp.blsretail.com:8809/IntranetApiUAT/workflow/";
                    }
                    else
                    {
                        Url = "http://twnapp.blsretail.com:8809/IntranetApi/workflow/";
                    }
                }
            }
        }

        public WorkFlowApiClient()
        {
            InitUrl();
            Client = new HttpClient();
            if (!Client.DefaultRequestHeaders.Any(p => p.Key.EqualsIgnoreCaseAndBlank("clientsecret")))
                Client.DefaultRequestHeaders.Add("clientsecret", Consts.WorkFlowApIToken);
            Client.BaseAddress = new Uri(Url);
            if (!Client.DefaultRequestHeaders.Accept.Any(p => p.MediaType.EqualsIgnoreCaseAndBlank("application/json")))
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string TrySendMaxTimesRequest(string url, Func<HttpResponseMessage> getResponse, Func<object> getParameter)
        {
            string errorResult = null;
            for (int i = 0; i < DefaultTryTimes; i++)
            {
                string str = null;
                try
                {
                    HttpResponseMessage response = getResponse();
                    str = response.Content.ReadAsStringAsync().Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return str;
                    }
                    errorResult = str;
                }
                catch (Exception e)
                {
                    errorResult = e.Message;
                    Singleton<ILogWritter>.Instance?.WriteLog("Invoke workflow api" + url, str ?? "ERROR", JsonConvert.SerializeObject(getParameter()));
                }
                //Thread.Sleep(DefaultTryTimeInterval);
            }
            return errorResult;
        }

        public string PostString(string url, Dictionary<string, string> formData = null)
        {
            return TrySendMaxTimesRequest(url, () => Client.PostAsync(url, new FormUrlEncodedContent(formData ?? new Dictionary<string, string>())).Result, () => formData);
        }

        public string PostString(string url, object jsonObject = null)
        {
            return TrySendMaxTimesRequest(url, () => Client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(jsonObject), Encoding.UTF8, "application/json")).Result, () => jsonObject);
        }

        public string JoinParaString(Dictionary<string, string> items, bool encode = true)
        {
            if (encode)
                return string.Join("&", items.Where(p => p.Value != null).Select(p => p.Key.Trim() + "=" + HttpUtility.UrlEncode(p.Value ?? "").Trim()));
            return string.Join("&", items.Where(p => p.Value != null).Select(p => p.Key.Trim() + "=" + p.Value.Trim()));
        }

        public string GetString(string url, Dictionary<string, string> queryString = null, bool encode = true)
        {
            if (queryString != null && queryString.Count > 0)
            {
                if (!url.EndsWith("?"))
                    url = url + "?" + JoinParaString(queryString, encode);
                else
                    url = url + JoinParaString(queryString, encode);
            }
            string result = TrySendMaxTimesRequest(url, () => Client.GetAsync(url).Result, () => queryString);
            return result;
        }

        public string UploadFile(string orgFileName, string base64String)
        {
            return PostString("operation/upload_file", new
            {
                OriFileName = orgFileName,
                Base64String = base64String
            });
        }

        public string UploadImg(string orgFileName, string base64String)
        {
            return PostString("operation/upload_icon", new
            {
                OriFileName = orgFileName,
                Base64String = base64String
            });
        }

        public string ArchiveApproved(int flowCaseId)
        {
            return PostString("operation/archive_approved", flowCaseId);
        }

        public byte[] GetImageFile(int attachmentId, out string contentType)
        {
            string url = "data/get_image_file?attachmentId=" + attachmentId;
            HttpResponseMessage ret = Client.GetAsync(url).Result;
            if (ret.Content.Headers.ContentType != null)
                contentType = ret.Content.Headers.ContentType.ToString();
            else
                contentType = "application/octet-stream";
            byte[] bytes = ret.Content.ReadAsByteArrayAsync().Result;
            return bytes;
        }

        public byte[] GetAttachmentFile(int attachmentId)
        {
            string url = "data/download_attachment?attachmentId=" + attachmentId;
            return Client.GetAsync(url).Result.Content.ReadAsByteArrayAsync().Result;
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
