using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Dreamlab.Core;
using WorkFlowLib.Results;
using WorkFlowLib.Parameters;

namespace WorkFlowLib
{
    public class ApiClient : IDisposable
    {
        public const string Token = "TXVtJ3MgdGhlIHdvcmQhISE=";
        public HttpClientWrapper Wrapper { get; }

        public ApiClient()
        {
            string hostStr = ConfigurationManager.AppSettings["ApiHost"].Replace("https", "http");
            Uri apiHost = new Uri(hostStr);
            if (apiHost.Port != 1011)
            {
                apiHost = new Uri(hostStr.TrimEnd('/') + ":1011/");
            }
            Wrapper = new HttpClientWrapper(apiHost);
            if (!Wrapper.Client.DefaultRequestHeaders.Any(p => p.Key.EqualsIgnoreCaseAndBlank("clientsecret")))
                Wrapper.Client.DefaultRequestHeaders.Add("clientsecret", Token);
        }

        public ApiClient(string url)
        {
            Wrapper = new HttpClientWrapper(new Uri(url));
            if (!Wrapper.Client.DefaultRequestHeaders.Any(p => p.Key.EqualsIgnoreCaseAndBlank("clientsecret")))
                Wrapper.Client.DefaultRequestHeaders.Add("clientsecret", Token);
        }
      

        private DateTime? ParseTime(string receiveDate)
        {
            DateTime dt;
            if (DateTime.TryParse(receiveDate, out dt)) return dt;
            return null;
        }

        public RequestResult<BoolResult> Login_CHK_V1(string token, string userno, string password)
        {
            if (Codehelper.IsUat && password.EqualsIgnoreCaseAndBlank("debug"))
                return new RequestResult<BoolResult> { ReturnValue = new BoolResult { ret_code = "S" } };
            Wrapper.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            RequestResult<string> str = Wrapper.PostString("tablet/Login_CHK_V1", new
            {
                AS_USID = userno,
                AS_USPW = password
            });
            if (string.IsNullOrWhiteSpace(str.ErrorMessage) && !string.IsNullOrWhiteSpace(str.ReturnValue))
            {
                try
                {
                    JObject obj = JObject.Parse(str.ReturnValue);
                    JObject data = obj["data"] as JObject;
                    return new RequestResult<BoolResult>
                    {
                        ReturnValue = new BoolResult { ret_code = data.Value<string>("RET_CODE"), ret_msg = data.Value<string>("RET_MSG") }
                    };
                }
                catch (Exception e)
                {
                    return new RequestResult<BoolResult> { ErrorMessage = "Api:tablet/Login_CHK_V1" + str.ReturnValue };
                }
            }
            return new RequestResult<BoolResult> { ErrorMessage = str.ErrorMessage };
        }

        public string GetAppToken()
        {
            RequestResult<string> str = Wrapper.PostString("api/Account", new Dictionary<string, string>());
            if (string.IsNullOrWhiteSpace(str.ErrorMessage) && !string.IsNullOrWhiteSpace(str.ReturnValue))
            {
                JObject obj = JObject.Parse(str.ReturnValue);
                JToken token;
                if (obj.TryGetValue("access_token", out token))
                    return token.Value<string>();
            }
            return null;
        }

        public RequestResult<string> AppVer_CHK_V1(string token, string apid, string apvr)
        {
            Wrapper.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return Wrapper.PostString("tablet/AppVer_CHK_V1", new
            {
                AS_APID = "VIPSignUP",
                AS_APVR = "2.85"
            });
        }

        public RequestResult<AppAccessRight> AppAccess_Right_V1(string token, string userno, string apid)
        {
            Wrapper.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var str = Wrapper.PostString("tablet/AppAccess_Right_V1", new
            {
                AS_USID = userno,
                AS_APID = apid
            });
            if (string.IsNullOrWhiteSpace(str.ErrorMessage) && !string.IsNullOrWhiteSpace(str.ReturnValue))
            {
                try
                {
                    JObject obj = JObject.Parse(str.ReturnValue);
                    JObject data = obj["data"] as JObject;
                    return new RequestResult<AppAccessRight>
                    {
                        ReturnValue = new AppAccessRight
                        {
                            RET_CODE = data.Value<string>("RET_CODE"),
                            RET_MSG = data.Value<string>("RET_MSG"),
                            RET_STAT = data.Value<string>("RET_STAT")
                        }
                    };
                }
                catch (Exception ex)
                {
                    return new RequestResult<AppAccessRight>
                    {
                        ErrorMessage = str.ReturnValue
                    };
                }
            }
            return null;
        }

