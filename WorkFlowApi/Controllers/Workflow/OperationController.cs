using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web.Http;
using Newtonsoft.Json;
using Omnibackend.Api.Workflow.Data;
using Dreamlab.Core;
using WorkFlowLib;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;
using WorkFlowLib.DTO;

namespace Omnibackend.Api.Controllers.Workflow
{
    [RoutePrefix("workflow/operation")]
    [Authorize]
    public class OperationController : BaseWFApiController
    {
        [HttpPost]
        [Route("get_next_approvers")]
        public object GetNextApprovers(NextApproverParameterModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-get_next_approvers", JsonConvert.SerializeObject(model));
            if (model.caseInfo.Deadline.HasValue && model.caseInfo.Deadline.Value.ToUniversalTime() <= DateTime.UtcNow)
            {
                return new { ret_code = RetCode.Error, ret_msg = "Invalid dead line" };
            }

            Applicant manager = new Applicant(Entities, User.Identity.Name);
            int selectedFlowId = manager.SelectFlow(model.flowTypeId, model.caseInfo);
            if (selectedFlowId > 0)
            {
                if (!manager.HasSteps(selectedFlowId))
                {
                    //如果没有step，允许用户在没有nextapprover的情况下创建case，此时返回成功，但是nextapprover是空数组
                    string[] empty = new string[0];
                    return new { ret_code = RetCode.Success, ret_objects = empty };
                }

                try
                {
                    NextStepData nsd = manager.GetNextStepApprovers(selectedFlowId, model.caseInfo.Properties,
                        User.Identity.Name);
                    if (nsd == null || nsd.EmployeeList == null || nsd.EmployeeList.Count == 0 ||
                        nsd.EmployeeList.Any(p => p == null || p.Length == 0))
                    {
                        return new { ret_code = RetCode.Error, ret_msg = "未找到符合条件的审批者" };
                    }

                    return new { ret_code = RetCode.Success, ret_objects = nsd.EmployeeList.ToArray() };
                }
                catch (Exception ex)
                {
                    return new { ret_code = RetCode.Error, ret_msg = ex.Message };
                }
            }
            else
            {
                return new { ret_code = RetCode.Error, ret_msg = "Can not find workflow" };
            }
        }

        [HttpGet]
        [Route("get_next_approvers_by_caseid")]
        public object GetNextApprovers(int flowcaseid)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-get_next_approvers_by_caseid", flowcaseid.ToString());
            Applicant applicant = new Applicant(Entities, User.Identity.Name);
            ApplicationUser applicationUser = new ApplicationUser(Entities, User.Identity.Name);
            var flowcase = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == flowcaseid && p.StatusId > 0);
            if (flowcase == null)
            {
                return new { ret_code = RetCode.Error, ret_msg = "Can not find workflow" };
            }

