using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Resources;
using WorkFlow.Ext;
using WorkFlowLib;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.DTO.Query;
using WorkFlowLib.Results;

namespace WorkFlow.Controllers
{
    public class InboxController : WFController
    {
        public ActionResult Index(string order, string type)
        {
            Approver applicant = new Approver(WFEntities, this.Username);
            InboxQueryRow[] actions = applicant.GetInboxApplications();
            foreach (InboxQueryRow rowData in actions)
            {
                if (rowData.Flag == 1 || (rowData.Deadline.HasValue && (rowData.Deadline.Value - DateTime.Now).TotalHours < 24 && (rowData.Deadline.Value - DateTime.Now).TotalHours > 0))
                {
                    rowData.Priority = 1;
                }
            }
            ViewBag.Order = order;
            ViewBag.Type = type;
            return PartialView(actions);
        }

        public ActionResult ViewCase(int flowCaseId)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            PropertiesValue properties = manager.GetProperties(flowCaseId);
            FlowInfo info = manager.GetFlowAndCase(flowCaseId);
            manager.SetCaseAsViewed(flowCaseId, this.Username);
            manager.UpdateLastChecked(flowCaseId, this.Username);
            WF_FlowTypes flowType = manager.GetFlowTypeById(info.FlowTypeId);
            if (flowType.TemplateType.HasValue && flowType.TemplateType.Value == 7)
            {
                WF_FlowPropertys prop = properties.PropertyInfo.FirstOrDefault(p => p.PropertyName.ToLower().Equals("brand") && p.StatusId < 0);
                if (prop != null)
                {
                    string brand = properties.Values.FirstOrDefault(p => p.PropertyId == prop.FlowPropertyId)?.StringValue;
                    Dictionary<string, string> shopList = WFEntities.BLSShopView
                                            .Where(s => s.Brand.ToLower().Equals(brand.ToLower()))
                                            .Select(s => new { s.ShopCode, s.ShopName })
                                            .ToDictionary(s => s.ShopCode, s => s.ShopName);
                    ViewBag.ShopList = shopList;
                }
            }
            ViewBag.Properties = properties;
            ViewBag.Attachments = manager.GetAttachments(flowCaseId);
            ViewBag.FlowType = flowType;
            ViewBag.History = manager.GetCaseHistory(info.CaseInfo.FlowCaseId, info.CaseInfo.BaseFlowCaseId);
            return PartialView(info);
        }

        public ActionResult ViewHistoryCases(int id)
        {
            ViewBag.Title = StringResource.VIEW_APPLICATION_HISTORY;
            ViewBag.DisplayButtons = false;
            ViewBag.DisplayLogs = false;
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            ViewBag.FlowCaseId = id;
            //ViewBag.DisplaySelfButtons = true;
            return View("ViewHistoryCasess", "~/Views/Shared/_LargeModalLayout.cshtml", manager.GetCaseHistory(id));
        }

        public ActionResult ApproveComment(int flowCaseId)
        {
            ViewBag.flowCaseId = flowCaseId;
            ViewBag.Title = StringResource.COMMENTS + " :";
            return this.ModalView("Comment");
        }

        [HttpPost]
        public ActionResult ApproveComment(int flowCaseId, string comments, bool? ccToAll)
        {
            if (string.IsNullOrWhiteSpace(comments))
                return this.ShowErrorInModal(StringResource.COMMENT_REQUIRED);
            ViewBag.flowCaseId = flowCaseId;
            Approver approver = new Approver(WFEntities, this.Username);
            var result = approver.CommentStep(flowCaseId, comments, ccToAll);
            if (result == CommentStepResult.Success)
            {
                approver.NotificationSender.Send();
                return RedirectToAction("Approve", new { flowCaseId = flowCaseId });
            }
            return this.ShowErrorInModal(StringResource.SAVE_COMMENT_FAILED);
        }

        public ActionResult Approve(int flowCaseId, string[] nextApprover)
        {
            Approver manager = new Approver(WFEntities, this.Username);
            if (nextApprover == null || nextApprover.Length == 0)
            {
                int curGroupId = manager.GetCurrentStepGroupId(flowCaseId);
                NextStepData nsd = manager.GetNextStepApprovers(flowCaseId, curGroupId);
                if (nsd.EmployeeList.Count > 0)
                {
                    ViewBag.flowCaseId = flowCaseId;
                    return View("SelectNextApprover", "~/Views/Shared/_ModalLayout.cshtml", nsd.EmployeeList);
                }
            }
            ReturnApproveResult returnValue = manager.Approve(flowCaseId, nextApprover);
            manager.NotificationSender.Send();
            ViewBag.nextStepUsers = returnValue.NextApprovers;
            ViewBag.approveResult = returnValue.Result;
            ViewBag.InboxCount = manager.CountInbox();
            return PartialView("Result", manager.GetFlowAndCase(flowCaseId));
        }

        public ActionResult RejectMessageModal(int flowCaseId)
        {
            ViewBag.flowCaseId = flowCaseId;
            return View("_RejectMessageModal", "~/Views/Shared/_ModalLayout.cshtml");
        }

        public ActionResult Reject(int flowCaseId, string comments)
        {
            Approver manager = new Approver(WFEntities, this.Username);
            ApproveResult result = manager.Reject(flowCaseId, comments);
            manager.NotificationSender.Send();
            ViewBag.approveResult = result;
            ViewBag.InboxCount = manager.CountInbox();
            return PartialView("Result", manager.GetFlowAndCase(flowCaseId));
        }

        public ActionResult SendBackMessageModal(int flowCaseId)
        {
            ViewBag.flowCaseId = flowCaseId;
            return View("_SendBackMessageModal", "~/Views/Shared/_ModalLayout.cshtml");
        }

        public ActionResult SendBack(int flowCaseId, string comments)
        {
            Approver applicant = new Approver(WFEntities, this.Username);
            ApproveResult result = applicant.Abort(flowCaseId, comments);
            applicant.NotificationSender.Send();
            ViewBag.approveResult = result;
            ViewBag.InboxCount = applicant.CountInbox();
            return PartialView("Result", applicant.GetFlowAndCase(flowCaseId));
        }

        public ActionResult GetCaseLogs(int flowCaseId)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            var logs =
                 manager.GetCaseLogs(flowCaseId)
                .Union(manager.GetCaseNotificationLogs(flowCaseId)).OrderBy(p => p.Created).ToArray();
            ViewBag.FlowCaseId = flowCaseId;
            ViewBag.ShowRefresh = true;
            return PartialView("_FlowLogs", logs);
        }

        public ActionResult ViewSimpleCase(int flowcaseid)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            return PartialView("ViewSimpleCase", manager.GetFlowAndCase(flowcaseid));
        }

        public ActionResult ViewComment(int messageId)
        {
            WF_CaseNotifications item = WFEntities.WF_CaseNotifications.FirstOrDefault(p => p.CaseNotificationId == messageId);
            ViewBag.Title = WebCacheHelper.GetWF_UsernameByNo(item?.Sender) + "'s comments:";
            return View("CommentContent", "~/Views/Shared/_ReadonlyModalLayout.cshtml", item?.Comments);
        }
    }
}