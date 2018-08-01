using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Dreamlab.Core;
using Resources;
using WorkFlow.Commands;
using WorkFlow.Ext;
using WorkFlow.Logic;
using WorkFlow.Models;
using WorkFlowLib;
using WorkFlowLib.DTO;
using WorkFlowLib.Results;

namespace WorkFlow.Controllers
{
    [Authorize]
    public class AccountController : AdapterController
    {

        [AllowAnonymous]
        public ActionResult LogOn(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                LoginProfile item = LoginProfile.Parse(token);
                if (item != null)
                {
                    LoginApiClient login = new LoginApiClient();
                    using (login.Wrapper)
                    {
                        UserProfile profile = login.UserProfile(item.Username).ReturnValue?.data;
                        if (item.Username.EqualsIgnoreCaseAndBlank("admin") || profile != null && profile.Authority?.Any(p => p.EqualsIgnoreCaseAndBlank(item.Country)) == true)
                        {
                            CmdResult res = UpdateUsername(item.Username, profile?.UserName).Result;
                            RequestResult<string[]> result = GetAccessableBrands(item.Username);
                            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                            {
                                ModelState.AddModelError("", result.ErrorMessage);
                            }
                            else
                            {
                                FormsAuthenticationHelper.SetAuthCookie(item.Username.Trim(), false, string.Join(",", result.ReturnValue));
                                return RedirectToAction("Index", "Home", new { lang = item.Lang });
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", $"You are not allowed to visit {item.Country}'s intranet");
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("", StringResource.INVALID_USERNAME_OR_PASSWORD);
                }
            }
            Response.Buffer = true;
            Response.ExpiresAbsolute = DateTime.Now.AddDays(-1);
            Response.Cache.SetExpires(DateTime.Now.AddDays(-1));
            Response.Expires = 0;
            Response.CacheControl = "no-cache";
            Response.Cache.SetNoStore();
            return View();
        }

        public ActionResult SwitchSite(string country, string language)
        {
            ViewBag.country = country;
            ViewBag.language = language;
            return View();
        }

        [HttpPost]
        public ActionResult GotoNewSite(string country, string language)
        {
            FormsAuthentication.SignOut();
            return Redirect(new LoginProfile(User.Identity.Name, country, language).GetCountrySiteUrl());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LogOn(LogOnViewModel user)
        {
            if (ModelState.IsValid)
            {
                UserLoginProfile profile = await LoginManager.Authenticate(user, HttpContext.IsDebuggingEnabled);
                if (profile != null)
                {
                    bool confirm = false;
                    if (!string.IsNullOrEmpty(profile.error))
                    {
                        if (profile.error.IndexOf("패스워드가 만료 되었습니다", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                              profile.error.IndexOf("密碼已經過期", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                              profile.error.IndexOf("password has been expired", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                          profile.error.IndexOf("密码已经过期", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            return View("ChangePassword", (object)user.Username);
                        }

                        if (profile.error.IndexOf("密碼將於", StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                            profile.error.IndexOf("天後到期", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                            profile.error.IndexOf("days left to be password expiration",
                                StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                            profile.error.IndexOf("패스워드 만료가", StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                            profile.error.IndexOf("일 남았습니다", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            confirm = true;
                        }

                        if (!confirm)
                        {
                            ModelState.AddModelError("", profile.error);
                            return View(user);
                        }
                    }
                    await UpdateUsername(user.Username, profile.UserName);
                    RequestResult<string[]> result = GetAccessableBrands(user.Username);
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        ModelState.AddModelError("", result.ErrorMessage);
                        return View(user);
                    }
                    string lang = Codehelper.GetLang(profile.Language);
                    if (HttpContext.IsDebuggingEnabled)
                    {
                        FormsAuthenticationHelper.SetAuthCookie(user.Username, false, string.Join(",", result.ReturnValue));
                        return RedirectToAction("Index", "Home", new { lang });
                    }
                    FormsAuthenticationHelper.SetAuthCookie(user.Username, false, string.Join(",", result.ReturnValue));
                    if (confirm)
                    {
                        ViewBag.Msg = profile.error;
                        ViewBag.Country = profile.Country;
                        ViewBag.Language = lang;
                        return View("ConfirmChangePassword");
                    }
                    if (!Codehelper.DefaultCountry.EqualsIgnoreCaseAndBlank(profile.Country))
                    {
                        return RedirectToAction("SwitchSite", new { country = profile.Country, language = lang });
                    }
                    return RedirectToAction("Index", "Home", new { lang });
                }
            }
            ModelState.AddModelError("", StringResource.INVALID_USERNAME_OR_PASSWORD);
            return View(user);
        }

        public ActionResult ConfirmChangePassword(string country, string lang, bool change)
        {
            if (change)
            {
                return View("ChangePassword", (object)Username);
            }
            if (!Codehelper.DefaultCountry.EqualsIgnoreCaseAndBlank(country))
            {
                return RedirectToAction("SwitchSite", new { country = country, language = lang });
            }
            return RedirectToAction("Index", "Home", new { lang });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            HttpCookie cookie = new HttpCookie("settingauth", "")
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(-5)
            };
            HttpCookie cafebrand = new HttpCookie("cafebrand", "")
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(-5)
            };
            Response.Cookies.Add(cookie);
            Response.Cookies.Add(cafebrand);
            return RedirectToAction("LogOn", "Account", new { lang = Codehelper.GetLang(Codehelper.DefaultCountry) });
        }

        [HttpGet]
        public ActionResult LoginAsOther()
        {
            if (!Request.IsLocal)
                return Content("Invalid");
            return this.ModalView("_SelectOtherUser");
        }

        [HttpPost]
        public ActionResult LoginAsOther(string user)
        {
            if (!Request.IsLocal)
                return Content("Invalid");
            RequestResult<string[]> result = GetAccessableBrands(user);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                ModelState.AddModelError("", result.ErrorMessage);
                return View(user);
            }
            FormsAuthenticationHelper.SetAuthCookie(user, false, string.Join(",", result.ReturnValue));
            return this.CloseModalView();
        }

        public RequestResult<string[]> GetAccessableBrands(string username)
        {
            //if (HttpContext.IsDebuggingEnabled)
            //{
            //    return new RequestResult<string[]> { ReturnValue = new[] { "HCT" } };
            //}
            RequestResult<string[]> result = new RequestResult<string[]>();
            if (username.EqualsIgnoreCaseAndBlank("admin"))
            {
                result.ReturnValue = BrandSetting.GetBrands(Codehelper.DefaultCountry);
            }
            else
            {
                LoginApiClient login = new LoginApiClient();
                result = login.GetUserBrand(username, Codehelper.DefaultCountry);
            }
            return result;
        }

        public Task<CmdResult> UpdateUsername(string userNo, string username = null)
        {
            if (!userNo.EqualsIgnoreCaseAndBlank("admin"))
            {
                return new UpdateUsernameCmd(DbRepository)
                {
                    Parameter = new UserPara
                    {
                        UserNo = userNo,
                        Username = username
                    }
                }.ExecuteAsync();
            }
            return Task.FromResult<CmdResult>(null);
        }
        [AllowAnonymous]
        public async Task<ActionResult> ChangePassword(string UserId, string Password, string NewPassword, string ConfirmPassword)
        {
            if (User.Identity.Name == "Admin")
            {
                ModelState.AddModelError("", (string)"Admin cannot change password");
                return View("ChangePassword", (object)UserId);
            }
            if (NewPassword == ConfirmPassword)
            {
                LoginApiClient login = new LoginApiClient();
                UserStaffInfo userInfo = WFUtilities.GetUserStaffInfo(UserId);
                RequestResult<BoolResult> res =
                    await login.ChangeUserPasswordAsync(UserId, Password, NewPassword, userInfo.Country);
                if (!string.IsNullOrEmpty(res.ReturnValue.ret_msg))
                {
                    ModelState.AddModelError("", res.ReturnValue.ret_msg);
                    return View("ChangePassword", (object)UserId);
                }
                return RedirectToAction("LogOn");
            }
            ModelState.AddModelError("", StringResource.PASSWORD_INCONSISTENT);
            return View("ChangePassword", (object)UserId);
        }
    }
}