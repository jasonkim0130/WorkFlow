using System.Linq;
using System.Web.Mvc;
using Dreamlab.Core;
using Resources;
using WorkFlowLib;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;

namespace WorkFlow.Controllers
{
    public class NotificationController : WFController
    {
        public ActionResult Index()
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            var myNotifiedApplications = manager.GetUndismissedNotifications();
            ViewBag.currentUser = this.Username;
            return PartialView(myNotifiedApplications.ToArray());
        }

        public ActionResult ViewItem(int flowcaseid, int caseNotificationReceiverId, bool enableCancel = false)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            manager.UpdateLastChecked(flowcaseid, this.Username);
            WF_CaseNotificationReceivers not = manager.NotificationManager.SetNotificationAsRead(caseNotificationReceiverId);
            ViewBag.Notification = not;
            ViewBag.Comments = manager.NotificationManager.GetComments(flowcaseid);
            ViewBag.Attachments = manager.GetAttachments(flowcaseid);
            manager.NotificationManager.SetNotificationDismissed(caseNotificationReceiverId);
            FlowInfo info = manager.GetFlowAndCase(flowcaseid);
            var flowType = WFEntities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == info.FlowTypeId);
            ViewBag.FlowType = flowType;
            ViewBag.Cities = WFEntities.Cities.ToArray();
            var properties = manager.GetProperties(flowcaseid);
            ViewBag.Properties = properties;
            ViewBag.Attachments = manager.GetAttachments(flowcaseid);
            ViewBag.EnableCancel = enableCancel
                                   && info.CaseInfo.Applicant.EqualsIgnoreCase(User.Identity.Name)
                                   && (!info.CaseInfo.RelatedFlowCaseId.HasValue || info.CaseInfo.RelatedFlowCaseId == 0)
                                   && not.WF_CaseNotifications.NotificationType == (int) NotificationTypes.AppFinishedApproved;
            if (flowType.TemplateType.HasValue && flowType.TemplateType.Value == 7)
            {
                var prop = properties.PropertyInfo.FirstOrDefault(p => p.PropertyName.ToLower().Equals("brand") && p.StatusId < 0);
                if (prop != null)
                {
                    var brand = properties.Values.FirstOrDefault(p => p.PropertyId == prop.FlowPropertyId)?.StringValue;
                    var shopList = WFEntities.BLSShopView
                                            .Where(s => s.Brand.ToLower().Equals(brand.ToLower()))
                                            .Select(s => new { s.ShopCode, s.ShopName })
                                            .ToDictionary(s => s.ShopCode, s => s.ShopName);
                    ViewBag.ShopList = shopList;
                }
            }
            return PartialView("ViewNotification", info);
        }

        [HttpPost]
        public ActionResult CreateCancel(int flowcaseid, string[] nextApprover)
        {
            Applicant applicant = new Applicant(WFEntities, this.Username);
            ApplicationUser applicationUser = new ApplicationUser(WFEntities, this.Username);
            var flowcase = WFEntities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == flowcaseid && p.StatusId > 0);
            if (WFEntities.WF_FlowCases.FirstOrDefault(p =>
                    p.StatusId > 0 && p.RelatedFlowCaseId == flowcaseid) != null)
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", "Cancelled already.");
            }
            if (flowcase?.FlowId > 0)
            {
                bool HasStep = applicant.HasSteps(flowcase.FlowId);
                if ((nextApprover == null || nextApprover.Length == 0) && HasStep)
                {
                    NextStepData nsd = applicant.GetNextStepApprovers(flowcase.FlowId,
                        applicationUser.GetPropertyValues(flowcaseid), this.Username);
                    if (nsd == null || nsd.EmployeeList == null || nsd.EmployeeList.Count == 0 ||
                        nsd.EmployeeList.Any(p => p == null || p.Length == 0))
                    {
                        ViewBag.DisplayButtons = false;
                        return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.NO_NEXT_APPROVER_FOUND);
                    }
                    return View("SelectNextApprover", "~/Views/Shared/_ModalLayout.cshtml", nsd.EmployeeList);
                }
                CreateFlowCaseInfo create = new CreateFlowCaseInfo();
                if (nextApprover != null && nextApprover.Length > 0)
                {
                    create.Approver = nextApprover;
                    if (nextApprover.GroupBy(p => p).Any(p => p.Count() > 1))
                    {
                        ViewBag.DisplayButtons = false;
                        return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.DUPLICATE_APPROVERS);
                    }
                }

                create.FlowId = flowcase.FlowId;
                create.Properties = applicationUser.GetPropertyValues(flowcaseid);
                create.Subject = flowcase.Subject + "[Cancel]";
                create.RelatedCaseId = flowcaseid;
                create.Deadline = flowcase.Deadline;
                create.Dep = flowcase.Department;
                create.NotifyUsers = flowcase.WF_CaseNotificateUsers.Where(p => p.StatusId > 0).Select(p => p.UserNo)
                    .ToArray();
                create.CoverDuties = flowcase.WF_CaseCoverUsers.Where(p => p.StatusId > 0).Select(p => p.UserNo)
                    .ToArray();
                (CreateFlowResult result, int flowCaseId) res = applicant.CreateFlowCase(create);
                ViewBag.FlowCaseId = res.flowCaseId;
                ViewBag.NextApprovers = create.Approver;
                if (res.result == CreateFlowResult.Success)
                {
                    EmailService.SendWorkFlowEmail(
                            WFEntities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == User.Identity.Name)?.EmployeeName,
                            create.Approver, create.Subject, null);
                    applicant.NotificationSender.PushInboxMessages(create.Approver, res.flowCaseId);
                    applicant.NotificationSender.Send();
                    ViewBag.PendingCount = applicant.CountPending();
                    return PartialView("_CreateResult", applicant.GetFlowAndCase(res.flowCaseId));
                }
            }
            ViewBag.DisplayButtons = false;
            return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", "Cancel flowcase failed.");
        }
    }
}