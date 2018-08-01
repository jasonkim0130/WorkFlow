using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Dreamlab.Core;
using Resources;
using WorkFlow.Logic;
using WorkFlowLib;
using WorkFlowLib.DTO;
using WorkFlowLib.DTO.Query;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;

namespace WorkFlow.Controllers
{
    public class PendingController : WFController
    {
        public ActionResult Index()
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            var data = manager.PendingQuery.OrderByDescending(p => p.Created)
                .Select(p => new PendingQueryRow
                {
                    IsFlagged = p.IsFlagged,
                    FlowCaseId = p.FlowCaseId,
                    Subject = p.Subject,
                    Department = p.Department,
                    Rejected = p.Rejected,
                    Aborted = p.ReviseAbort,
                    Applicant = p.UserNo,
                    Type = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    CompletedGroup = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0 && q.OrderId <= p.WF_StepGroups.OrderId),
                    TotalGroup = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0),
                    SubmitDate = p.Created,
                    LastApproverViewed = p.LastApproverViewed,
                    Ver = p.Ver,
                    IsDraft = p.StatusId == 0,
                    HasAttachment = p.WF_FlowCases_Attachments.Any(q => q.StatusId > 0)
                });
            ViewBag.currentUser = this.Username;
            return PartialView(data);
        }
        public ActionResult DraftIndex()
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            var data = manager.DraftQuery.OrderByDescending(p => p.Created)
                .Select(p => new PendingQueryRow
                {
                    IsFlagged = p.IsFlagged,
                    FlowCaseId = p.FlowCaseId,
                    Subject = p.Subject,
                    Department = p.Department,
                    Rejected = p.Rejected,
                    Aborted = p.ReviseAbort,
                    Applicant = p.UserNo,
                    Type = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    CompletedGroup = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0 && q.OrderId <= p.WF_StepGroups.OrderId),
                    TotalGroup = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0),
                    SubmitDate = p.Created,
                    LastApproverViewed = p.LastApproverViewed,
                    Ver = p.Ver,
                    IsDraft = p.StatusId == 0,
                    HasAttachment = p.WF_FlowCases_Attachments.Any(q => q.StatusId > 0)
                });
            ViewBag.currentUser = this.Username;
            ViewBag.IsDraft = true;
            return PartialView("~/Views/Pending/Index.cshtml", data);
        }

        public ActionResult ViewCase(int id)
        {
            FlowInfo flowInfo = PrepareViewCase(id);
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            manager.UpdateLastChecked(id, this.Username);
            ViewBag.History = manager.GetCaseHistory(flowInfo.CaseInfo.FlowCaseId, flowInfo.CaseInfo.BaseFlowCaseId);
            var flowType = WFEntities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowInfo.FlowTypeId);
            ViewBag.FlowType = flowType;
            //ViewBag.Cities = Entities.Cities.ToArray();
            if (flowType.TemplateType.HasValue)
            {
                if (flowType.TemplateType.Value == 7)
                {
                    var properties = manager.GetProperties(id);
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
            }
            return PartialView(flowInfo);
        }

        public ActionResult ViewReadOnlyCase(int id, int? current)
        {
            ViewBag.Title = StringResource.VIEW_PREVIOUS_VERSIONS;
            ViewBag.DisplayButtons = false;
            ViewBag.DisplayLogs = false;
            FlowInfo flowInfo = PrepareViewCase(id);
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            ViewBag.Logs =
                 manager.GetCaseLogs(flowInfo.CaseInfo.FlowCaseId)
                .Union(manager.GetCaseNotificationLogs(flowInfo.CaseInfo.FlowCaseId)).OrderBy(p => p.Created).ToArray();
            if (current.HasValue)
            {
                ViewBag.DisplaySelfButtons = true;
                ViewBag.FlowCaseId = current;
            }
            var flowType = WFEntities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowInfo.FlowTypeId);
            ViewBag.FlowType = flowType;
            //ViewBag.Cities = Entities.Cities.ToArray();
            if (flowType.TemplateType.HasValue)
            {
                if (flowType.TemplateType.Value == 7)
                {
                    var properties = manager.GetProperties(id);
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
            }
            return View("ViewReadOnlyCase", "~/Views/Shared/_LargeModalLayout.cshtml", flowInfo);
        }

        private FlowInfo PrepareViewCase(int id)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            FlowInfo flowInfo = manager.GetFlowAndCase(id);
            ViewBag.Properties = manager.GetProperties(id);
            ViewBag.Attachments = manager.GetAttachments(id);
            return flowInfo;
        }

        public ActionResult EditCase(int id)
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            FlowInfo flowInfo = manager.GetFlowAndCase(id);
            var properties = manager.GetProperties(id);
            ViewBag.Properties = properties;
            var attachments = manager.GetAttachments(id);
            ViewBag.Attachments = attachments;
            ViewBag.FinalNotifyUsers = manager.GetFinalNotifyUsers(id);
            var flowType = WFEntities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowInfo.FlowTypeId);
            ViewBag.FlowType = flowType;
            ViewBag.HasCoverDuties = WFEntities.WF_FlowGroups.FirstOrDefault(p => p.FlowTypeId == flowInfo.FlowTypeId && p.StatusId > 0)?.HasCoverUsers;
            ViewBag.LeaveTypes = WFUtilities.GetLeaveType(RouteData.Values["lang"] as string);

            if (flowType.TemplateType.HasValue)
            {
                if (flowType.TemplateType.Value == 1)
                {
                    ViewBag.Cities = WFEntities.Cities.Where(p => p.CountryCode == Country).AsNoTracking().ToArray();
                }
                else if (flowType.TemplateType.Value == 7)
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
                else if (flowType.TemplateType.Value == 2)
                {
                    UserStaffInfo userInfo = WFUtilities.GetUserStaffInfo(this.Username);
                    double balance = userInfo?.LeaveBalance ?? 0;
                    double notstarted = manager.GetNotStartedBalance(Username);
                    double unapproved = manager.GetUnApprovedBalance(Username);
                    ViewBag.DisplayBalance = notstarted + balance;
                    ViewBag.ValidBalance = balance > unapproved ? balance - unapproved : 0;
                }
            }
            return PartialView(flowInfo);
        }

        [HttpPost]
        public ActionResult SaveApp(int flowTypeId, int flowCaseId, CreateFlowCaseInfo caseinfo, string[] nextApprover)
        {
            Applicant manager = new Applicant(WFEntities, this.Username);
            FlowCaseInfo @case = manager.GetFlowCaseInfo(flowCaseId);
            if (!@case.Applicant.EqualsIgnoreCaseAndBlank(this.Username))
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "_ModalLayout", StringResource.YOU_CAN_NOT_MODIFY_APPLATION);
            }
            if (caseinfo.Deadline.HasValue && caseinfo.Deadline <= DateTime.Now)
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.INVALID_DEADLINE);
            }

            //if (!manager.CheckCancelLeaveBalance(caseinfo.Properties, flowTypeId))
            //{
            //    ViewBag.DisplayButtons = false;
            //    return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.ALREADY_CANCELED);
            //}

            if (nextApprover != null && nextApprover.Length > 0)
            {
                caseinfo.Approver = nextApprover;
                if (nextApprover.GroupBy(p => p).Any(p => p.Count() > 1))
                {
                    ViewBag.DisplayButtons = false;
                    return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.DUPLICATE_APPROVERS);
                }
            }
            int selectedFlowId = manager.SelectFlow(flowTypeId, caseinfo);
            if (selectedFlowId == 0 || selectedFlowId != caseinfo.FlowId)
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.TEMPLATE_OUT_OF_DATE);
            }
            if (selectedFlowId > 0)
            {
                caseinfo.FlowId = selectedFlowId;
                if ((nextApprover == null || nextApprover.Length == 0) && manager.HasSteps(selectedFlowId))
                {
                    NextStepData nsd = manager.GetNextStepApprovers(selectedFlowId, caseinfo.Properties, this.Username);
                    if (nsd == null || nsd.EmployeeList == null || nsd.EmployeeList.Count == 0 || nsd.EmployeeList.Any(p => p == null || p.Length == 0))
                    {
                        ViewBag.DisplayButtons = false;
                        return View("_PartialError", "_ModalLayout", StringResource.NO_NEXT_APPROVER_FOUND);
                    }
                    return View("~/Views/Application/SelectNextApprover.cshtml", "_ModalLayout", nsd.EmployeeList);
                    //if (nsd.EmployeeList.Count == 1 && nsd.EmployeeList[0].Length == 1)
                    //{
                    //    caseinfo.Approver = new string[] {nsd.EmployeeList.FirstOrDefault()?.FirstOrDefault()?.UserNo};
                    //}
                    //else
                    //{
                    //    return View("~/Views/Application/SelectNextApprover.cshtml", "~/Views/Shared/_ModalLayout.cshtml", nsd.EmployeeList);
                    //}
                }
                (CreateFlowResult result, int flowCaseId) result;
                if (@case.IsDraft)
                {
                    manager.MarkFlowAsObsolete(flowCaseId);
                    result = manager.CreateFlowCase(caseinfo);
                }
                else
                {
                    int version = (@case.Ver ?? 0) + 1;
                    int baseId = @case.BaseFlowCaseId ?? @case.FlowCaseId;
                    manager.MarkFlowAsObsolete(flowCaseId);
                    result = manager.CreateFlowCase(caseinfo, version, baseId);
                }
                ViewBag.FlowCaseId = result.flowCaseId;
                ViewBag.NextApprovers = caseinfo.Approver;
                if (result.result == CreateFlowResult.Success)
                {
                    if (caseinfo.Approver != null)
                    {
                        EmailService.SendWorkFlowEmail(
                            WFEntities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == User.Identity.Name)?.EmployeeName,
                            caseinfo.Approver, caseinfo.Subject, null);
                        manager.NotificationSender.PushInboxMessages(caseinfo.Approver, result.flowCaseId);
                        manager.NotificationSender.Send();
                    }

                    ViewBag.PendingCount = manager.CountPending();
                    return PartialView("_CreateResult", manager.GetFlowAndCase(result.flowCaseId));
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
        public ActionResult CancelApp(int flowCaseId)
        {
            Applicant manager = new Applicant(WFEntities, this.Username);
            var result = manager.Cancel(flowCaseId);
            manager.NotificationSender.Send();
            ViewBag.CancelResult = result;
            ViewBag.PendingCount = manager.CountPending();//#TODO
            return PartialView("_CancelResult", manager.GetFlowAndCase(flowCaseId));
        }
        /*
        [HttpPost]
        public ActionResult ReceivedPLExcel()
        {
            HttpPostedFileBase file = Request.Files.Count > 0 ? Request.Files[0] : null;
            if (file != null)
            {
                try
                {
                    string dir = Server.MapPath("~/temp/app");
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string newName = DateTime.Now.Ticks + Path.GetExtension(file.FileName);
                    string filename = Path.Combine(dir, newName);
                    file.SaveAs(filename);
                    if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xls"))
                    {
                        using (ExcelQueryFactory excel = new ExcelQueryFactory(filename))
                        {
                            ExcelQueryable<Row> sheet = excel.Worksheet(0);
                            var rows = sheet.Select(p => p).ToArray();
                            Dictionary<string, object> fileData = new Dictionary<string, object>();
                            var sumProfit = 0.0;
                            foreach (var row in rows)
                            {
                                var key = row[0].Value.ToString();
                                var keyForConstruction = row[3].Value.ToString();
                                if (key.Equals("Gross Profit") ||
                                    key.Equals("Net Sales") ||
                                    key.Equals("Occupancy charges") ||
                                    key.Equals("Salary") ||
                                    key.Equals("Depreciation") ||
                                    key.Equals("Royalty") ||
                                    key.Equals("Others") ||
                                    key.Equals("Total operating expenses") ||
                                    key.Equals("Operating Profit"))
                                {
                                    var formattedKey = key.Replace(" ", "");
                                    if (!fileData.ContainsKey(formattedKey))
                                    {
                                        var year1 = row[3].Value.ToString().Replace(",", "");
                                        var year2 = row[6].Value.ToString().Replace(",", "");
                                        var year3 = row[10].Value.ToString().Replace(",", "");
                                        fileData[formattedKey] = new Dictionary<string, string>()
                                        {
                                            { "Year1", year1},
                                            { "Year2", year2},
                                            { "Year3", year3}
                                        };
                                        if (key.Equals("Operating Profit"))
                                        {
                                            sumProfit = double.Parse(year1) + double.Parse(year2) + double.Parse(year3);
                                        }
                                    }
                                    if (key.Equals("Gross Profit"))
                                    {
                                        formattedKey = "GrossMargin";
                                        if (!fileData.ContainsKey(formattedKey))
                                        {
                                            var year1 = row[4].Value.ToString()
                                                                    .Replace(",", "")
                                                                    .Replace("%", "");
                                            var year2 = row[7].Value.ToString()
                                                                    .Replace(",", "")
                                                                    .Replace("%", "");
                                            var year3 = row[11].Value.ToString()
                                                                    .Replace(",", "")
                                                                    .Replace("%", "");
                                            fileData[formattedKey] = new Dictionary<string, string>()
                                            {
                                                { "Year1", year1},
                                                { "Year2", year2},
                                                { "Year3", year3}
                                            };
                                        }
                                    }
                                }
                                else if (key.Equals("Store Size (sq.m)"))
                                {
                                    fileData["StoreSize"] = row[1].Value.ToString().Replace(",", "");
                                }
                                else if (key.Equals("# of Staffs:"))
                                {
                                    fileData["ofStaffs"] = row[1].Value.ToString().Replace(",", "");
                                }
                                if (keyForConstruction.Equals("Walls, Ceiling & Floor") ||
                                    keyForConstruction.Equals("Furniture") ||
                                    keyForConstruction.Equals("Labor Cost") ||
                                    keyForConstruction.Equals("IT Equipment") ||
                                    keyForConstruction.Equals("Utilities & Others") ||
                                    keyForConstruction.Equals("Total Costs") ||
                                    keyForConstruction.Equals("Moving, Assembly, Removal"))
                                {
                                    var formattedKey = keyForConstruction
                                                        .Replace(" ", "")
                                                        .Replace("&", "")
                                                        .Replace(",", "");
                                    if (!fileData.ContainsKey(formattedKey))
                                    {
                                        var value = row[4].Value.ToString().Replace(",", "");
                                        fileData[formattedKey] = value;
                                        if (keyForConstruction.Equals("Total Costs") && value.Length > 0)
                                        {
                                            fileData["NetGain"] = sumProfit - double.Parse(value);
                                        }
                                    }
                                }
                                if (row[6].Value.ToString().Equals("Commission as % of Net Sales:"))
                                {
                                    var year1 = row[7].Value.ToString()
                                                            .Replace(",", "")
                                                            .Replace("%", "");
                                    var year2 = row[8].Value.ToString()
                                                            .Replace(",", "")
                                                            .Replace("%", "");
                                    var year3 = row[9].Value.ToString()
                                                            .Replace(",", "")
                                                            .Replace("%", "");
                                    fileData["CommissionPercent"] = new Dictionary<string, string>()
                                    {
                                        { "Year1", year1},
                                        { "Year2", year2},
                                        { "Year3", year3}
                                    };
                                }
                            }
                            ViewBag.fileData = JsonConvert.SerializeObject(fileData);
                        }
                    }
                    ViewBag.newName = newName;
                    ViewBag.filename = file.FileName;
                    ViewBag.fileSize = file.ContentLength;
                    return PartialView("~/Views/Application/_UploadedFile.cshtml");
                }
                catch (Exception e)
                {
                    Singleton<ILogWritter>.Instance.WriteExceptionLog("Read excel for event", e, null);
                    return Content("Unable to upload, " + e.Message);
                }
            }
            return Content("File is missing");
        }
        */


    }
}