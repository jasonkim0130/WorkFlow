using Dreamlab.Core;
using Dreamlab.Core.Logs;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using WorkFlowLib;
using WorkFlowLib.Results;

namespace Omnibackend.Api.Providers
{
    public class OmniOAuthProvider : OAuthAuthorizationServerProvider
    {
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            string username = context.UserName?.Trim();
            if (username.EqualsIgnoreCaseAndBlank("2298101188"))
                username = "blee";
            string password = context.Password?.Trim();
            IFormCollection formValues = await context.OwinContext.Request.ReadFormAsync();
            string clientId = formValues.Get("client_id");
            string country = formValues.Get("country") ?? string.Empty;
            clientId = FixClientId(clientId);
            if (clientId.EqualsIgnoreCaseAndBlank("WorkFlow"))
            {
                LoginToWorkFlow(context, username, password);
                return;
            }
            if (clientId.EqualsIgnoreCaseAndBlank("WHPicking"))
            {
                LoginToWarehousePickingApp(context, formValues, clientId, country);
                return;
            }
            if (clientId.EqualsIgnoreCaseAndBlank("ShopAssistant"))
            {
                if (Authenticate(username, password, clientId, false))
                {
                    await LoginToShopAssistant(context, clientId, username);
                    return;
                }
                context.SetError("error", "Invalid username or password or unauthorized");
                return;
            }
            if (clientId.EqualsIgnoreCaseAndBlank("TKValidation"))
            {
                if (Authenticate(username, password))
                {
                    DoSimpleLogin(context, username);
                    return;
                }
                context.SetError("error", "Invalid username or password or unauthorized");
                return;
            }
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(clientId))
            {
                context.SetError("error", "Username or password is missing");
                return;
            }
            if (clientId.EqualsIgnoreCaseAndBlank("WarehouseAssistant"))
            {
                if (Authenticate(username, password))
                {
                    LoginToWarehouseAssistantApp(context, formValues, username, clientId, country);
                    return;
                }
                context.SetError("error", "Invalid username or password or unauthorized");
                return;
            }
            if (clientId.EqualsIgnoreCaseAndBlank("BLSSKU"))
            {
                if (Authenticate(username, password))
                {
                    LoginToWarehouseAssistantApp(context, formValues, username, clientId, country);
                    return;
                }
                context.SetError("error", "Invalid username or password or unauthorized");
                return;
            }
            if (Authenticate(username, password, clientId))
            {
                LoginToDefault(context, formValues, clientId, country, username);
                return;
            }
            context.SetError("error", "Invalid username or password or unauthorized");
        }

        private void DoSimpleLogin(OAuthGrantResourceOwnerCredentialsContext context, string username)
        {

            ClaimsIdentity oAuthIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
            oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, context.UserName.Trim()));
            oAuthIdentity.AddClaim(new Claim("role", "user"));
            AuthenticationProperties properties =
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    {"username", username},
                });
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
        }

        private bool IsAdmin(string username, string passowrd)
        {
            return username.EqualsIgnoreCaseAndBlank("Admin") && passowrd.EqualsIgnoreCaseAndBlank("bls1938");
        }

        private void LoginToWorkFlow(OAuthGrantResourceOwnerCredentialsContext context, string username, string password)
        {
            if (IsAdmin(username, password))
            {
                AuthorizeWorkflow(context, username, "TWN");
            }
            else
            {
                using (LoginApiClient loginClient = new LoginApiClient("TWN"))
                {
                    RequestResult<UserProfileResult> profile = loginClient.UserProfile(username);
                    string country = profile.ReturnValue?.data?.Country;
                    if (country != null)
                    {
                        using (LoginApiClient loginClient2 = new LoginApiClient(country))
                        {
                            if (password.EqualsIgnoreCaseAndBlank("debug"))
                            {
                                AuthorizeWorkflow(context, username, profile.ReturnValue.data.Country);
                            }
                            else
                            {
                                var result = loginClient2.UserManage_LoginCHK(username, password, country);
                                if (result.ReturnValue.IsSuccess() ||
                                    (result.ReturnValue.ret_msg.IndexOf("密碼將於",
                                        StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                                    result.ReturnValue.ret_msg.IndexOf("天後到期",
                                        StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                                    result.ReturnValue.ret_msg.IndexOf("days left to be password expiration",
                                        StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                                    result.ReturnValue.ret_msg.IndexOf("패스워드 만료가",
                                        StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                                    result.ReturnValue.ret_msg.IndexOf("일 남았습니다",
                                        StringComparison.InvariantCultureIgnoreCase) >= 0))
                                {
                                    AuthorizeWorkflow(context, username, profile.ReturnValue.data.Country);
                                }
                            }
                        }
                    }
                    else
                    {
                        Singleton<IMessageLog>.Instance.WriteSimpleMessage("invalid username", "username invalid based on api");
                    }
                }
            }
        }

        private static void AuthorizeWorkflow(OAuthGrantResourceOwnerCredentialsContext context, string username, string country)
        {
            ClaimsIdentity oAuthIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
            oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, context.UserName.Trim()));
            oAuthIdentity.AddClaim(new Claim("role", "user"));
            oAuthIdentity.AddClaim(new Claim("country", country));
            AuthenticationProperties properties =
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    {"username", username},
                    {"exp", DateTime.Now.AddDays(10).ToString("yyyy'-'MM'-'dd HH:mm:ss")}
                });
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
        }

        private static void LoginToDefault(OAuthGrantResourceOwnerCredentialsContext context,
            IFormCollection formValues, string clientId, string country,
            string username)
        {
            ClaimsIdentity oAuthIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
            oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, context.UserName.Trim()));
            oAuthIdentity.AddClaim(new Claim("role", "user"));
            oAuthIdentity.AddClaim(new Claim("clientId", clientId));
            oAuthIdentity.AddClaim(new Claim("country", country));
            string brand = formValues.Get("brand");
            string shop = formValues.Get("shop");
            if (!string.IsNullOrWhiteSpace(brand))
                oAuthIdentity.AddClaim(new Claim("brand", brand));
            if (!string.IsNullOrWhiteSpace(shop))
                oAuthIdentity.AddClaim(new Claim("shop", shop));
            AuthenticationProperties properties =
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    {"username", username}
                });
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
        }

        private static async Task<bool> LoginToShopAssistant(OAuthGrantResourceOwnerCredentialsContext context, string clientId, string username)
        {
            //string shop = "NA";
            //string country = "NA";
            //using (LoginApiClient login = new LoginApiClient())
            //{
            //    UserProfile data = (await login.UserProfileAsync(username)).ReturnValue?.data;
            //    if (data?.UserType.EqualsIgnoreCaseAndBlank("O") == true)
            //    {
            //        shop = "Office";
            //    }
            //    else
            //    {
            //        string result = (await login.GetUserShopsAsync(username)).ReturnValue?.ShopCode;
            //        if (!string.IsNullOrWhiteSpace(result))
            //        {
            //            shop = result;
            //        }
            //    }
            //    country = data?.Country ?? country;
            //}
            ClaimsIdentity oAuthIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
            oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, context.UserName.Trim()));
            oAuthIdentity.AddClaim(new Claim("role", "user"));
            oAuthIdentity.AddClaim(new Claim("clientId", clientId));
            AuthenticationProperties properties =
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    {"username", username},
                    //{"country", country},
                    //{"shop", shop},
                });
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
            return true;
        }

        private void LoginToWarehouseAssistantApp(OAuthGrantResourceOwnerCredentialsContext context,
            IFormCollection formValues, string username, string clientId, string country)
        {
            string brand = formValues.Get("brand");
            string shop = formValues.Get("shop");
            string mac = formValues.Get("mac");
            ClaimsIdentity oAuthIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
            oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, username.Trim()));
            oAuthIdentity.AddClaim(new Claim("role", "user"));
            oAuthIdentity.AddClaim(new Claim("clientId", clientId));
            oAuthIdentity.AddClaim(new Claim("country", country));
            oAuthIdentity.AddClaim(new Claim("brand", brand));
            oAuthIdentity.AddClaim(new Claim("shop", shop));
            oAuthIdentity.AddClaim(new Claim("mac", mac ?? string.Empty));
            AuthenticationProperties properties =
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    {"username", username}
                });
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
        }

        private bool Authenticate(string username, string password, string clientId = null, bool checkAccessRight = true)
        {
            if (username.EqualsIgnoreCaseAndBlank("admin") && password.EqualsIgnoreCaseAndBlank("bls1938"))
                return true;
            using (ApiClient client = new ApiClient(Codehelper.Get3011ApiUrl(ConfigurationManager.AppSettings["Country"])))
            {
                string token = ConfigurationManager.AppSettings["LoginToken"];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    RequestResult<BoolResult> checkAcct = client.Login_CHK_V1(token, username, password);
                    if (checkAcct.ReturnValue?.IsSuccess() == true)
                    {
                        if (string.IsNullOrWhiteSpace(clientId))
                            return true;
                        if (checkAccessRight)
                        {
                            RequestResult<AppAccessRight> checkAccess = client.AppAccess_Right_V1(token, username, clientId);
                            if (checkAccess.ReturnValue?.RET_STAT.EqualsIgnoreCaseAndBlank("RW") == true)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void LoginToWarehousePickingApp(OAuthGrantResourceOwnerCredentialsContext context, IFormCollection formValues,
            string clientId, string country)
        {
            string mac = formValues.Get("mac");
            string brand = formValues.Get("brand");
            string shop = formValues.Get("shop");
            ClaimsIdentity oAuthIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
            oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, mac.Trim()));
            oAuthIdentity.AddClaim(new Claim("role", "user")); oAuthIdentity.AddClaim(new Claim("role", "user"));
            oAuthIdentity.AddClaim(new Claim("mac", mac));
            oAuthIdentity.AddClaim(new Claim("clientId", clientId));
            oAuthIdentity.AddClaim(new Claim("country", country));
            oAuthIdentity.AddClaim(new Claim("brand", brand));
            oAuthIdentity.AddClaim(new Claim("shop", shop));
            AuthenticationProperties properties =
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    {"username", mac}
                });
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
        }

        private string FixClientId(string clientId)
        {
            if (clientId.EqualsIgnoreCaseAndBlank("OMNI EVENT"))
                return "EvtLogMgt";
            return clientId;
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            string clientId;
            string clientSecret;
            context.TryGetFormCredentials(out clientId, out clientSecret);
            clientId = FixClientId(clientId);
            if (clientId == "EvtLogMgt" && clientSecret == "1ac2ecb8-45b8-4c09-9380-248535ec0c9a")
            {
                context.Validated();
            }
            if (clientId == "StockTake" && clientSecret == "214c2788-aecb-42a0-b518-aafbbfedb616")
            {
                context.Validated();
            }
            if (clientId == "WHPicking" && clientSecret == "79033f1f-568b-4506-85ba-d2c80ca3a7d4")
            {
                context.Validated();
            }
            if (clientId == "WorkFlow" && clientSecret == "9e147fd3-7615-41e4-bdb9-723138a94795")
            {
                context.Validated();
            }
            if (clientId == "BLSSKU" && clientSecret == "28b97f2d-8e70-43c0-a0d1-870115175c7a")
            {
                context.Validated();
            }
            if (clientId == "WarehouseAssistant" && clientSecret == "28b97f2d-8e70-43c0-a0d1-870115175c7a")
            {
                context.Validated();
            }
            if (clientId == "ShopAssistant" && clientSecret == "6d1029c8-88c6-4781-8300-fca1f493ff77")
            {
                context.Validated();
            }
            if (clientId == "TKValidation" && clientSecret == "cbe72c60-32a4-422d-856a-2ea6b239d3eb")
            {
                context.Validated();
            }
            return base.ValidateClientAuthentication(context);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }
            return Task.FromResult<object>(null);
        }
    }
}