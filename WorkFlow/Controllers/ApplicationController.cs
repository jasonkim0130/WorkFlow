using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.Entity;
using Dreamlab.Core;
using Newtonsoft.Json;
using Resources;
using WorkFlow.Logic;
using WorkFlowLib.Data;
using WorkFlowLib;
using WorkFlowLib.DTO;
using WorkFlowLib.Logic;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;

namespace WorkFlow.Controllers
{
    public class ApplicationController : WFController
    {
        public ActionResult Index()
        {
            WF_FlowTypes[] types = WFEntities.WF_FlowTypes.Where(p => p.StatusId > 0).ToArray();
            if (!Codehelper.IsUat)
            {
                types = types.Where(p => p.TemplateType == 2).ToArray();
            }
            return PartialView(types);
        }

        public ActionResult NotifyUser()
        {
            return PartialView("_NotifyUser");
        }

        public ActionResult AddCoverDuties()
        {
            ViewData["CurrentStaffNo"] = this.Username;
            ViewData["Deletable"] = true;
            return PartialView("_CoverDuties");
        }

        public ActionResult FillFormValue(int flowtypeid)
        {
            ViewBag.Country = Country;
            UserStaffInfo userInfo = WFUtilities.GetUserStaffInfo(this.Username);
            ViewBag.CurrentDep = userInfo?.DepartmentName;
            WF_FlowTypes flowType = WFEntities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowtypeid && p.StatusId > 0);
            ViewBag.FlowType = flowType;
            if (flowType?.TemplateType.HasValue == true)
            {
                if (flowType.TemplateType.Value == (int)FlowTemplateType.DynamicTemplate)
                {
                    var props =
                        WFEntities.WF_FlowGroups.AsNoTracking()
                            .Include("WF_FlowPropertys")
                            .FirstOrDefault(p => p.FlowTypeId == flowtypeid && p.StatusId > 0)?.WF_FlowPropertys.ToArray();
                    return PartialView("FillDynamicFormValue", props);
                }
                if (flowType.TemplateType.Value == (int)FlowTemplateType.StoreApprovalTemplate)
                {
                    ViewBag.Cities = WFEntities.Cities.Where(p => p.CountryCode == Country).AsNoTracking().ToArray();
                }
                else if (flowType.TemplateType.Value == (int)FlowTemplateType.LeaveTemplate)
                {
                    ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
                    double balance = userInfo?.LeaveBalance ?? 0;
                    double notstarted = manager.GetNotStartedBalance(Username);
                    double unapproved = manager.GetUnApprovedBalance(Username);
                    ViewBag.NotStartedBalance = notstarted;
                    ViewBag.APIReturnBalance = balance;
                    ViewBag.DisplayBalance = notstarted + balance;
                    ViewBag.ValidBalance = balance > unapproved ? balance - unapproved : 0;
                    ViewBag.LeaveTypes = WFUtilities.GetLeaveType(RouteData.Values["lang"] as string);
                }
            }
            ViewBag.StaffNo = this.Username;
            ViewBag.HasCoverDuties = WFEntities.WF_FlowGroups.FirstOrDefault(p => p.FlowTypeId == flowtypeid && p.StatusId > 0)?.HasCoverUsers;
            return PartialView("FillFormValue", WFEntities.WF_FlowGroups.AsNoTracking().Include("WF_FlowPropertys").FirstOrDefault(p => p.FlowTypeId == flowtypeid && p.StatusId > 0));
        }

