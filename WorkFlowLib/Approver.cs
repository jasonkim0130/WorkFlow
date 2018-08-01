using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dreamlab.Core;
using Newtonsoft.Json.Linq;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.Logic;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 2/13/2017 3:58:41 PM.
    */
    public class Approver : ApplicationUser
    {
        public Approver(WorkFlowEntities entities, string applicantUser) : base(entities, applicantUser)
        {

        }

        private string GetPropertyCode(PropertyInfo info)
        {
            var flowProperty = Entities.WF_FlowPropertys.FirstOrDefault(fp => fp.FlowPropertyId == info.Id);
            if (flowProperty != null && flowProperty.Code != null)
            {
                var tpc = Entities.WF_TemplatePropCode.FirstOrDefault(tp => tp.TemplatePropCodeId == flowProperty.Code);
                if (tpc != null)
                    return tpc.PropCode;
            }
            return null;
        }

        private int GetOverseasClaimCodeId(string overseasTripCodeId)
        {
            string code = null;
            if (overseasTripCodeId.Equals("OTA_Destinations"))
            {
                code = "OCA_Destinations";
            }
            else if (overseasTripCodeId.Equals("OTA_From"))
            {
                code = "OCA_From";
            }
            else if (overseasTripCodeId.Equals("OTA_To"))
            {
                code = "OCA_To";
            }
            else if (overseasTripCodeId.Equals("OTA_TripDays"))
            {
                code = "OCA_TripDays";
            }
            else if (overseasTripCodeId.Equals("OTA_Purpose"))
            {
                code = "OCA_Purpose";
            }
            else if (overseasTripCodeId.Equals("OTA_Estimated_Total"))
            {
                code = "OCA_Budgeted_Amount";
            }
            var tcode = Entities.WF_TemplatePropCode.FirstOrDefault(
                tp => tp.StatusId > 0 && tp.PropCode == code && tp.TemplateType == (int)FlowTemplateType.OverSeasClaimTemplate);
            if (tcode != null)
            {
                return tcode.TemplatePropCodeId;
            }
            return -1;
        }

        private int GetPropertyId(int groupId, int codeId)
        {
            var property = Entities.WF_FlowPropertys.FirstOrDefault(
                fp => fp.StatusId != 0 && fp.FlowGroupId == groupId && fp.Code == codeId);
            if (property != null)
                return property.FlowPropertyId;
            return -1;
        }

        private Dictionary<string, int> GetPdcProperties(int flowCaseId)
        {
            var flowCase = Entities.WF_FlowCases.FirstOrDefault(fc => fc.StatusId > 0 && fc.FlowCaseId == flowCaseId);
            var flow = Entities.WF_Flows.FirstOrDefault(f => f.FlowId == flowCase.FlowId);
            if (flow != null)
            {
                var result = Entities.WF_FlowPropertys.Where(
                                        fp => fp.StatusId == -1 && fp.FlowGroupId == flow.FlowGroupId && fp.WF_TemplatePropCode.TemplateType == (int)FlowTemplateType.UserDataChangeTemplate).Select(fp => new
                                        {
                                            Code = fp.WF_TemplatePropCode.PropCode,
                                            PropertyId = fp.FlowPropertyId
                                        });
                Dictionary<string, int> dict = new Dictionary<string, int>();
                foreach (var rt in result)
                {
                    dict.Add(rt.Code, rt.PropertyId);
                }
                return dict;
            }
            return null;
        }

        private Dictionary<string, int> GetLeaveProperties(int flowCaseId)
        {
            var flowCase = Entities.WF_FlowCases.FirstOrDefault(fc => fc.StatusId > 0 && fc.FlowCaseId == flowCaseId);
            var flow = Entities.WF_Flows.FirstOrDefault(f => f.FlowId == flowCase.FlowId);
            if (flow != null)
            {
                var result = Entities.WF_FlowPropertys.Where(
                                        fp => fp.StatusId == -1 && fp.FlowGroupId == flow.FlowGroupId && fp.WF_TemplatePropCode.TemplateType == (int)FlowTemplateType.LeaveTemplate).Select(fp => new
                                        {
                                            Code = fp.WF_TemplatePropCode.PropCode,
                                            PropertyId = fp.FlowPropertyId
                                        });
                Dictionary<string, int> dict = new Dictionary<string, int>();
                foreach (var rt in result)
                {
                    dict.Add(rt.Code, rt.PropertyId);
                }
                return dict;
            }
            return null;
        }

        private PDFFlowCase GetPDFFlowModel(int flowCaseId)
        {
            var pdfModel = new PDFFlowCase
            {
                FlowInfo = GetFlowAndCase(flowCaseId),
                PropertiesValue = GetProperties(flowCaseId),
                Attachments = GetAttachments(flowCaseId),
                CaseLogs = GetCaseLogs(flowCaseId).Union(GetCaseNotificationLogs(flowCaseId)).OrderBy(p => p.Created).ToArray(),
                Comments = NotificationManager.GetComments(flowCaseId)
            };
            pdfModel.FlowType = Entities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == pdfModel.FlowInfo.FlowTypeId);
            var prop = pdfModel.PropertiesValue.PropertyInfo.FirstOrDefault(p => p.PropertyName.ToLower().Equals("brand") && p.StatusId < 0);
            if (prop != null)
            {
                var brand = pdfModel.PropertiesValue.Values.FirstOrDefault(p => p.PropertyId == prop.FlowPropertyId)?.StringValue;
                if (brand != null)
                {
                    pdfModel.BLSShopViews = Entities.BLSShopView.Where(s => s.Brand.ToLower().Equals(brand.ToLower()))
                                            .Select(s => new { s.ShopCode, s.ShopName }).ToDictionary(s => s.ShopCode, s => s.ShopName);
                }
            }

            return pdfModel;
        }

        public ReturnApproveResult Approve(int flowCaseId, string[] nextApprovers)
        {
            ReturnApproveResult result = new ReturnApproveResult();
            var action =
                    Entities.WF_CaseUserActions.Where(
                        p => p.FlowCaseId == flowCaseId && p.UserNo == CurrentUser && p.ActionId == 0 && p.StatusId > 0)
                        .Select(p => new
                        {
                            UserAction = p,
                            Step = p.WF_FlowSteps,
                            Group = p.WF_FlowSteps.WF_StepGroups,
                            FlowCase = p.WF_FlowCases
                        }).FirstOrDefault();
            PropertyInfo[] propertyValues = GetPropertyValues(flowCaseId);
            //using (TransactionScope scope = new TransactionScope())
            //{
            if (action == null)
            {
                result.Result = ApproveResult.InvalidUserAction;
                return result;
            }

            if (action.FlowCase == null || action.FlowCase.StatusId <= 0)
            {
                result.Result = ApproveResult.InvalidFlowCase;
                return result;
            }

            if (action.FlowCase.Approved != null || action.FlowCase.Rejected != null ||
                action.FlowCase.ReviseAbort != null)
            {
                result.Result = ApproveResult.FlowCaseTerminate;
                return result;
            }

            if (action.Step == null || action.Step.StatusId <= 0)
            {
                result.Result = ApproveResult.InvalidFlowStep;
                return result;
            }
            NextStepData nsd = GetNextStepApprovers(action.FlowCase.FlowId, propertyValues, action.FlowCase.UserNo, action.Group.StepGroupId); //#TODO
            if (nsd.EmployeeList?.Count != nextApprovers?.Length && nextApprovers?.Length > 0)
            {
                result.Result = ApproveResult.InvalidApprover;
                return result;
            }
            if (nextApprovers != null && nextApprovers.Length > 0)
            {
                for (int i = 0; i < nextApprovers.Length; i++)
                {
                    string userno = nextApprovers[i];
                    if (nsd.EmployeeList[i].All(p => p.UserNo != userno))
                    {
                        result.Result = ApproveResult.InvalidApprover;
                        return result;
                    }
                }
                result.NextApprovers = nextApprovers;
            }

            action.UserAction.ActionId = Consts.Approved;
            action.UserAction.LastUpdated = DateTime.UtcNow;
            AddCaseLog(CurrentUser, action.FlowCase, CaseLogType.AppStepApproved);

            var secretary = action.Step.WF_Secretary.Where(p => p.StatusId > 0).ToArray();
            if (secretary?.Length > 0)
            {
                if (secretary.Any(p => CurrentUser.EqualsIgnoreCase(p.UserId)))
                {
                    WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCaseId,
                        action.UserAction.FlowStepId, null, NotificationTypes.SecretaryNotification);
                    foreach (var rule in secretary)
                    {
                        if (CurrentUser.EqualsIgnoreCase(rule.UserId))
                        {
                            NotificationManager.PushNotification(notification, rule.SecretaryId,
                                NotificationSources.Secretary);
                        }
                    }
                }
            }

            bool isCurrentStepApproved = action.Group.ConditionId == Consts.ConditionAny;
            if (!isCurrentStepApproved)
            {
                var otherSteps =
                    Entities.WF_FlowSteps.Where(
                        p =>
                            p.StepGroupId == action.Group.StepGroupId &&
                            p.FlowStepId != action.UserAction.FlowStepId && p.StatusId > 0);
                isCurrentStepApproved = !otherSteps.Any();
                if (!isCurrentStepApproved)
                {
                    isCurrentStepApproved =
                        otherSteps.All(
                            p =>
                                Entities.WF_CaseUserActions.Any(
                                    q =>
                                        q.FlowCaseId == flowCaseId && q.FlowStepId == p.FlowStepId &&
                                        q.ActionId == Consts.Approved && q.StatusId > 0));
                }
            }

            //if current is finished
            if (isCurrentStepApproved)
            {
                if (nsd.NextStepGroupId == 0) //All steps finished
                {
                    action.FlowCase.Approved = DateTime.UtcNow;
                    action.FlowCase.LastUpdated = DateTime.UtcNow;
                    WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCaseId,
                        action.UserAction.FlowStepId, null, NotificationTypes.AppFinishedApproved);
                    NotificationManager.PushNotification(notification, action.FlowCase.UserNo,
                        NotificationSources.ApproverApproved);
                    NotificationManager.PushNotification(notification, CurrentUser,
                        NotificationSources.ApproverApproved, n => n.IsDissmissed = 1);
                    AddNotificationToPrevApprovers(flowCaseId, action.UserAction.FlowStepId, notification);
                    AddFinalUserNotifications(flowCaseId, notification);
                    AddCoverPersonNotifications(flowCaseId, flowCaseId, notification);
                    AddLastStepNotifyUserNotifications(action.FlowCase.FlowId, notification, propertyValues, action.FlowCase.UserNo);
                    //获取当前flowcase的templateid
                    var flowCase =
                        Entities.WF_FlowCases.FirstOrDefault(fc => fc.FlowCaseId == flowCaseId);
                    int? groupId = flowCase?.WF_Flows.FlowGroupId;
                    var flowType = Entities.WF_FlowGroups.Where(p => p.FlowGroupId == groupId)
                            .Select(p => p.WF_FlowTypes)
                            .FirstOrDefault();
                    int? templateId = flowType?.TemplateType;
                    if (templateId.HasValue)
                    {
                        //如果是overseas trip application则在最后的审核者审核以后自动创建一个overseas claim application
                        if (templateId.Value == (int)FlowTemplateType.OverSeasTripTemplate)
                        {
                            Applicant applicant = new Applicant(Entities, flowCase.UserNo);
                            CreateFlowCaseInfo info = new CreateFlowCaseInfo();
                            var userInfo = SearStaffInfo(applicant.CurrentUser);
                            //var user = Entities.Users.FirstOrDefault(p => p.UserNo == applicant.CurrentUser);
                            //info.Dep = user?.DeptCode;
                            info.Dep = userInfo?.Department;
                            info.Subject = PresetValues.OverseasClaimSubjectPrefix + " " + userInfo.StaffName;
                            PropertyInfo[] infos = new PropertyInfo[6];
                            var flowtype =
                                Entities.WF_FlowTypes.FirstOrDefault(
                                    ft => ft.TemplateType == (int)FlowTemplateType.OverSeasClaimTemplate && ft.StatusId > 0);
                            if (flowtype != null)
                            {
                                var claimGroup =
                                    Entities.WF_FlowGroups.FirstOrDefault(
                                        fg => fg.StatusId > 0 && fg.FlowTypeId == flowtype.FlowTypeId);

                                var flow =
                                    Entities.WF_Flows.FirstOrDefault(
                                        f => f.FlowGroupId == claimGroup.FlowGroupId && f.StatusId > 0);

                                if (claimGroup != null && flow != null)
                                {
                                    int i = 0;
                                    foreach (var propertyValue in propertyValues)
                                    {
                                        string code = GetPropertyCode(propertyValue);
                                        int codeId = GetOverseasClaimCodeId(code);
                                        if (codeId > 0)
                                        {
                                            int propertyId = GetPropertyId(claimGroup.FlowGroupId, codeId);
                                            PropertyInfo newInfo = new PropertyInfo();
                                            newInfo.Value = propertyValue.Value;
                                            newInfo.Type = propertyValue.Type;
                                            newInfo.Id = propertyId;
                                            infos[i] = newInfo;
                                            i++;
                                        }
                                    }
                                    info.Properties = infos;
                                    info.FlowId = flow.FlowId;
                                    applicant.SaveDraft(info);
                                }
                            }

                        }
                        else if (templateId.Value == (int)FlowTemplateType.LeaveTemplate)
                        {
                            float hours = 0f;
                            string reason = null;
                            var dict = GetLeaveProperties(flowCaseId);
                            foreach (var propertyValue in propertyValues)
                            {
                                if (dict.ContainsKey("Leave_Type") && propertyValue.Id == dict["Leave_Type"])
                                {
                                    reason = propertyValue.Value;
                                }
                                if (dict.ContainsKey("Total_Hours") && propertyValue.Id == dict["Total_Hours"])
                                {
                                    hours = float.Parse(propertyValue.Value);
                                }
                            }
                            if (hours > 0)
                            {
                                var uInfo = SearStaffInfo(flowCase.UserNo);
                                if (reason.EqualsIgnoreCaseAndBlank(PresetValues.AnnualLeave) && (flowCase.RelatedFlowCaseId == null || flowCase.RelatedFlowCaseId == 0))
                                {
                                    UpdateLeaveBalance(uInfo.Country, flowCase.UserNo, hours / 8);
                                }
                                else if (reason.EqualsIgnoreCaseAndBlank(PresetValues.AnnualLeave) && flowCase.RelatedFlowCaseId != null)
                                {
                                    UpdateLeaveBalance(uInfo.Country, flowCase.UserNo, -hours / 8);
                                }
                            }
                        }
                        //如果是User Data Change form，在通过所有审核后，需要插入或更新一条current的数据到StaffInfo表
                        else if (templateId.Value == (int)FlowTemplateType.UserDataChangeTemplate)
                        {
                            string eFirstName = null;
                            string eLastName = null;
                            string lFirstName = null;
                            string lLastName = null;
                            string address = null;
                            string cAddress = null;
                            string homeTel = null;
                            string mobile = null;
                            string bankName = null;
                            string bankAccount = null;
                            string maritalStatus = null;
                            string familyMembers = null;
                            string qualification = null;
                            string email = null;
                            DateTime effectiveDate = DateTime.UtcNow;
                            var dict = GetPdcProperties(flowCaseId);
                            foreach (var propertyValue in propertyValues)
                            {
                                if (dict.ContainsKey("PDC_EffectiveDateForChanges") && propertyValue.Id == dict["PDC_EffectiveDateForChanges"])
                                {
                                    effectiveDate = DateTime.Parse(propertyValue.Value).ToUniversalTime();
                                    continue;
                                }
                                //var model = JsonConvert.DeserializeObject<dynamic>(propertyValue.Value);
                                //if (model == null)
                                //continue;
                                if (dict.ContainsKey("PDC_EnglishFirstName") && propertyValue.Id == dict["PDC_EnglishFirstName"])
                                {
                                    eFirstName = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_EnglishLastName") && propertyValue.Id == dict["PDC_EnglishLastName"])
                                {
                                    eLastName = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_LocalLanguageFirstName") && propertyValue.Id == dict["PDC_LocalLanguageFirstName"])
                                {
                                    lFirstName = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_LocalLanguageLastName") && propertyValue.Id == dict["PDC_LocalLanguageLastName"])
                                {
                                    lLastName = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_ResidentialAddress") && propertyValue.Id == dict["PDC_ResidentialAddress"])
                                {
                                    address = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_CorrespondenceAddress") && propertyValue.Id == dict["PDC_CorrespondenceAddress"])
                                {
                                    cAddress = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_HomeTelephone") && propertyValue.Id == dict["PDC_HomeTelephone"])
                                {
                                    homeTel = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_MobileTelephone") && propertyValue.Id == dict["PDC_MobileTelephone"])
                                {
                                    mobile = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_BankName") && propertyValue.Id == dict["PDC_BankName"])
                                {
                                    bankName = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_BankAccountNumber") && propertyValue.Id == dict["PDC_BankAccountNumber"])
                                {
                                    bankAccount = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_MaritalStatus") && propertyValue.Id == dict["PDC_MaritalStatus"])
                                {
                                    maritalStatus = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_FamilyMembers") && propertyValue.Id == dict["PDC_FamilyMembers"])
                                {
                                    StringBuilder sb = new StringBuilder();
                                    //#TODO
                                    //foreach (var member in model)
                                    //{
                                    //    sb.Append(member.Value);
                                    //    sb.Append("|");
                                    //}
                                    //sb.Remove(sb.Length - 1, 1);
                                    familyMembers = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_Qualification") && propertyValue.Id == dict["PDC_Qualification"])
                                {
                                    StringBuilder sb = new StringBuilder();
                                    //#TODO
                                    //foreach (var qual in model)
                                    //{
                                    //    sb.Append(qual.Value);
                                    //    sb.Append("|");
                                    //}
                                    //sb.Remove(sb.Length - 1, 1);
                                    qualification = propertyValue.Value;
                                }
                                else if (dict.ContainsKey("PDC_EmailAddress") && propertyValue.Id == dict["PDC_EmailAddress"])
                                {
                                    email = propertyValue.Value;
                                }
                            }
                            var staffInfo =
                                Entities.StaffInfo.FirstOrDefault(
                                    si => si.StaffId.Equals(flowCase.UserNo) && si.StatusId > 0);
                            if (staffInfo != null)
                            {
                                if (eFirstName != null)
                                {
                                    staffInfo.EnglishFirstName = eFirstName;
                                }
                                if (eLastName != null)
                                {
                                    staffInfo.EnglishLastName = eLastName;
                                }
                                if (lFirstName != null)
                                {
                                    staffInfo.LocalFirstName = lFirstName;
                                }
                                if (lLastName != null)
                                {
                                    staffInfo.LocalLastName = lLastName;
                                }
                                if (address != null)
                                {
                                    staffInfo.ResidentialAddress = address;
                                }
                                if (cAddress != null)
                                {
                                    staffInfo.CorrespondenceAddress = cAddress;
                                }
                                if (homeTel != null)
                                {
                                    staffInfo.HomeTelephone = homeTel;
                                }
                                if (mobile != null)
                                {
                                    staffInfo.MobileTelephone = mobile;
                                }
                                if (bankName != null)
                                {
                                    staffInfo.BankName = bankName;
                                }
                                if (bankAccount != null)
                                {
                                    staffInfo.BankAccountNumber = bankAccount;
                                }
                                if (maritalStatus != null)
                                {
                                    staffInfo.MaritalStatus = maritalStatus;
                                }
                                if (familyMembers != null)
                                {
                                    staffInfo.FamilyMembers = familyMembers;
                                }
                                if (qualification != null)
                                {
                                    staffInfo.Qualification = qualification;
                                }
                                if (effectiveDate != null)
                                {
                                    staffInfo.EffectiveDate = effectiveDate;
                                }
                                staffInfo.LastUpdated = DateTime.UtcNow;
                            }
                            else
                            {
                                Entities.StaffInfo.Add(new StaffInfo
                                {
                                    Created = DateTime.UtcNow,
                                    LastUpdated = DateTime.UtcNow,
                                    StatusId = 1,
                                    StaffId = flowCase.UserNo,
                                    EnglishFirstName = eFirstName,
                                    EnglishLastName = eLastName,
                                    LocalFirstName = lFirstName,
                                    LocalLastName = lLastName,
                                    ResidentialAddress = address,
                                    CorrespondenceAddress = cAddress,
                                    HomeTelephone = homeTel,
                                    MobileTelephone = mobile,
                                    BankName = bankName,
                                    BankAccountNumber = bankAccount,
                                    MaritalStatus = maritalStatus,
                                    FamilyMembers = familyMembers,
                                    Qualification = qualification,
                                    EmailAddress = email,
                                    EffectiveDate = effectiveDate
                                });
                            }
                        }
                    }
                }
                else
                {
                    if (nextApprovers != null)
                    {
                        EmailService.SendWorkFlowEmail(
                            Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == CurrentUser)?.EmployeeName,
                            nextApprovers, action.FlowCase.Subject, null);
                        NotificationSender.PushInboxMessages(nextApprovers, flowCaseId);
                        for (int i = 0; i < nsd.NextSteps.Length; i++)
                        {
                            WF_FlowSteps currentStep = nsd.NextSteps[i];
                            string currentUser = nextApprovers[i];
                            if (currentStep.ApproverType == (int)ApproverType.Person &&
                                currentStep.UserNo.EqualsIgnoreCaseAndBlank(currentUser))
                                continue;
                            //string dep #TODO
                            var userInfo = SearStaffInfo(currentUser);
                            Entities.WF_CaseSteps.Add(new WF_CaseSteps
                            {
                                Created = DateTime.UtcNow,
                                FlowStepId = currentStep.FlowStepId,
                                Department = userInfo?.DepartmentName ?? string.Empty,
                                //Department = Entities.Users.FirstOrDefault(p => p.UserNo == currentUser)?.DeptCode,
                                UserNo = currentUser,
                                FlowCaseId = flowCaseId,
                                LastUpdated = DateTime.UtcNow,
                                StatusId = 1
                            });
                        }
                        for (int i = 0; i < nsd.NextSteps.Length; i++)
                        {
                            AddCaseUserAction(action.FlowCase, nsd.NextSteps[i].FlowStepId, nextApprovers[i]);
                        }
                    }
                }
            }

            if (action.FlowCase.Approved == null)
            {
                WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCaseId,
                    action.UserAction.FlowStepId, null, NotificationTypes.ApproveApp);
                AddNotificationToStepGroup(action.Group.StepGroupId, notification, propertyValues, action.FlowCase.UserNo);
                NotificationManager.PushNotification(notification, action.FlowCase.UserNo,
                    NotificationSources.ApproverApproved);
                NotificationManager.PushNotification(notification, CurrentUser, NotificationSources.ApproverApproved,
                    n => n.IsDissmissed = 1);
            }
            else
            {
                try
                {
                    using (WorkFlowApiClient client = new WorkFlowApiClient())
                    {
                        var fileBytes =
                            ApprovedCasePdfExporter.GenerateFlowCase(GetPDFFlowModel(action.FlowCase.FlowCaseId),
                                CurrentUser);
                        var fileName = $"PDF FORM {action.FlowCase.Subject}-{action.FlowCase.FlowCaseId}.pdf";
                        string ret = client.UploadFile(fileName, Convert.ToBase64String(fileBytes));
                        JObject obj = JObject.Parse(ret);
                        AddCaseAttachment(action.FlowCase,
                            new Attachment
                            {
                                FileName = obj["fileName"].ToString(),
                                OriFilName = fileName,
                                FileSize = fileBytes.Length
                            });
                    }
                }
                catch (Exception ex)
                {
                    Singleton<ILogWritter>.Instance?.WriteLog("generate pdf error", ex.Message, $"flowcaseid: {action.FlowCase.FlowCaseId}");
                }
            }
            action.FlowCase.CurrentStepGroup = action.Group.StepGroupId;

            try
            {
                Entities.SaveChanges();
                //scope.Complete();
            }
            catch (Exception e)
            {
                result.Result = ApproveResult.Error;
                return result;
            }

            if (action.FlowCase.Approved != null)
            {
                using (WorkFlowApiClient client = new WorkFlowApiClient())
                {
                    client.ArchiveApproved(action.FlowCase.FlowCaseId);
                }
                result.Result = ApproveResult.FlowApproved;
            }
            else
            {
                result.Result = ApproveResult.Approved;
            }
            //}
            AutoApprove(action.FlowCase.FlowCaseId, action.FlowCase.FlowId, propertyValues, action.FlowCase.UserNo, action.Group.StepGroupId);
            var subsequents = GetSubsequentSteps(action.FlowCase.FlowId, propertyValues, action.FlowCase.UserNo,
                                                     action.Group.StepGroupId);
            if (nextApprovers != null && nextApprovers.Length == 1 && subsequents != null && subsequents.Count > 0 && subsequents[0].EmployeeList != null)
            {
                var firstList = subsequents[0].EmployeeList[0];
                string autoNextApprover = firstList[0].UserNo;
                bool shouldAutoApprove = false;
                foreach (var nextStepData in subsequents)
                {
                    var employee = nextStepData.EmployeeList[0].First();
                    //因为只添加了只有一个审批者的步骤
                    if (nextApprovers.Contains(employee.UserNo))
                    {
                        shouldAutoApprove = true;
                        break;
                    }
                }
                if (shouldAutoApprove)
                {
                    Approver approver = new Approver(Entities, nextApprovers[0]);
                    approver.Approve(flowCaseId, new string[] { autoNextApprover });
                }
            }
            return result;
        }

        public ApproveResult Reject(int flowCaseId, string message)
        {
            //using (TransactionScope scope = new TransactionScope())
            //{
                var action = Entities.WF_CaseUserActions.Where(p => p.FlowCaseId == flowCaseId && p.UserNo == CurrentUser && p.ActionId == 0 && p.StatusId > 0).Select(p => new
                {
                    UserAction = p,
                    Step = p.WF_FlowSteps,
                    Group = p.WF_FlowSteps.WF_StepGroups,
                    FlowCase = p.WF_FlowCases
                }).FirstOrDefault();
                if (action == null)
                    return ApproveResult.InvalidUserAction;

                if (action.FlowCase == null || action.FlowCase.StatusId <= 0)
                    return ApproveResult.InvalidFlowCase;

                if (action.FlowCase.Approved != null || action.FlowCase.Rejected != null || action.FlowCase.ReviseAbort != null)
                    return ApproveResult.FlowCaseTerminate;

                action.UserAction.ActionId = Consts.Rejected;
                action.UserAction.LastUpdated = DateTime.UtcNow;
                action.UserAction.Comments = message; //#TODO
                AddCaseLog(CurrentUser, action.FlowCase, CaseLogType.AppStepRejected);

                bool isFullyRejected = action.Group.ConditionId == Consts.ConditionAll;
                if (!isFullyRejected)
                {
                    var otherStepsQuery = Entities.WF_FlowSteps.Where(p => p.StepGroupId == action.Group.StepGroupId && p.FlowStepId != action.UserAction.FlowStepId && p.StatusId > 0);
                    isFullyRejected = !otherStepsQuery.Any();
                    if (!isFullyRejected)
                    {
                        isFullyRejected = otherStepsQuery.All(p => Entities.WF_CaseUserActions.Any(q => q.FlowCaseId == flowCaseId && q.FlowStepId == p.FlowStepId && q.ActionId == Consts.Rejected && q.StatusId > 0));
                    }
                }

                if (isFullyRejected)
                {
                    action.FlowCase.Rejected = DateTime.UtcNow;
                    action.FlowCase.LastUpdated = DateTime.UtcNow;
                    WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCaseId, action.UserAction.FlowStepId, message, NotificationTypes.AppFinishedRejected);
                    NotificationManager.PushNotification(notification, action.FlowCase.UserNo, NotificationSources.ApproverRejected);
                    NotificationManager.PushNotification(notification, CurrentUser, NotificationSources.ApproverRejected, n => n.IsDissmissed = 1);
                    AddNotificationToPrevApprovers(flowCaseId, action.UserAction.FlowStepId, notification);
                    AddFinalUserNotifications(flowCaseId, notification);
                }//#TODO send message to step notification users ?
                else
                {
                    WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCaseId, action.UserAction.FlowStepId, message, NotificationTypes.RejectApp);
                    AddNotificationToStepGroup(action.Group.StepGroupId, notification, GetPropertyValues(flowCaseId), action.FlowCase.UserNo);
                    NotificationManager.PushNotification(notification, CurrentUser, NotificationSources.ApproverRejected, n => n.IsDissmissed = 1);
                }
                action.FlowCase.CurrentStepGroup = action.Group.StepGroupId;
                Entities.SaveChanges();
                //scope.Complete();
                if (isFullyRejected)
                    return ApproveResult.FlowRejected;
                return ApproveResult.Rejected;
          //  }
        }

        public ApproveResult Abort(int flowCaseId, string message)
        {
            //using (TransactionScope scope = new TransactionScope())
            //{
                var action = Entities.WF_CaseUserActions.Where(p => p.FlowCaseId == flowCaseId && p.UserNo == CurrentUser && p.ActionId == 0 && p.StatusId > 0).Select(p => new
                {
                    UserAction = p,
                    Step = p.WF_FlowSteps,
                    FlowCase = p.WF_FlowCases
                }).FirstOrDefault();

                if (action == null)
                    return ApproveResult.InvalidUserAction;

                if (action.FlowCase == null || action.FlowCase.StatusId <= 0)
                    return ApproveResult.InvalidFlowCase;

                if (action.FlowCase.Approved != null || action.FlowCase.Rejected != null || action.FlowCase.ReviseAbort != null)
                    return ApproveResult.FlowCaseTerminate;

                if (action.Step == null || action.Step.StatusId <= 0)
                    return ApproveResult.InvalidFlowStep;

                action.UserAction.ActionId = Consts.Aborted;
                action.UserAction.LastUpdated = DateTime.UtcNow;
                action.UserAction.Comments = message;
                AddCaseLog(CurrentUser, action.FlowCase, CaseLogType.AppStepAbort);
                //AddFinalUserNotifications(flowCaseId);
                WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCaseId, action.Step.FlowStepId, message, NotificationTypes.AppFinishedAbort);
                AddNotificationToStepGroup(action.Step.StepGroupId, notification, GetPropertyValues(flowCaseId), action.FlowCase.UserNo);
                NotificationManager.PushNotification(notification, action.FlowCase.UserNo, NotificationSources.ApproverAbort);
                NotificationManager.PushNotification(notification, CurrentUser, NotificationSources.ApproverAbort, n => n.IsDissmissed = 1);
                AddNotificationToPrevApprovers(flowCaseId, action.UserAction.FlowStepId, notification);

                action.FlowCase.ReviseAbort = DateTime.UtcNow;
                action.FlowCase.LastUpdated = DateTime.UtcNow;
                action.FlowCase.CurrentStepGroup = action.Step.StepGroupId;
                Entities.SaveChanges();
               // scope.Complete();
                return ApproveResult.Aborted;
           // }
        }

        public CommentStepResult CommentStep(int flowCaseId, string comments, bool? ccToPrev)
        {
            WF_FlowCases flowCase = Entities.WF_FlowCases.AsNoTracking().FirstOrDefault(p => p.FlowCaseId == flowCaseId);
            if (flowCase == null || flowCase.StatusId <= 0)
                return CommentStepResult.InvalidCase;

            if (flowCase.Approved != null || flowCase.Rejected != null || flowCase.Canceled != null)
                return CommentStepResult.CaseTerminate;

            WF_CaseUserActions userAction = Entities.WF_CaseUserActions.AsNoTracking().FirstOrDefault(p => p.ActionId == 0 && p.StatusId > 0 && p.UserNo == CurrentUser);
            if (userAction == null || !userAction.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                return CommentStepResult.NotAllowed;

            WF_CaseNotifications message = NotificationManager.CreateNotification(flowCaseId, userAction.FlowStepId, comments, NotificationTypes.Comments);
            string[] users = Entities.WF_CaseUserActions.AsNoTracking().Where(p => p.FlowCaseId == flowCaseId && p.StatusId > 0 && p.UserNo != CurrentUser).Select(p => p.UserNo).ToArray();
            //把comment的statusid设置成2，这样在获取notification的时候如果只获取statusid=1的，就不会获取到comment的notification，参考ApplicationUser.GetNotifications
            NotificationManager.PushNotification(message, flowCase.UserNo, NotificationSources.ApproverCommentted, p =>
             {
                 p.IsDissmissed = 1;
                 p.StatusId = 2;
             });
            if (ccToPrev == true)
            {
                foreach (string userno in users)
                {
                    NotificationManager.PushNotification(message, userno, NotificationSources.ApproverCommentted);
                }
            }
            Entities.SaveChanges();
            return CommentStepResult.Success;
        }
    }
}
