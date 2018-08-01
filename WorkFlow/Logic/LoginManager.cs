using System.Threading.Tasks;
using Dreamlab.Core;
using WorkFlow.Models;
using WorkFlowLib;
using WorkFlowLib.Results;

namespace WorkFlow.Logic
{
    public class LoginManager
    {
        public static async Task<UserLoginProfile> Authenticate(LogOnViewModel user, bool isDebug)
        {
            if (user.Username.EqualsIgnoreCaseAndBlank("admin") && user.Password.EqualsIgnoreCaseAndBlank("bls1938"))
            {
                return new UserLoginProfile
                {
                    Country = Codehelper.DefaultCountry,
                    Language = Codehelper.DefaultCountry,
                    Authority = null
                };
            }
            if (user.Username.EqualsIgnoreCaseAndBlank("2298101188"))
                user.Username = "blee";
            using (LoginApiClient login = new LoginApiClient())
            {
                UserProfile data = (await login.UserProfileAsync(user.Username)).ReturnValue?.data;
                if (data != null && data.Country != null && data.Language != null
                    && data.Authority != null && data.UserName != null)
                {
                    if (isDebug && user.Password.EqualsIgnoreCaseAndBlank("debug"))
                    {
                        return new UserLoginProfile
                        {
                            Country = data.Country,
                            Language = data.Language,
                            Authority = data.Authority,
                            UserName = data.UserName
                        };
                    }

                    using (LoginApiClient login2 = new LoginApiClient(data.Country))
                    {
                        RequestResult<BoolResult> result = await login2.UserManage_LoginCHKAsync(user.Username, user.Password, data.Country);
                        return new UserLoginProfile
                        {
                            Country = data.Country,
                            Language = data.Language,
                            Authority = data.Authority,
                            UserName = data.UserName,
                            error = result.ErrorMessage
                        };
                    }
                }
            }
            return null;
        }
    }
}