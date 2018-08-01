using System.Linq;
using System.Web.Mvc;
using WorkFlowLib;
using WorkFlowLib.DTO;

namespace WorkFlow.Controllers
{
    public class ArchiveController : WFController
    {
        public ActionResult Index()
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            var myNotifiedApplications = manager.GetDismissedNotifications();
            ViewBag.currentUser = this.Username;
            return PartialView(myNotifiedApplications.ToArray());
        }

        public ActionResult ViewCase(int id)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            FlowInfo flowInfo = manager.GetFlowAndCase(id);
            ViewBag.Properties = manager.GetProperties(id);
            ViewBag.Attachments = manager.GetAttachments(id);
            ViewBag.DisplayButtons = false;
            return PartialView("~/Views/Pending/ViewCase.cshtml", flowInfo);
        }
    }
}