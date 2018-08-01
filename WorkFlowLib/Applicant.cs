using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dreamlab.Core;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;

namespace WorkFlowLib
{
    public class Applicant : ApplicationUser
    {
        public Applicant(WorkFlowEntities entities, string applicantUser) : base(entities, applicantUser)
        {

        }

        public (CreateFlowResult result, int flowCaseId) CreateFlowCase(CreateFlowCaseInfo info, int version = 0, int? baseId = null)
        {
            NextStepData nsd = GetNextStepApprovers(info.FlowId, info.Properties, CurrentUser);
            if ((nsd.EmployeeList.Count == 0 || nsd.EmployeeList.Count != info.Approver.Length) && nsd.NextSteps != null && nsd.NextSteps.Length > 0)
            {
                return (CreateFlowResult.InvalidSelectedApprovers, 0);
            }
            if (info.Approver != null)
            {
                for (int i = 0; i < info.Approver.Length; i++)
                {
                    string userno = info.Approver[i];
                    if (nsd.EmployeeList[i].All(p => p.UserNo != userno))
                    {
                        return (CreateFlowResult.InvalidSelectedApprovers, 0);
                    }
                }
            }
            //没有step的也允许创建，并且创建的时候设置通过了审核
            WF_FlowCases flowCase = new WF_FlowCases
            {
                Created = DateTime.UtcNow,
                Deadline = info.Deadline?.ToUniversalTime(),
                Department = info.Dep,
                LastUpdated = DateTime.UtcNow,
                LastChecked = DateTime.UtcNow,
                FlowId = info.FlowId,
                Subject = info.Subject,
                UserNo = CurrentUser,
                IsDissmissed = 0,
                IsRead = 0,
                StatusId = 1,
                Ver = version,
                BaseFlowCaseId = baseId,
                RelatedFlowCaseId = info.RelatedCaseId
            };
            Entities.WF_FlowCases.Add(flowCase);
            AddCaseLog(CurrentUser, flowCase, CaseLogType.AppCreated);
            if (info.Attachments != null)
            {
                foreach (Attachment att in info.Attachments)
                    AddCaseAttachment(flowCase, att);
            }
            AddFlowProperties(flowCase, info.Properties);
            AddCoverDuties(info.CoverDuties, flowCase);
            AddFinalNotifyUser(info.NotifyUsers, flowCase);
            bool hasSteps = true;
            if (nsd.NextSteps != null && nsd.NextSteps.Length > 0)
            {
                for (int i = 0; i < nsd.NextSteps.Length; i++)
                {
                    WF_FlowSteps currentStep = nsd.NextSteps[i];
                    string currentUser = info.Approver[i];
                    if (currentStep.ApproverType == (int)ApproverType.Person && currentStep.UserNo.EqualsIgnoreCaseAndBlank(currentUser))
                        continue;
                    UserStaffInfo userInfo = SearStaffInfo(currentUser);
                    Entities.WF_CaseSteps.Add(new WF_CaseSteps
                    {
                        Created = DateTime.UtcNow,
                        FlowStepId = currentStep.FlowStepId,
                        Department = userInfo?.DepartmentName ?? string.Empty,
                        UserNo = currentUser,
                        WF_FlowCases = flowCase,
                        LastUpdated = DateTime.UtcNow,
                        StatusId = 1
                    });
                }
                for (int i = 0; i < nsd.NextSteps.Length; i++)
                {
                    AddCaseUserAction(flowCase, nsd.NextSteps[i].FlowStepId, info.Approver[i]);
                }
            }
            else
            {
                hasSteps = false;
                flowCase.Approved = DateTime.UtcNow;
                flowCase.IsDissmissed = 1;
            }
            if (!hasSteps)
            {
                WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCase, null, null, NotificationTypes.AppFinishedApproved);
                NotificationManager.PushNotification(notification, CurrentUser, NotificationSources.ApproverApproved);
            }
            bool hasNotifyUsers = Entities.WF_ApplicantNotificationUsers.Local.Any();
            if (hasNotifyUsers)
            {
                WF_CaseNotifications applicantNotification = NotificationManager.CreateNotification(flowCase, null, null, NotificationTypes.ApplicantNotification);
                PropertyInfo[] propertyInfos = info.Properties
                    .Where(p => p.Value != null)
                    .ToArray();
                AddApplicantNotification(info.FlowId, applicantNotification, propertyInfos, CurrentUser);
            }

            try
            {
                Entities.SaveChanges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return (CreateFlowResult.InvalidData, 0);
            }