        [HttpPost]
        public ActionResult SaveApp(int flowTypeId, CreateFlowCaseInfo caseInfo, string[] nextApprover)
        {
            if (caseInfo.Deadline.HasValue && caseInfo.Deadline <= DateTime.Now)
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.INVALID_DEADLINE);
            }

            Applicant manager = new Applicant(WFEntities, this.Username);

            //if (!manager.CheckCancelLeaveBalance(caseInfo.Properties, flowTypeId))
            //{
            //    ViewBag.DisplayButtons = false;
            //    return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.ALREADY_CANCELED);
            //}

            if (nextApprover != null && nextApprover.Length > 0)
            {
                caseInfo.Approver = nextApprover;
                if (nextApprover.GroupBy(p => p).Any(p => p.Count() > 1))
                {
                    ViewBag.DisplayButtons = false;
                    return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.DUPLICATE_APPROVERS);
                }
            }

            int selectedFlowId = manager.SelectFlow(flowTypeId, caseInfo);
            if (selectedFlowId > 0)
            {
                caseInfo.FlowId = selectedFlowId;
                bool HasStep = manager.HasSteps(selectedFlowId);
                if ((nextApprover == null || nextApprover.Length == 0) && HasStep)
                {
                    NextStepData nsd = manager.GetNextStepApprovers(selectedFlowId, caseInfo.Properties, this.Username);
                    if (nsd == null || nsd.EmployeeList == null || nsd.EmployeeList.Count == 0 || nsd.EmployeeList.Any(p => p == null || p.Length == 0))
                    {
                        ViewBag.DisplayButtons = false;
                        return View("_PartialError", "_ModalLayout", StringResource.NO_NEXT_APPROVER_FOUND);
                    }
                    return View("SelectNextApprover", "_ModalLayout", nsd.EmployeeList);
                }
                (CreateFlowResult result, int flowCaseId) res = manager.CreateFlowCase(caseInfo);
                ViewBag.FlowCaseId = res.flowCaseId;
                ViewBag.NextApprovers = caseInfo.Approver;
                if (res.result == CreateFlowResult.Success)
                {
                    EmailService.SendWorkFlowEmail(
                            WFEntities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == User.Identity.Name)?.EmployeeName,
                            caseInfo.Approver, caseInfo.Subject, null);
                    manager.NotificationSender.PushInboxMessages(caseInfo.Approver, res.flowCaseId);
                    manager.NotificationSender.Send();
                    ViewBag.PendingCount = manager.CountPending();
                    return PartialView("_CreateResult", manager.GetFlowAndCase(res.flowCaseId));
                }
            }
            else
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "_ModalLayout", StringResource.NO_ELIGIBLE_WORKFLOW_FOUND);
            }
            ViewBag.DisplayButtons = false;
            return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.UNABLE_TO_CREATE_YOUR_APPLICATION);
        }

        [HttpPost]
        public ActionResult SaveDraft(int flowTypeId, CreateFlowCaseInfo caseInfo)
        {
            Applicant manager = new Applicant(WFEntities, this.Username);
            int selectedFlowId = manager.SelectFlow(flowTypeId, caseInfo);
            if (selectedFlowId > 0)
            {
                caseInfo.FlowId = selectedFlowId;
                (CreateFlowResult result, int flowCaseId) result = manager.SaveDraft(caseInfo);
                if (result.result == CreateFlowResult.Success)
                {
                    ViewBag.PendingCount = manager.CountPending();
                    ViewBag.FlowCaseId = result.flowCaseId;
                    return PartialView("_SaveDraftResult", manager.GetFlowAndCase(result.flowCaseId));
                }
                else
                {
                    ViewBag.DisplayButtons = false;
                    return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.UNABLE_TO_CREATE_YOUR_APPLICATION);
                }
            }
            ViewBag.DisplayButtons = false;
            return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.NO_ELIGIBLE_WORKFLOW_FOUND);
        }

        public JsonResult GetShopList(string brand)
        {
            var shops = WFEntities.BLSShopView
                .Where(p => p.Brand.ToLower().Equals(brand.ToLower()) && p.Country.ToLower().Equals(Country.ToLower()))
                .ToArray();

            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = JsonConvert.SerializeObject(shops)
            };
        }

        public JsonResult GetShopInfo(string shopCode)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            var propertyInfos = manager.GetStorePropertiesForClosure(shopCode);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (propertyInfos != null)
            {
                string[] propertyNames =
                {
                    "StoreType",
                    "StoreNamePrefix",
                    "City",
                    "StoreLocation",
                    "StoreTypeForName",
                    "CityTier",
                    "MallDeptStoreTier",
                    "StoreSize",
                    "OfSalesStaff",
                    "ExpectedOpeningDate",
                    "NameOfLandlord",
                    "CreditTerms",
                    "ContractPeriodFrom",
                    "ContractPeriodTo",
                    "CurrentTermType",
                    "CurrentTermRent",
                    "CurrentTermTurnover",
                    "CashAmount",
                    "MoneyRefundable",
                    "Premium"
                };
                var properties = propertyInfos.PropertyInfo
                    .Where(p => propertyNames.Contains(p.PropertyName))
                    .ToList();
                for (int i = 0; i < properties.Count(); i++)
                {
                    var value = propertyInfos.Values.FirstOrDefault(p => p.PropertyId == properties[i].FlowPropertyId);
                    if (value != null)
                    {
                        var temp = value?.StringValue
                                        ?? value?.IntValue?.ToString()
                                        ?? value?.DateTimeValue?.ToString("yyyy-MM-ddTHH:mm")
                                        ?? value?.NumericValue?.ToString("#.##")
                                        ?? value?.TextValue
                                        ?? value?.DateValue?.ToString("M/d/yyyy")
                                        ?? value?.UserNoValue;
                        if (properties[i].PropertyName.EqualsIgnoreCaseAndBlank("City"))
                        {
                            temp = WFEntities.Cities.FirstOrDefault(c => c.Code.ToLower().Equals(temp.ToLower()))?.Name;
                        }
                        dict.Add(properties[i].PropertyName, temp);
                    }
                }
            }

            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = JsonConvert.SerializeObject(dict)
            };
        }
    }
}