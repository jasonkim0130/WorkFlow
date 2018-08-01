using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Dreamlab.Core;
using WorkFlowLib;
using WorkFlowLib.Results;
using WorkFlowLib.Parameters;

namespace WorkFlowLib
{
    public class LoginApiClient : IDisposable
    {
        public HttpClientWrapper Wrapper { get; }

        public LoginApiClient()
        {
            Wrapper = new HttpClientWrapper(new Uri($"{ConfigurationManager.AppSettings["ApiHost"].TrimEnd('/')}:3011/"));
        }

        public LoginApiClient(string country)
        {
            Wrapper = new HttpClientWrapper(new Uri(Codehelper.Get3011ApiUrl(country)));
        }

        public RequestResult<UserProfileResult> UserProfile(string id)
        {
            return Wrapper.PostJson<UserProfileResult>("User/UserProfile", new { AS_USID = id });
        }

        public async Task<RequestResult<UserProfileResult>> UserProfileAsync(string id)
        {
            return await Wrapper.PostJsonAsync<UserProfileResult>("User/UserProfile", new { AS_USID = id });
        }

        public async Task<RequestResult<MenuResult>> GetMenusAsync(MenuParams mennParams, string lang)
        {
            switch (lang)
            {
                case "EN":
                    mennParams.AS_LANG = "ENG";
                    break;
                case "TC":
                    mennParams.AS_LANG = "TWN";
                    break;
                case "SC":
                    mennParams.AS_LANG = "CHN";
                    break;
                case "KO":
                    mennParams.AS_LANG = "KOR";
                    break;
            }
            return await Wrapper.PostJsonAsync<MenuResult>("User/Menu", mennParams);
        }

        public async Task<RequestResult<BoolResult>> ChangeUserPasswordAsync(string userId, string password, string newPassword, string country)
        {
            var res = await Wrapper.PostStringAsync("User/UserManage_Update_PWD", new
            {
                COUNTRY = country,
                AS_USID = userId,
                AS_USPW_OLD = password,
                AS_USPW_NEW = newPassword,
                AS_LANG = "TWN"
            });
            JObject job = JObject.Parse(res.ReturnValue);
            JObject data = (JObject)(job.GetValue("data"));
            return new RequestResult<BoolResult>
            {
                ReturnValue = new BoolResult { ret_code = data.Value<string>("RET_CODE"), ret_msg = data.Value<string>("RET_MSG") }
            };
        }

        public RequestResult<string[]> GetUserBrand(string username, string country)
        {
            var res = Wrapper.PostString("User/UserBrand", new
            {
                AS_USID = username,
                AS_COUN = country
            });
            if (string.IsNullOrEmpty(res.ReturnValue))
            {
                return new RequestResult<string[]>
                {
                    ErrorMessage = "Api返回的brand为空"
                };
            }
            JObject job = JObject.Parse(res.ReturnValue);
            JArray jArray = (JArray)job.GetValue("data");
            List<string> brandsList = new List<string>();
            for (int i = 0; i < jArray.Count; i++)
            {
                JObject jItem = (JObject)jArray[i];
                brandsList.Add(jItem.Value<string>("Brand"));
            }
            return new RequestResult<string[]>
            {
                ReturnValue = brandsList.ToArray()
            };
        }

        public RequestResult<BoolResult> UserManage_LoginCHK(string userid, string password)
        {
            RequestResult<UserProfileResult> profile = UserProfile(userid);
            if (!string.IsNullOrWhiteSpace(profile.ErrorMessage))
            {
                return new RequestResult<BoolResult> { ErrorMessage = profile.ErrorMessage };
            }
            string uri = Codehelper.Get3011ApiUrl(profile.ReturnValue.data.Country);
            if (uri.EqualsIgnoreCaseAndBlank(this.Wrapper.Client.BaseAddress.ToString()))
                return UserManage_LoginCHK(userid, password, profile.ReturnValue.data.Country);
            else
            {
                using (LoginApiClient client2 = new LoginApiClient(profile.ReturnValue.data.Country))
                {
                    return client2.UserManage_LoginCHK(userid, password, profile.ReturnValue.data.Country);
                }
            }
        }
        public RequestResult<BoolResult> UserManage_LoginCHK(string userid, string password, string country)
        {
            RequestResult<string> str = Wrapper.PostString("User/UserManage_LoginCHK", new
            {
                COUNTRY = country,
                AS_USID = userid,
                AS_USPW = password,
                AS_LANG = "ENG",
                AS_CPCD = "000"
            });
            if (!string.IsNullOrWhiteSpace(str.ErrorMessage))
            {
                return new RequestResult<BoolResult> { ErrorMessage = str.ErrorMessage };
            }
            try
            {
                JObject obj = JObject.Parse(str.ReturnValue);
                string msg = ((JObject)obj["data"]).Value<string>("RET_MSG");
                return new RequestResult<BoolResult>
                {
                    ErrorMessage = msg,
                    ReturnValue = new BoolResult
                    {
                        ret_msg = msg,
                        ret_code = ((JObject)obj["data"]).Value<string>("RET_CODE")
                    }
                };
            }
            catch (Exception e)
            {
                return new RequestResult<BoolResult> { ErrorMessage = e.Message };
            }
        }

        public async Task<RequestResult<BoolResult>> UserManage_LoginCHKAsync(string userid, string password, string country)
        {
            RequestResult<string> str = await Wrapper.PostStringAsync("User/UserManage_LoginCHK", new
            {
                COUNTRY = country,
                AS_USID = userid,
                AS_USPW = password,
                AS_LANG = "ENG",
                AS_CPCD = "000"
            });
            if (!string.IsNullOrWhiteSpace(str.ErrorMessage))
            {
                return new RequestResult<BoolResult> { ErrorMessage = str.ErrorMessage };
            }
            try
            {
                JObject obj = JObject.Parse(str.ReturnValue);
                string msg = ((JObject)obj["data"]).Value<string>("RET_MSG");
                return new RequestResult<BoolResult>
                {
                    ErrorMessage = msg,
                    ReturnValue = new BoolResult
                    {
                        ret_msg = msg,
                        ret_code = ((JObject)obj["data"]).Value<string>("RET_CODE")
                    }
                };
            }
            catch (Exception e)
            {
                return new RequestResult<BoolResult> { ErrorMessage = e.Message };
            }
        }

        public RequestResult<string> UserEmail(string id, string country)
        {
            UserEmailParams p = new UserEmailParams
            {
                COUNTRY = country,
                AS_USID = id
            };
            RequestResult<string> str = Wrapper.PostString("User/UserManage_List", p);
            try
            {
                JObject obj = JObject.Parse(str.ReturnValue);
                string email = obj["data"][0].Value<string>("EMAIL");
                return new RequestResult<string>
                {
                    ReturnValue = email,
                    ErrorMessage = null
                };
            }
            catch (Exception e)
            {
                return new RequestResult<string>
                {
                    ReturnValue = null,
                    ErrorMessage = e.Message
                };
            }
        }


        public void Dispose()
        {
            Wrapper?.Dispose();
        }
    }
}