            AutoApprove(flowCase.FlowCaseId, info.FlowId, info.Properties, CurrentUser);
            List<NextStepData> subsequents = GetSubsequentSteps(info.FlowId, info.Properties, CurrentUser);
            if (info.Approver != null && info.Approver.Length == 1 && subsequents != null && subsequents.Count > 0 && subsequents[0].EmployeeList != null)
            {
                Employee[] firstList = subsequents[0].EmployeeList[0];
                string autoNextApprover = firstList[0].UserNo;
                bool shouldAutoApprove = false;
                foreach (var nextStepData in subsequents)
                {
                    Employee employee = nextStepData.EmployeeList[0].First();
                    //因为只添加了只有一个审批者的步骤
                    if (info.Approver.Contains(employee.UserNo))
                    {
                        shouldAutoApprove = true;
                        break;
                    }
                }
                if (shouldAutoApprove)
                {
                    Approver approver = new Approver(Entities, info.Approver[0]);
                    approver.Approve(flowCase.FlowCaseId, new string[] { autoNextApprover });
                }
            }
            return (CreateFlowResult.Success, flowCase.FlowCaseId);
        }

        public CancelFlowResult Cancel(int flowCaseId)
        {
            WF_FlowCases flowCase = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == flowCaseId);
            if (flowCase == null || flowCase.StatusId < 0)
            {
                return CancelFlowResult.InvalidFlowCase;
            }
            if (flowCase.Approved != null || flowCase.Rejected != null || flowCase.ReviseAbort != null || flowCase.Canceled != null)
            {
                return CancelFlowResult.InvalidFlowCaseStatus;
            }
            if (!flowCase.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
            {
                return CancelFlowResult.InvalidUser;
            }
            if (flowCase.StatusId == 0) // is draft ?
            {
                flowCase.StatusId = Consts.ApplicationDeleted;
                Entities.SaveChanges();
                return CancelFlowResult.Canceled;
            }
            flowCase.LastUpdated = DateTime.UtcNow;
            flowCase.Canceled = DateTime.UtcNow;
            flowCase.StatusId = Consts.ApplicationCancelled;
            AddCaseLog(CurrentUser, flowCase, CaseLogType.AppCancelled);
            WF_CaseNotifications not = NotificationManager.CreateNotification(flowCaseId, null, null,
                NotificationTypes.CancelApp);
            NotificationManager.PushNotification(not, flowCase.UserNo, NotificationSources.ApplicantCancelled, p => p.IsDissmissed = 1);//Dissmissed the message of cancelled app for current user.
            AddNotificationToPrevApprovers(flowCaseId, 0, not);
            Entities.SaveChanges();

            if (flowCase.Approved != null)
                return CancelFlowResult.Error;
            return CancelFlowResult.Canceled;
        }

        public (CreateFlowResult result, int flowCaseId) SaveDraft(CreateFlowCaseInfo info)
        {
            WF_FlowCases flowCase = new WF_FlowCases
            {
                Created = DateTime.UtcNow,
                Deadline = info.Deadline?.ToUniversalTime(),
                Department = info.Dep,
                LastUpdated = DateTime.UtcNow,
                LastChecked = DateTime.UtcNow,
                FlowId = info.FlowId,
                Subject = info.Subject,
                UserNo = CurrentUser,
                IsDissmissed = 0,
                IsRead = 0,
                StatusId = 0,
                Ver = 0,
                BaseFlowCaseId = null
            };
            Entities.WF_FlowCases.Add(flowCase);
            if (info.Attachments != null)
            {
                foreach (Attachment att in info.Attachments)
                {
                    Entities.WF_FlowCases_Attachments.Add(new WF_FlowCases_Attachments()
                    {
                        Created = DateTime.UtcNow,
                        FileName = att.FileName,
                        OriFileName = att.OriFilName,
                        FileSize = att.FileSize,
                        WF_FlowCases = flowCase,
                        LastUpdated = DateTime.UtcNow,
                        StatusId = 1
                    });
                }
            }
            AddFlowProperties(flowCase, info.Properties);
            AddFinalNotifyUser(info.NotifyUsers, flowCase);
            AddCoverDuties(info.CoverDuties, flowCase);
            bool success = Entities.SaveChanges() > 0;
            if (success)
            {
                return (CreateFlowResult.Success, flowCase.FlowCaseId);
            }
            return (CreateFlowResult.InvalidData, 0);
        }
    }
}