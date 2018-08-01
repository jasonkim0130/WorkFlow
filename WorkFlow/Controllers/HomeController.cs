using System.Threading.Tasks;
using System.Web.Mvc;
using Dreamlab.Core;
using Resources;
using WorkFlow.Controllers;
using WorkFlow.Ext;
using WorkFlow.Logic;
using WorkFlow.Models;
using WorkFlowLib;
using WorkFlowLib.DTO;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;

namespace omnibackend.web.Controllers
{
    public class HomeController : AdapterController
    {   
        public async Task<ActionResult> Index(string lang)
        {
            ViewBag.username = Username;
            MenuViewModel menuModel = new MenuViewModel(); ;
            LoginApiClient login = new LoginApiClient();
            using (login.Wrapper)
            {
                string username = Username.EqualsIgnoreCaseAndBlank("admin") ? "2298311094" : Username;
                UserProfile up = login.UserProfile(username)?.ReturnValue?.data;
                if (up == null)
                    return PartialView("_PartialError", "unable to read user's profile form api");
                RequestResult<MenuResult> menusResult = await login.GetMenusAsync(new MenuParams
                {
                    AS_USID = username,
                    COUNTRY = up.Country,
                    AS_COUN = Codehelper.DefaultCountry,
                    AS_LANG = lang,
                    AS_SYST = "INTRANET"
                }, lang);   
                ViewData["UserProfile"] = up;
                menuModel.Menus = menusResult.ReturnValue.data;
            }
            ViewData["MenuViewModel"] = menuModel;
            return View();
        }

        public ActionResult ChangePassword()
        {
            return this.ModalView("ChangePassword");
        }

        [HttpPost]
        public async Task<ActionResult> ChangePassword(string Password, string NewPassword, string ConfirmPassword)
        {
            if (User.Identity.Name == "Admin")
            {
                return this.ShowErrorInModal("Admin cannot change password");
            }
            if (NewPassword == ConfirmPassword)
            {
                LoginApiClient login = new LoginApiClient();
                UserStaffInfo userInfo = WFUtilities.GetUserStaffInfo(this.Username);
                RequestResult<BoolResult> res = await login.ChangeUserPasswordAsync(User.Identity.Name, Password, NewPassword, userInfo.Country);
                if (!string.IsNullOrEmpty(res.ReturnValue.ret_msg))
                {
                    return this.ShowErrorInModal(res.ReturnValue.ret_msg);
                }
                return this.ShowSuccessModal(StringResource.PASSWORD_CHANGE);
            }
            return this.ShowErrorInModal(StringResource.PASSWORD_INCONSISTENT);
        }
    }
}