            try
            {
                NextStepData nsd = applicant.GetNextStepApprovers(flowcase.FlowId, applicationUser.GetPropertyValues(flowcaseid), User.Identity.Name);
                if (nsd == null || nsd.EmployeeList == null || nsd.EmployeeList.Count == 0 ||
                    nsd.EmployeeList.Any(p => p == null || p.Length == 0))
                {
                    return new { ret_code = RetCode.Error, ret_msg = "未找到符合条件的审批者" };
                }
                return new { ret_code = RetCode.Success, ret_objects = nsd.EmployeeList.ToArray() };
            }
            catch (Exception e)
            {
                return new { ret_code = RetCode.Error, ret_msg = e.Message };
            }
        }

        [HttpPost]
        [Route("create_cancel")]
        public object CreateCancel(CreateCancelParameterModel model)
        {
            Applicant applicant = new Applicant(Entities, User.Identity.Name);
            ApplicationUser applicationUser = new ApplicationUser(Entities, User.Identity.Name);
            var flowcase = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == model.flowcaseid && p.StatusId > 0);
            if (flowcase == null)
            {
                return new { ret_code = RetCode.Error, ret_msg = "Can not find workflow" };
            }

            if (Entities.WF_FlowCases.FirstOrDefault(p =>
                    p.StatusId > 0 && p.RelatedFlowCaseId == model.flowcaseid) != null)
            {
                return new { ret_code = RetCode.Error, ret_msg = "Cancelled already" };
            }

            CreateFlowCaseInfo create = new CreateFlowCaseInfo();
            if (model.approvers != null && model.approvers.Length > 0)
            {
                create.Approver = model.approvers;
                if (model.approvers.GroupBy(p => p).Any(p => p.Count() > 1))
                {
                    return new { ret_code = RetCode.Error, ret_msg = "Duplicate approvers" };
                }
            }
            create.FlowId = flowcase.FlowId;
            create.Properties = applicationUser.GetPropertyValues(model.flowcaseid);
            create.Subject = flowcase.Subject + "[Cancel]";
            create.RelatedCaseId = model.flowcaseid;
            create.Deadline = flowcase.Deadline;
            create.Dep = flowcase.Department;
            create.NotifyUsers = flowcase.WF_CaseNotificateUsers.Where(p => p.StatusId > 0).Select(p => p.UserNo)
                .ToArray();
            create.CoverDuties = flowcase.WF_CaseCoverUsers.Where(p => p.StatusId > 0).Select(p => p.UserNo)
                .ToArray();
            try
            {
                (CreateFlowResult result, int flowCaseId) res = applicant.CreateFlowCase(create);
                if (res.result == CreateFlowResult.Success)
                {
                    if (create.Approver != null)
                    {
                        EmailService.SendWorkFlowEmail(
                            Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == User.Identity.Name)?.EmployeeName,
                            create.Approver, create.Subject, null);
                        applicant.NotificationSender.PushInboxMessages(create.Approver, res.flowCaseId);
                        applicant.NotificationSender.Send();
                    }

                    return new { ret_code = RetCode.Success };
                }
            }
            catch (Exception ex)
            {
                return new { ret_code = RetCode.Error, ret_msg = ex.Message };
            }
            return new { ret_code = RetCode.Error, ret_msg = "Can not create application" };
        }

        [HttpPost]
        [Route("post_application")]
        public object PostApplication(SaveApplicationParameterModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-PostApplication", JsonConvert.SerializeObject(model));
            //对于没有step的case，客户端传入的nextapprover需为空
            if (model.caseInfo.Deadline.HasValue && model.caseInfo.Deadline.Value.ToUniversalTime() <= DateTime.UtcNow)
            {
                return new { ret_code = RetCode.Error, ret_msg = "Invalid dead line" };
            }

            Applicant manager = new Applicant(Entities, User.Identity.Name);
            int selectedFlowId = manager.SelectFlow(model.flowTypeId, model.caseInfo);
            bool hasSteps = manager.HasSteps(selectedFlowId);
            model.caseInfo.Approver = model.nextApprover;

            if (model.nextApprover != null && model.nextApprover.Length > 0)
            {
                if (model.nextApprover.GroupBy(p => p).Any(p => p.Count() > 1))
                {
                    return new { ret_code = RetCode.Error, ret_msg = "Duplicate approvers" };
                }
            }

            //if (!manager.CheckCancelLeaveBalance(model.caseInfo.Properties, model.flowTypeId))
            //{
            //    return new { ret_code = RetCode.Error, ret_msg = "The date you selected is cancelled already." };
            //}

            if (selectedFlowId > 0)
            {
                model.caseInfo.FlowId = selectedFlowId;
                if ((model.nextApprover == null || model.nextApprover.Length == 0) && hasSteps)
                {
                    return new { ret_code = RetCode.Error, ret_msg = "Must provide next approver" };
                }

                try
                {
                    (CreateFlowResult result, int flowCaseId) res = manager.CreateFlowCase(model.caseInfo);
                    if (res.result == CreateFlowResult.Success)
                    {
                        if (model.caseInfo.Approver != null)
                        {
                            EmailService.SendWorkFlowEmail(
                                Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == User.Identity.Name)?.EmployeeName,
                                model.caseInfo.Approver, model.caseInfo.Subject, null);
                            manager.NotificationSender.PushInboxMessages(model.caseInfo.Approver, res.flowCaseId);
                            manager.NotificationSender.Send();
                        }

                        return new { ret_code = RetCode.Success };
                    }
                }
                catch (Exception ex)
                {
                    return new { ret_code = RetCode.Error, ret_msg = ex.Message };
                }
            }
            else
            {
                return new { ret_code = RetCode.Error, ret_msg = "Can not find workflow" };
            }

            return new { ret_code = RetCode.Error, ret_msg = "Can not create application" };
        }

        [HttpPost]
        [Route("edit_application")]
        public object edit_application(EditApplicationParameterModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-edit_application", JsonConvert.SerializeObject(model));
            Applicant manager = new Applicant(Entities, User.Identity.Name);
            FlowCaseInfo @case = manager.GetFlowCaseInfo(model.flowCaseId);
            if (!@case.Applicant.EqualsIgnoreCaseAndBlank(User.Identity.Name))
            {
                return new { ret_code = RetCode.Error, ret_msg = "Can only edit application yourself." };
            }

            if (model.caseInfo.Deadline.HasValue && model.caseInfo.Deadline.Value.ToUniversalTime() <= DateTime.UtcNow)
            {
                return new { ret_code = RetCode.Error, ret_msg = "Invalid dead line" };
            }

            if (model.nextApprover != null && model.nextApprover.Length > 0)
            {
                model.caseInfo.Approver = model.nextApprover;
                if (model.nextApprover.GroupBy(p => p).Any(p => p.Count() > 1))
                {
                    return new { ret_code = RetCode.Error, ret_msg = "Duplicate approvers." };
                }
            }

            int selectedFlowId = manager.SelectFlow(model.flowTypeId, model.caseInfo);
            if (selectedFlowId == 0)
            {
                return new { ret_code = RetCode.Error, ret_msg = "Template out of date." };
            }

            if (selectedFlowId > 0)
            {
                model.caseInfo.FlowId = selectedFlowId;
                (CreateFlowResult result, int flowCaseId) res;
                if (@case.IsDraft)
                {
                    manager.MarkFlowAsObsolete(model.flowCaseId);
                    res = manager.CreateFlowCase(model.caseInfo);
                }
                else
                {
                    int version = (@case.Ver ?? 0) + 1;
                    int baseId = @case.BaseFlowCaseId ?? @case.FlowCaseId;
                    manager.MarkFlowAsObsolete(model.flowCaseId);
                    res = manager.CreateFlowCase(model.caseInfo, version, baseId);
                }

                if (res.result == CreateFlowResult.Success)
                {
                    if (model.caseInfo.Approver != null)
                    {
                        EmailService.SendWorkFlowEmail(
                            Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == User.Identity.Name)?.EmployeeName,
                            model.caseInfo.Approver, model.caseInfo.Subject, null);
                        manager.NotificationSender.PushInboxMessages(model.caseInfo.Approver, res.flowCaseId);
                        manager.NotificationSender.Send();
                    }

                    return new { ret_code = RetCode.Success };
                }
            }
            else
            {
                return new { ret_code = RetCode.Error, ret_msg = "未找到符合条件的工作流" };
            }

            return new { ret_code = RetCode.Error, ret_msg = "创建失败" };
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("upload_file")]
        public object upload_file(UploadFileModel model)
        {
            string token = Request.Headers.GetValues("clientsecret").FirstOrDefault();
            if (!token.Equals(Consts.WorkFlowApIToken))
            {
                return new { ret_code = RetCode.Error, ret_msg = "Bad request" };
            }

            string subDir = DateTime.Now.ToString("yyyyMMdd");
            string dir = Path.Combine(Dir, subDir);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string newName = DateTime.Now.Ticks + Path.GetExtension(model.OriFileName);
            string filename = Path.Combine(dir, newName);
            try
            {
                byte[] bytes = Convert.FromBase64String(model.Base64String);
                using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
            }
            catch (Exception ex)
            {
                return new { ret_code = RetCode.Error, ret_msg = "upload failed" };
            }

            return new { ret_code = RetCode.Success, fileName = Path.Combine(subDir, newName) };
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("saveError")]
        public object SaveError(ErrorModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow_app_error", model.version, model.error);
            return new { ret_code = RetCode.Success, ret_msg = "" };
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("upload_icon")]
        public object upload_icon(UploadFileModel model)
        {
            string token = Request.Headers.GetValues("clientsecret").FirstOrDefault();
            if (!token.Equals(Consts.WorkFlowApIToken))
            {
                return new { ret_code = RetCode.Error, ret_msg = "Bad request" };
            }

            string dir = Path.Combine(Dir, "icons\\");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string filename = Path.Combine(dir, DateTime.Now.Ticks.ToString());
            try
            {
                byte[] bytes = Convert.FromBase64String(model.Base64String);
                using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
            }
            catch (Exception ex)
            {
                return new { ret_code = RetCode.Error, ret_msg = "upload failed" };
            }

            return new { ret_code = RetCode.Success, fileName = filename };
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("archive_approved")]
        public object archive_approved([FromBody] int flowCaseId)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-archive_approved", flowCaseId.ToString());
            string token = Request.Headers.GetValues("clientsecret").FirstOrDefault();
            if (!token.Equals(Consts.WorkFlowApIToken))
            {
                return new { ret_code = RetCode.Error, ret_msg = "Bad request" };
            }

            try
            {
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
                {
                    var flowCase = Entities.WF_FlowCases.AsNoTracking().Select(c => new
                    {
                        c.FlowCaseId,
                        Applicant = c.UserNo,
                        c.Subject,
                        c.Approved,
                        c.WF_Flows.WF_FlowGroups.ApprovedArchivePath,
                        CountryArchivePaths = c.WF_Flows.WF_FlowGroups.WF_CountryArchivePaths.Select(p =>
                            new { CountryCode = p.CountryCode, ApprovedArchivePath = p.ApprovedArchivePath })
                    }).FirstOrDefault(c => c.FlowCaseId == flowCaseId);
                    if (flowCase == null || !flowCase.Approved.HasValue ||
                        (string.IsNullOrWhiteSpace(flowCase.ApprovedArchivePath) &&
                         flowCase.CountryArchivePaths.Count() == 0))
                        return new { ret_code = RetCode.Error, ret_msg = "Invalid flowcase" };

                    var countryCode = Entities.GlobalUserView.Where(p => p.EmpStatus == "A").FirstOrDefault(u => u.EmployeeID == flowCase.Applicant)
                        ?.Country;
                    if (string.IsNullOrEmpty(countryCode))
                        return new { ret_code = RetCode.Error, ret_msg = "Invalid user" };

                    var archivePath = flowCase.CountryArchivePaths
                        .FirstOrDefault(p => p.CountryCode.EqualsIgnoreCase(countryCode))?.ApprovedArchivePath;
                    if (string.IsNullOrEmpty(archivePath))
                        archivePath = flowCase.ApprovedArchivePath;

                    if (string.IsNullOrEmpty(archivePath))
                        return new { ret_code = RetCode.Error, ret_msg = "Path is not configured" };

                    if (!Directory.Exists(archivePath))
                        try
                        {
                            Directory.CreateDirectory(archivePath);
                        }
                        catch
                        {
                        }

                    ;
                    if (!Directory.Exists(archivePath))
                        return new { ret_code = RetCode.Error, ret_msg = "Archive folder not found" };

                    if (!Directory.Exists(Dir))
                        return new { ret_code = RetCode.Error, ret_msg = "Attachment folder not found" };

                    var attachments = Entities.WF_FlowCases_Attachments.Where(f => f.FlowCaseId == flowCase.FlowCaseId)
                        .Select(a => new { a.FileName, a.OriFileName }).ToArray();
                    string attachFileName, newFileName;
                    int fileNum = 2;
                    var archivedFileNames = new List<string>();
                    foreach (var att in attachments)
                    {
                        attachFileName = Path.Combine(Dir, att.FileName);
                        if (!File.Exists(attachFileName)) continue;

                        if (!archivedFileNames.Contains(att.OriFileName) && Regex.IsMatch(att.OriFileName,
                                $@"^PDF FORM .+\-{flowCase.FlowCaseId}\.pdf$"))
                            newFileName = att.OriFileName;
                        else
                            newFileName =
                                $"PDF FORM {flowCase.Subject}-{flowCase.FlowCaseId}-{fileNum++}{Path.GetExtension(att.OriFileName)}";
                        archivedFileNames.Add(newFileName);

                        File.Copy(attachFileName, Path.Combine(archivePath, newFileName), true);
                    }
                }
            }
            catch
            {
                return new { ret_code = RetCode.Error, ret_msg = "upload failed" };
            }

            return new { ret_code = RetCode.Success };
        }
    }

    public class ErrorModel
    {
        public string error { get; set; }
        public string version { get; set; }
    }
}