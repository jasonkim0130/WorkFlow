using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Dreamlab.Core;

namespace WorkFlowLib
{
    public class HttpClientWrapper : IDisposable
    {
        public HttpClient Client { get; }
        public int DefaultTryTimes = 1;
        public int DefaultTryTimeInterval = 100;

        public HttpClientWrapper(Uri host)
        {
            Client = new HttpClient { BaseAddress = host };
            if (!Client.DefaultRequestHeaders.Accept.Any(p => p.MediaType.EqualsIgnoreCaseAndBlank("application/json")))
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string JoinParaString(Dictionary<string, string> items, bool encode = true)
        {
            if (encode)
                return string.Join("&", items.Where(p => p.Value != null).Select(p => p.Key.Trim() + "=" + Uri.EscapeDataString(p.Value ?? "").Trim()));
            return string.Join("&", items.Where(p => p.Value != null).Select(p => p.Key.Trim() + "=" + p.Value.Trim()));
        }

        public async Task<RequestResult<TResult>> PostJsonAsync<TResult>(string url, Dictionary<string, string> formData = null)
        {
            RequestResult<string> result = await PostStringAsync(url, formData);
            return ConvertToJson<TResult>(result);
        }

        public RequestResult<TResult> PostJson<TResult>(string url, Dictionary<string, string> formData = null)
        {
            RequestResult<string> result = PostString(url, formData);
            return ConvertToJson<TResult>(result);
        }

        public async Task<RequestResult<TResult>> PostJsonAsync<TResult>(string url, object jsonObject)
        {
            RequestResult<string> result = await PostStringAsync(url, jsonObject);
            return ConvertToJson<TResult>(result);
        }

        public RequestResult<TResult> PostJson<TResult>(string url, object jsonObject)
        {
            RequestResult<string> result = PostString(url, jsonObject);
            return ConvertToJson<TResult>(result);
        }

        private async Task<RequestResult<string>> TrySendMaxTimesRequestAsync(string url, string method, HttpContent content, Func<object> getParameter)
        {
            RequestResult<string> errorResult = null;
            for (int i = 0; i < DefaultTryTimes; i++)
            {
                string str = null;
                try
                {
                    HttpResponseMessage response = method.Equals("get") ? await Client.GetAsync(url) : await Client.PostAsync(url, content);
                    str = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return new RequestResult<string>
                        {
                            ReturnValue = str
                        };
                    }
                    errorResult = new RequestResult<string> { ErrorMessage = str + " StatusCode: " + response.StatusCode };
                }
                catch (Exception e)
                {
                    errorResult = new RequestResult<string> { ErrorMessage = e.Message };
                    Singleton<ILogWritter>.Instance?.WriteLog("Invoke api" + url + Client.BaseAddress, str ?? "ERROR", JsonConvert.SerializeObject(getParameter()));
                }
            }
            return errorResult;
        }

        private RequestResult<string> TrySendMaxTimesRequest(string url, string method, HttpContent content, Func<object> getParameter)
        {
            RequestResult<string> errorResult = null;
            for (int i = 0; i < DefaultTryTimes; i++)
            {
                string str = null;
                try
                {
                    HttpResponseMessage response = method.Equals("get") ? Client.GetAsync(url).Result : Client.PostAsync(url, content).Result;
                    str = response.Content.ReadAsStringAsync().Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return new RequestResult<string>
                        {
                            ReturnValue = str
                        };
                    }
                    errorResult = new RequestResult<string> { ErrorMessage = str + " StatusCode: " + response.StatusCode };
                }
                catch (Exception e)
                {
                    errorResult = new RequestResult<string> { ErrorMessage = e.Message };
                    Singleton<ILogWritter>.Instance?.WriteLog("Invoke api" + url + Client.BaseAddress, str ?? "ERROR", JsonConvert.SerializeObject(getParameter()));
                }
            }
            return errorResult;
        }

        public async Task<RequestResult<string>> PostStringAsync(string url, Dictionary<string, string> formData = null)
        {
            return await TrySendMaxTimesRequestAsync(url, "post", new FormUrlEncodedContent(formData ?? new Dictionary<string, string>()), () => formData);
        }

        public RequestResult<string> PostString(string url, Dictionary<string, string> formData = null)
        {
            return TrySendMaxTimesRequest(url, "post", new FormUrlEncodedContent(formData ?? new Dictionary<string, string>()), () => formData);
        }

        public async Task<RequestResult<string>> PostStringAsync(string url, object jsonObject = null)
        {
            return await TrySendMaxTimesRequestAsync(url, "post", new StringContent(JsonConvert.SerializeObject(jsonObject), Encoding.UTF8, "application/json"), () => jsonObject);
        }

        public RequestResult<string> PostString(string url, object jsonObject = null)
        {
            return TrySendMaxTimesRequest(url, "post", new StringContent(JsonConvert.SerializeObject(jsonObject), Encoding.UTF8, "application/json"), () => jsonObject);
        }

        public async Task<RequestResult<TResult>> GetJsonAsync<TResult>(string url, Dictionary<string, string> queryString = null, bool encode = true)
        {
            RequestResult<string> result = await GetStringAsync(url, queryString, encode);
            return ConvertToJson<TResult>(result);
        }

        public RequestResult<TResult> GetJson<TResult>(string url, Dictionary<string, string> queryString = null, bool encode = true)
        {
            RequestResult<string> result = GetString(url, queryString, encode);
            return ConvertToJson<TResult>(result);
        }

        public RequestResult<TResult> ConvertToJson<TResult>(RequestResult<string> source)
        {
            if (!string.IsNullOrWhiteSpace(source.ErrorMessage))
            {
                return new RequestResult<TResult> { ErrorMessage = source.ErrorMessage };
            }
            try
            {
                TResult res = (TResult)JsonConvert.DeserializeObject(source.ReturnValue, typeof(TResult),
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                return new RequestResult<TResult> { ReturnValue = res };
            }
            catch (Exception e)
            {
                return new RequestResult<TResult> { ErrorMessage = e.Message + source.ReturnValue };
            }
        }

        public async Task<RequestResult<string>> GetStringAsync(string url, Dictionary<string, string> queryString = null, bool encode = true)
        {
            if (queryString != null && queryString.Count > 0)
            {
                if (!url.EndsWith("?"))
                    url = url + "?" + JoinParaString(queryString, encode);
                else
                    url = url + JoinParaString(queryString, encode);
            }
            RequestResult<string> result = await TrySendMaxTimesRequestAsync(url, "get", null, () => queryString);
            return result;
        }

        public RequestResult<string> GetString(string url, Dictionary<string, string> queryString = null, bool encode = true)
        {
            if (queryString != null && queryString.Count > 0)
            {
                if (!url.EndsWith("?"))
                    url = url + "?" + JoinParaString(queryString, encode);
                else
                    url = url + JoinParaString(queryString, encode);
            }
            RequestResult<string> result = TrySendMaxTimesRequest(url, "get", null, () => queryString);
            return result;
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }

    public class RequestResult<TResult>
    {
        public TResult ReturnValue { get; set; }
        public string ErrorMessage { get; set; }
    }
}