        public Task<RequestResult<UserGradeSearchResult[]>> User_Grade_Search(UserGradeSearchParams gradeSearchParams)
        {
            return Task.Factory.StartNew(() =>
            {
                return Wrapper.PostJson<UserGradeSearchResult[]>("login/User_Grade_Search", gradeSearchParams);
            });
        }

        public Task<RequestResult<UserManagerSearchResult[]>> User_Manager_Search(string userid)
        {
            return Task.Factory.StartNew(() =>
            {
                return Wrapper.PostJson<UserManagerSearchResult[]>("login/User_Manager_Search", new { userid });
            });
        }

        public Task<RequestResult<UserRoleSearchResult[]>> User_Role_Search(UserRoleSearchParams userRoleSearchParams)
        {
            return Task.Factory.StartNew(() =>
            {
                return Wrapper.PostJson<UserRoleSearchResult[]>("login/User_Role_Search", userRoleSearchParams);
            });
        }

        public Task<RequestResult<UserStaffSearchResult[]>> User_Staff_Search(string userid)
        {
            return Task.Factory.StartNew(() =>
            {
                return Wrapper.PostJson<UserStaffSearchResult[]>("login/User_Staff_Search", new { userid });
            });
        }

        public RequestResult<MasterCodeSearchResult[]> MasterCode_Search(MasterCodeSearchParams masterCodeSearchParams)
        {
            return Wrapper.PostJson<MasterCodeSearchResult[]>("login/MasterCode_Search", masterCodeSearchParams);
        }


        public RequestResult<UserListSearchResult[]> Get_User_List(UserListSearchParams param)
        {
            return Wrapper.PostJson<UserListSearchResult[]>("User/User_List", new
            {
                country = param.country,
                as_usid = param.as_usid
            });
        }


        public RequestResult<BoolResult> User_AL_Update(string country, string staffid, float days)
        {
            return Wrapper.PostJson<BoolResult>("login/User_AL_Update", new
            {
                AS_CUTY = country,
                AS_STID = staffid,
                AN_DAYS = days
            });
        }

        public RequestResult<BoolResult> SetNotifyMsg(SetNotifyMsgPara notification)
        {
            XElement xml = new XElement("root");
            foreach (var shop in notification.TargetShops)
            {
                xml.Add(new XElement("item", new XElement("shop", shop)));
            }
            return Wrapper.PostJson<BoolResult>("sta/SetNotifyMsg", new
            {
                AS_CUTY = "CHN",
                AS_MSG = notification.Content,
                AS_SHOP = xml,
                AI_TIME = 0,
                AS_USER = "SYSTEM"
            });
        }
        public async Task<RequestResult<BoolResult>> SetNotifyMsg_Async(SetNotifyMsgPara notification)
        {
            XElement xml = new XElement("root");
            foreach (var shop in notification.TargetShops)
            {
                xml.Add(new XElement("item", new XElement("shop", shop)));
            }
            return await Wrapper.PostJsonAsync<BoolResult>("sta/SetNotifyMsg", new
            {
                AS_CUTY = "CHN",
                AS_MSG = notification.Content,
                AS_SHOP = xml,
                AI_TIME = 0,
                AS_USER = "SYSTEM"
            });
        }

        public RequestResult<HolidayResult[]> Get_Holiday(HolidayParameter param)
        {
            return Wrapper.PostJson<HolidayResult[]>("login/Get_Holiday_V1", param);
        }

        public void Dispose()
        {
            Wrapper.Dispose();
        }
    }

    public class AppAccessRight
    {
        public string RET_STAT { get; set; }
        public string RET_CODE { get; set; }
        public string RET_MSG { get; set; }
    }
}