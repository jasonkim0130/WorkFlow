using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Dreamlab.Core;
using Unity;
using Unity.Interception.Utilities;
using WorkFlow.Service;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.DTO.Query;
using WorkFlowLib.Logic;
using WorkFlowLib.Parameters;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 1/3/2017 5:32:50 PM.
    */

    public class ApplicationUser
    {
        public INotificationSender NotificationSender { get; set; }
        public WorkFlowEntities Entities { get; private set; }
        public string CurrentUser { get; private set; }
        public NotificationManager NotificationManager { get; set; }

        public ApplicationUser(WorkFlowEntities entities, string user)
        {
            Entities = entities;
            CurrentUser = user;
            NotificationSender = new PushService();
            NotificationManager = new NotificationManager(entities, user) { NotificationSender = NotificationSender };
        }

        public IQueryable<WF_CaseUserActions> InboxQuery
        {
            get
            {
                return Entities.WF_CaseUserActions.Where(p =>
                    ((CurrentUser == "Admin" || p.UserNo == CurrentUser)
                     && p.StatusId > 0
                     && p.ActionId == 0
                     && p.WF_FlowCases.StatusId > 0
                     && p.WF_FlowCases.Approved == null
                     && p.WF_FlowCases.Rejected == null
                     && p.WF_FlowCases.ReviseAbort == null)
                    || (p.StatusId > 0
                        && p.WF_FlowCases.ReviseAbort != null
                        && p.ActionId == Consts.Aborted
                        && p.WF_FlowCases.UserNo == CurrentUser
                        && p.WF_FlowCases.StatusId > 0));
            }
        }

        public GlobalUserView GetUser(string userno)
        {
            return Entities.GlobalUserView.Where(p => p.EmpStatus == "A").FirstOrDefault(p => p.EmployeeID == userno);
        }

        public UserStaffInfo SearStaffInfo(string userno)
        {
            using (IUserManager um = Singleton<IUnityContainer>.Instance.Resolve<IUserManager>())
            {
                return um.SearchStaff(userno);
            }
        }

        public bool UpdateLeaveBalance(string country, string staffId, float days)
        {
            using (IUserManager um = Singleton<IUnityContainer>.Instance.Resolve<IUserManager>())
            {
                return um.UpdateLeaveBalance(country, staffId, days);
            }
        }

        public InboxQueryRow[] GetInboxApplications()
        {
            return InboxQuery.Select(p => new InboxQueryRow
            {
                FlowCaseId = p.WF_FlowCases.FlowCaseId,
                Department = p.WF_FlowCases.Department,
                Applicant = p.WF_FlowCases.UserNo,
                Type = p.WF_FlowCases.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                Subject = p.WF_FlowCases.Subject,
                Deadline = p.WF_FlowCases.Deadline,
                Ver = p.WF_FlowCases.Ver,
                Flag = p.WF_FlowCases.IsFlagged,
                Created = p.WF_FlowCases.Created,
                Received = p.Created,
                Aborted = p.WF_FlowCases.ReviseAbort,
                IsRead = p.StatusId == 2,
                HasAttachment = p.WF_FlowCases.WF_FlowCases_Attachments.Any(q => q.StatusId > 0)
            }).ToArray();
        }

        public FlowInfo GetFlowInfo(int flowId)
        {
            return GetFlowInfoQuery().FirstOrDefault(p => p.FlowId == flowId);
        }

        public FlowConditionInfo GetFlowConditions(int flowId)
        {
            return Entities.WF_Flows.Where(p => p.StatusId != 0 && p.FlowId == flowId)
                    .AsNoTracking()
                    .Select(p => new FlowConditionInfo
                    {
                        FlowId = p.FlowId,
                        FlowConditions = p.WF_FlowConditions.Where(q => q.StatusId > 0),
                        StepGroupConditons = p.WF_StepGroups.Where(q => q.StatusId > 0)
                                .SelectMany(q => q.WF_StepGroupConditions)
                                .Where(q => q.StatusId > 0),
                    }).FirstOrDefault();
        }

        public FlowConditionInfo[] GetFlowConditionsByGroupId(int flowGroupId)
        {
            return
                Entities.WF_Flows.Where(p => p.StatusId != 0 && p.FlowGroupId == flowGroupId)
                    .AsNoTracking()
                    .Select(p => new FlowConditionInfo
                    {
                        FlowId = p.FlowId,
                        FlowConditions = p.WF_FlowConditions.Where(q => q.StatusId > 0),
                        StepGroupConditons = p.WF_StepGroups.Where(q => q.StatusId > 0)
                            .SelectMany(q => q.WF_StepGroupConditions)
                            .Where(q => q.StatusId > 0),
                    }).ToArray();
        }

        public Dictionary<string, string> GetArchivePathsByGroupId(int flowGroupId)
        {
            return Entities.WF_CountryArchivePaths.Where(p => p.FlowGroupId == flowGroupId)
                    .AsNoTracking()
                    .ToDictionary(k => k.CountryCode, v => v.ApprovedArchivePath);
        }

        public WF_StepGroupConditions[] GetStepGroupConditions(int stepGroupId)
        {
            return Entities.WF_StepGroupConditions.Where(q => q.StatusId > 0 && q.StepGroupId == stepGroupId).ToArray();
        }

        protected IQueryable<FlowInfo> GetFlowInfoQuery()
        {
            IQueryable<WF_Flows> query = Entities.WF_Flows.Where(p => p.StatusId > 0);
            return query.Select(p => new FlowInfo
            {
                FlowId = p.FlowId,
                FlowTypeId = p.WF_FlowGroups.FlowTypeId,
                FlowGroupId = p.WF_FlowGroups.FlowGroupId,
                TypeName = p.WF_FlowGroups.WF_FlowTypes.Name,
                Title = p.Title,
                ConditionRelation = p.ConditionRelation,
                GroupVersion = p.WF_FlowGroups.Version,
                BaseFlowId = p.BaseFlowId,
                StepGroups =
                    p.WF_StepGroups.Where(q => q.StatusId > 0).OrderBy(q => q.OrderId).Select(q => new StepGroup
                    {
                        StepConditionId = q.ConditionId,
                        StepGroupId = q.StepGroupId,
                        OrderId = q.OrderId,
                        Steps =
                            q.WF_FlowSteps.Where(s => s.StatusId > 0).OrderBy(s => s.OrderId).Select(s => new FlowStep
                            {
                                Approver = s.UserNo,
                                FlowStepId = s.FlowStepId,
                                Department = s.Department,
                                ApproverType = s.ApproverType,
                                CriteriaGradeOperator = s.CriteriaGradeOperator,
                                CriteriaGrade = s.CriteriaGrade,
                                OrderId = s.OrderId,
                                NoApprover = s.WF_NoApproverConditions.Any(k => k.StatusId > 0)
                            }),
                        NotificationUsers =
                            q.WF_StepNotificateUsers.Where(t => t.StatusId > 0).Select(t => new StepNotificationUser
                            {
                                UserNo = t.UserNo,
                                NotificateUserId = t.StepNotificateUserId,
                                NotifyUserType = t.NotifyUserType,
                                CriteriaGradeOperator = t.CriteriaGradeOperator,
                                CriteriaGrade = t.CriteriaGrade,
                                UserRole = t.UserRole,
                                ManagerOption = t.ManagerOption,
                                ManagerLevelOperator = t.ManagerLevelOperator,
                                ManagerLevel = t.ManagerLevel,
                                ManagerMaxLevel = t.ManagerMaxLevel,
                                CountryType = t.CountryType,
                                DeptType = t.DeptType,
                                BrandType = t.BrandType,
                                FixedCountry = t.FixedCountry,
                                FixedDept = t.FixedDept,
                                FixedBrand = t.FixedBrand,
                                DeptTypeSource = t.DeptTypeSource,
                                FixedDeptType = t.FixedDeptType,
                                StatusId = t.StatusId
                            })
                    }),
                ApplicantNotificationUsers =
                    p.WF_ApplicantNotificationUsers
                    .Where(q => q.StatusId > 0)
                    .Select(u => new ApplicantNotificationUser
                    {
                        UserNo = u.UserNo,
                        NotificateUserId = u.ApplicationNotificationUserId,
                        NotifyUserType = u.NotifyUserType,
                        CriteriaGradeOperator = u.CriteriaGradeOperator,
                        CriteriaGrade = u.CriteriaGrade,
                        UserRole = u.UserRole,
                        ManagerOption = u.ManagerOption,
                        ManagerLevelOperator = u.ManagerLevelOperator,
                        ManagerLevel = u.ManagerLevel,
                        ManagerMaxLevel = u.ManagerMaxLevel,
                        CountryType = u.CountryType,
                        DeptType = u.DeptType,
                        BrandType = u.BrandType,
                        FixedCountry = u.FixedCountry,
                        FixedDept = u.FixedDept,
                        FixedBrand = u.FixedBrand,
                        DeptTypeSource = u.DeptTypeSource,
                        FixedDeptType = u.FixedDeptType,
                        StatusId = u.StatusId
                    }),
                LastStepNotifyUsers =
                    p.WF_LastStepNotifyUser
                    .Where(q => q.StatusId > 0)
                    .Select(u => new LastStepNotifyUser
                    {
                        UserNo = u.UserNo,
                        NotificateUserId = u.LastStepNotifyUserId,
                        NotifyUserType = u.NotifyUserType,
                        CriteriaGradeOperator = u.CriteriaGradeOperator,
                        CriteriaGrade = u.CriteriaGrade,
                        UserRole = u.UserRole,
                        ManagerOption = u.ManagerOption,
                        ManagerLevelOperator = u.ManagerLevelOperator,
                        ManagerLevel = u.ManagerLevel,
                        ManagerMaxLevel = u.ManagerMaxLevel,
                        CountryType = u.CountryType,
                        DeptType = u.DeptType,
                        BrandType = u.BrandType,
                        FixedCountry = u.FixedCountry,
                        FixedDept = u.FixedDept,
                        FixedBrand = u.FixedBrand,
                        DeptTypeSource = u.DeptTypeSource,
                        FixedDeptType = u.FixedDeptType,
                        StatusId = u.StatusId
                    })
            });
        }

        public FlowCaseInfo GetFlowCaseInfo(int flowCaseId)
        {
            var query =
                Entities.WF_FlowCases.Where(p => p.FlowCaseId == flowCaseId)
                    .Select(p => new FlowCaseInfo
                    {
                        FlowCaseId = p.FlowCaseId,
                        FlowId = p.FlowId,
                        Department = p.Department,
                        Approved = p.Approved,
                        Rejected = p.Rejected,
                        Deadline = p.Deadline,
                        Aborted = p.ReviseAbort,
                        Cancelled = p.Canceled,
                        Applicant = p.UserNo,
                        Subject = p.Subject,
                        Ver = p.Ver,
                        BaseFlowCaseId = p.BaseFlowCaseId,
                        RelatedFlowCaseId = p.RelatedFlowCaseId,
                        Created = p.Created,
                        LastUpdated = p.LastUpdated,
                        LastApproverViewed = p.LastApproverViewed,
                        LastChecked = p.LastChecked,
                        StepResults = p.WF_CaseUserActions.Where(q => q.StatusId > 0).Select(q => new StepResult
                        {
                            FlowStepId = q.FlowStepId,
                            Status = q.ActionId
                        }),
                        CoverDuties = p.WF_CaseCoverUsers.Where(q => q.FlowCaseId == flowCaseId && q.StatusId > 0).Select(q => q.UserNo),
                        IsDraft = p.StatusId == 0
                    });
            return query.FirstOrDefault();
        }

        public void AddCaseAttachment(WF_FlowCases flowCase, Attachment att)
        {
            Entities.WF_FlowCases_Attachments.Add(new WF_FlowCases_Attachments
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

        public void AddCaseLog(string userNo, WF_FlowCases flowCase, CaseLogType logtype)
        {
            Entities.WF_CaseLogs.Add(new WF_CaseLogs
            {
                Created = DateTime.UtcNow,
                WF_FlowCases = flowCase,
                LastUpdated = DateTime.UtcNow,
                StatusId = 1,
                UserNo = userNo,
                LogTypeId = (int)logtype
            });
        }

        public void AddCaseLog(string userNo, int flowCaseId, CaseLogType logtype)
        {
            Entities.WF_CaseLogs.Add(new WF_CaseLogs
            {
                Created = DateTime.UtcNow,
                FlowCaseId = flowCaseId,
                LastUpdated = DateTime.UtcNow,
                StatusId = 1,
                UserNo = userNo,
                LogTypeId = (int)logtype
            });
        }

        public void AddFinalUserNotifications(int flowCaseId, WF_CaseNotifications notification)
        {
            foreach (string finaluser in
                    Entities.WF_CaseNotificateUsers.Where(p => p.FlowCaseId == flowCaseId && p.StatusId > 0)
                        .Select(p => p.UserNo))
            {
                if (finaluser.EqualsIgnoreCaseAndBlank(CurrentUser))
                    continue;
                NotificationManager.PushNotification(notification, finaluser, NotificationSources.FinalNotifyUser);
            }
        }

        public void AddCoverPersonNotifications(int flowCaseId, int flowId, WF_CaseNotifications notification)
        {
            foreach (var coverUser in Entities.WF_CaseCoverUsers.Where(p => p.FlowCaseId == flowCaseId && p.StatusId > 0))
            {
                if (coverUser.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                    continue;
                NotificationManager.PushNotification(notification, coverUser.UserNo, NotificationSources.CoverPerson);
            }
        }

        public void AddLastStepNotifyUserNotifications(int flowId, WF_CaseNotifications notification, PropertyInfo[] propertyValues, string applicant)
        {
            foreach (WF_LastStepNotifyUser item in Entities.WF_LastStepNotifyUser.Where(p => p.FlowId == flowId && p.StatusId > 0))
            {
                if (item.NotifyUserType == (int)ApproverType.Person)
                {
                    if (item.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                        continue;
                    //#TODO
                    NotificationManager.PushNotification(notification, item.UserNo,
                        NotificationSources.LastStepNotificateUser);
                    continue;
                }

                UserResolver ur = Singleton<IUnityContainer>.Instance.Resolve<UserResolver>();
                ur.UserType = item.NotifyUserType;
                ur.CurrentUser = CurrentUser;
                ur.PropertyValues = propertyValues;
                ur.SelectedUser = item.UserNo;
                ur.SelectedUsername =
                    Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == item.UserNo)?.EmployeeName;
                ur.Operator = item.CriteriaGradeOperator;
                ur.Grade = item.CriteriaGrade;
                ur.Applicant = applicant;
                ur.ManagerLevel = item.ManagerLevel;
                ur.ManagerMaxLevel = item.ManagerMaxLevel;
                ur.ManagerOption = item.ManagerOption;
                ur.ManagerLevelOperator = item.ManagerLevelOperator;
                ur.UserRole = item.UserRole;
                ur.CountryType = item.CountryType;
                ur.DeptType = item.DeptType;
                ur.BrandType = item.BrandType;
                ur.FixedCountry = item.FixedCountry;
                ur.FixedBrand = item.FixedBrand;
                ur.FixedDept = item.FixedDept;
                ur.DeptTypeSource = item.DeptTypeSource;
                ur.FixedDeptType = item.FixedDeptType;
                Employee[] users = ur.FindUser();
                if (users != null)
                {
                    foreach (Employee empoyee in users)
                    {
                        if (empoyee.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                            continue;
                        NotificationManager.PushNotification(notification, empoyee.UserNo,
                            NotificationSources.LastStepNotificateUser);
                    }
                }
            }
        }

        public void AddCaseUserAction(WF_FlowCases flowCase, int flowStepId, string userno)
        {
            Entities.WF_CaseUserActions.Add(new WF_CaseUserActions
            {
                WF_FlowCases = flowCase,
                ActionId = 0,
                Created = DateTime.UtcNow,
                FlowStepId = flowStepId,
                UserNo = userno,
                LastUpdated = DateTime.UtcNow,
                StatusId = 1
            });
            AddCaseLog(userno, flowCase, CaseLogType.AppAssignApprover);
        }

        public void AddCoverDuties(string[] coverDuties, WF_FlowCases flowCase)
        {
            if (coverDuties == null || coverDuties.Length == 0)
            {
                return;
            }
            foreach (string coverDuty in coverDuties.Distinct(StringComparer.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(coverDuty))
                    continue;
                if (coverDuty.EqualsIgnoreCaseAndBlank(CurrentUser))
                    continue;
                Entities.WF_CaseCoverUsers.Add(new WF_CaseCoverUsers
                {
                    WF_FlowCases = flowCase,
                    LastUpdated = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    StatusId = 1,
                    UserNo = coverDuty
                });
            }
        }

        public void AddFinalNotifyUser(string[] notifyUsers, WF_FlowCases flowCase)
        {
            if (notifyUsers == null || notifyUsers.Length == 0)
                return;
            foreach (string item in notifyUsers.Distinct(StringComparer.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;
                if (item.EqualsIgnoreCaseAndBlank(CurrentUser))
                    continue;
                Entities.WF_CaseNotificateUsers.Add(new WF_CaseNotificateUsers
                {
                    WF_FlowCases = flowCase,
                    LastUpdated = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    StatusId = 1,
                    UserNo = item
                });
            }
        }

        public void AddFlowProperties(WF_FlowCases flowCase, PropertyInfo[] properties)
        {
            if (properties == null || properties.Length == 0)
                return;
            foreach (var property in properties)
            {
                if (property.Value == null)
                    continue;
                WF_CasePropertyValues newProp = new WF_CasePropertyValues
                {
                    PropertyId = property.Id,
                    WF_FlowCases = flowCase,
                    LastUpdated = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    StatusId = 1
                };
                if (string.IsNullOrWhiteSpace(property.Value))
                    continue;
                switch (property.Type)
                {
                    case 1:
                        newProp.StringValue = property.Value;
                        break;
                    case 2:
                        {
                            if (int.TryParse(property.Value, out var v))
                                newProp.IntValue = v;
                            else
                                newProp.StringValue = property.Value;
                            break;
                        }
                    case 3:
                        if (DateTime.TryParse(property.Value, out var v1))
                            newProp.DateTimeValue = v1;
                        else
                            newProp.StringValue = property.Value;
                        break;
                    case 4:
                        if (decimal.TryParse(property.Value, out var v2))
                            newProp.NumericValue = v2;
                        else
                            newProp.StringValue = property.Value;
                        break;
                    case 5:
                        if (DateTime.TryParse(property.Value, out var v3))
                            newProp.DateValue = v3;
                        else
                            newProp.StringValue = property.Value;
                        break;
                    case 6:
                        newProp.UserNoValue = property.Value;
                        break;
                    case 7:
                        newProp.TextValue = property.Value;
                        break;
                    default:
                        newProp.StringValue = property.Value;
                        break;
                }
                Entities.WF_CasePropertyValues.Add(newProp);
            }
        }

        //########################################################################################################

        //自动审批被跳过的步骤和NO approver的步骤
        public void AutoApprove(int flowCaseId, int flowId, PropertyInfo[] propertyValues, string applicant,
            int? currentGroupId = null)
        {
            Dictionary<int, List<WF_FlowSteps>> stepDict = GetJumpedSteps(flowId, propertyValues, applicant, currentGroupId);
            if (stepDict == null)
            {
                return;
            }
            Dictionary<int, List<WF_FlowSteps>>.KeyCollection keys = stepDict.Keys;
            var flowCase = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == flowCaseId && p.StatusId > 0);
            if (flowCase == null)
            {
                return;
            }
            var lastGroupId = Entities.WF_StepGroups.AsNoTracking().Where(p => p.FlowId == flowId && p.StatusId > 0).OrderByDescending(p => p.OrderId).Select(p => p.StepGroupId).FirstOrDefault();
            foreach (var key in keys)
            {
                foreach (var step in stepDict[key])
                {
                    //using (TransactionScope scope = new TransactionScope())
                    //{
                        Entities.WF_CaseUserActions.Add(
                        new WF_CaseUserActions
                        {
                            FlowCaseId = flowCaseId,
                            FlowStepId = step.FlowStepId,
                            ActionId = Consts.Approved,
                            UserNo = "SYSTEM",
                            Created = DateTime.UtcNow,
                            LastUpdated = DateTime.UtcNow,
                            StatusId = 1
                        });
                        AddCaseLog("SYSTEM", flowCase, CaseLogType.AppStepApproved);

                        Entities.WF_CaseSteps.Add(new WF_CaseSteps
                        {
                            Created = DateTime.UtcNow,
                            FlowStepId = step.FlowStepId,
                            Department = string.Empty,
                            //Department = 
                            //Entities.Users.FirstOrDefault(p => p.UserNo == currentUser)?.DeptCode ?? string.Empty,
                            UserNo = "SYSTEM",
                            WF_FlowCases = flowCase,
                            LastUpdated = DateTime.UtcNow,
                            StatusId = 1
                        });

                        Entities.SaveChanges();

                        //WF_CaseNotifications notification = NotificationManager.CreateNotification(flowCaseId,
                        //    step.FlowStepId, null, NotificationTypes.ApproveApp);
                        //if (lastGroupId != key)
                        //{
                        //    AddNotificationToStepGroup(key, notification, propertyValues, flowCase.UserNo);
                        //}
                        //NotificationManager.PushNotification(notification, flowCase.UserNo,
                        //    NotificationSources.ApproverApproved);

                      //  scope.Complete();
                    //}
                }
            }
        }

        public void AddNotificationToStepGroup(int stepGroupId, WF_CaseNotifications notification,
            PropertyInfo[] propertyValues, string applicant)
        {
            WF_StepNotificateUsers[] notifyUsers =
                Entities.WF_StepNotificateUsers.Where(p => p.StepGroupId == stepGroupId && p.StatusId > 0)
                    .AsNoTracking()
                    .ToArray();
            if (notifyUsers.Length > 0)
            {
                foreach (WF_StepNotificateUsers item in notifyUsers)
                {
                    if (item.NotifyUserType == (int)ApproverType.Person)
                    {
                        if (item.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                            continue;
                        //#TODO
                        NotificationManager.PushNotification(notification, item.UserNo,
                            NotificationSources.StepNotificateUser);
                        continue;
                    }

                    UserResolver ur = Singleton<IUnityContainer>.Instance.Resolve<UserResolver>();
                    ur.UserType = item.NotifyUserType;
                    ur.CurrentUser = CurrentUser;
                    ur.PropertyValues = propertyValues;
                    ur.SelectedUser = item.UserNo;
                    //ur.SelectedUsername = Entities.Users.FirstOrDefault(p => p.UserNo == item.UserNo)?.Username;
                    ur.SelectedUsername =
                        Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == item.UserNo)?.EmployeeName;
                    ur.Operator = item.CriteriaGradeOperator;
                    ur.Grade = item.CriteriaGrade;
                    ur.Applicant = applicant;
                    ur.ManagerLevel = item.ManagerLevel;
                    ur.ManagerMaxLevel = item.ManagerMaxLevel;
                    ur.ManagerOption = item.ManagerOption;
                    ur.ManagerLevelOperator = item.ManagerLevelOperator;
                    ur.UserRole = item.UserRole;
                    ur.CountryType = item.CountryType;
                    ur.DeptType = item.DeptType;
                    ur.BrandType = item.BrandType;
                    ur.FixedCountry = item.FixedCountry;
                    ur.FixedDept = item.FixedDept;
                    ur.FixedBrand = item.FixedBrand;
                    ur.DeptTypeSource = item.DeptTypeSource;
                    ur.FixedDeptType = item.FixedDeptType;
                    Employee[] users = ur.FindUser();
                    if (users != null)
                    {
                        foreach (Employee empoyee in users)
                        {
                            if (empoyee.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                                continue;
                            //#TODO
                            NotificationManager.PushNotification(notification, empoyee.UserNo,
                                NotificationSources.StepNotificateUser);
                        }
                    }
                }
            }
        }

        public void AddApplicantNotification(int flowId, WF_CaseNotifications notification,
            PropertyInfo[] propertyValues, string applicant)
        {
            WF_ApplicantNotificationUsers[] notifyUsers =
                Entities.WF_ApplicantNotificationUsers.Where(p => p.FlowId == flowId && p.StatusId > 0)
                    .AsNoTracking()
                    .ToArray();
            if (notifyUsers.Length > 0)
            {
                foreach (WF_ApplicantNotificationUsers item in notifyUsers)
                {
                    if (item.NotifyUserType == (int)ApproverType.Person)
                    {
                        if (item.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                            continue;
                        //#TODO
                        NotificationManager.PushNotification(notification, item.UserNo,
                            NotificationSources.ApplicantNotificateUser);
                        continue;
                    }

                    UserResolver ur = Singleton<IUnityContainer>.Instance.Resolve<UserResolver>();
                    ur.UserType = item.NotifyUserType;
                    ur.CurrentUser = CurrentUser;
                    ur.PropertyValues = propertyValues;
                    ur.SelectedUser = item.UserNo;
                    //ur.SelectedUsername = Entities.Users.FirstOrDefault(p => p.UserNo == item.UserNo)?.Username;
                    ur.SelectedUsername =
                        Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == item.UserNo)?.EmployeeName;
                    ur.Operator = item.CriteriaGradeOperator;
                    ur.Grade = item.CriteriaGrade;
                    ur.Applicant = applicant;
                    ur.ManagerLevel = item.ManagerLevel;
                    ur.ManagerMaxLevel = item.ManagerMaxLevel;
                    ur.ManagerOption = item.ManagerOption;
                    ur.ManagerLevelOperator = item.ManagerLevelOperator;
                    ur.UserRole = item.UserRole;
                    ur.CountryType = item.CountryType;
                    ur.DeptType = item.DeptType;
                    ur.BrandType = item.BrandType;
                    ur.FixedCountry = item.FixedCountry;
                    ur.FixedBrand = item.FixedBrand;
                    ur.FixedDept = item.FixedDept;
                    ur.DeptTypeSource = item.DeptTypeSource;
                    ur.FixedDeptType = item.FixedDeptType;
                    Employee[] users = ur.FindUser();
                    if (users != null)
                    {
                        foreach (Employee empoyee in users)
                        {
                            if (empoyee.UserNo.EqualsIgnoreCaseAndBlank(CurrentUser))
                                continue;
                            //#TODO
                            NotificationManager.PushNotification(notification, empoyee.UserNo,
                                NotificationSources.ApplicantNotificateUser);
                        }
                    }
                }
            }
        }

        public List<int> GetRelFlowCaseIds(int flowCaseId)
        {
            List<int> list = new List<int>();
            var flowCase = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == flowCaseId && p.StatusId > 0);
            if (flowCase != null)
            {
                if (flowCase.BaseFlowCaseId.HasValue)
                {
                    list.Add(flowCase.BaseFlowCaseId.Value);
                    var records = Entities.WF_FlowCases.Where(p => p.BaseFlowCaseId == flowCase.BaseFlowCaseId && (p.StatusId > 0 || p.StatusId == -2));
                    foreach (var record in records)
                    {
                        list.Add(record.FlowCaseId);
                    }
                }
            }
            return list;
        }

        public IEnumerable<CaseLog> GetCaseLogs(int flowCaseId)
        {
            var list = GetRelFlowCaseIds(flowCaseId);
            if (!list.Contains(flowCaseId))
            {
                list.Add(flowCaseId);
            }
            return Entities.WF_CaseLogs.Where(p => list.Contains(p.FlowCaseId) && p.StatusId > 0)
                .Select(p => new CaseLog
                {
                    MessageId = p.CaseLogId,
                    Created = p.Created,
                    MessageTypeId = p.LogTypeId,
                    LogType = "CaseLogs",
                    ReceiverUser = p.UserNo
                });
        }

        public IEnumerable<CaseLog> GetCaseNotificationLogs(int flowCaseId)
        {
            var list = GetRelFlowCaseIds(flowCaseId);
            if (!list.Contains(flowCaseId))
            {
                list.Add(flowCaseId);
            }
            int?[] sources =
            {
                (int) NotificationSources.ApproverCommentted,
                (int) NotificationSources.StepNotificateUser,
                (int) NotificationSources.CoverPerson,
                (int) NotificationSources.FinalNotifyUser,
                (int) NotificationSources.ApplicantNotificateUser,
                (int) NotificationSources.LastStepNotificateUser,
                (int) NotificationSources.ApproverAbort,
                (int) NotificationSources.ApproverRejected,
                (int) NotificationSources.Secretary
            };

            return Entities.WF_CaseNotificationReceivers.Where(p => list.Contains(p.WF_CaseNotifications.FlowCaseId)
                                                                    && sources.Contains(p.SourceType)
                                                                    && p.StatusId > 0)
                .Select(p => new CaseLog
                {
                    MessageId = p.CaseNotificationId,
                    Comments = p.WF_CaseNotifications.Comments,
                    Created = p.Created,
                    LogType = "Notifications",
                    MessageTypeId = p.SourceType,
                    ReceiverUser = p.Receiver,
                    SenderUser = p.WF_CaseNotifications.Sender,
                });
        }

        public WF_FlowCases GetCaseInfo(int flowCaseId)
        {
            return Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == flowCaseId);
        }

        public bool UpdateProperties(int flowcaseid, string subject, string dep, PropertyInfo[] properties)
        {
            WF_FlowCases info = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == flowcaseid);
            if (info == null) return false;
            info.Subject = subject;
            info.Department = dep;
            //#TODO
            foreach (var prop in properties)
            {
                if (prop.CasePropertyValueId.HasValue)
                {
                    var target =
                        Entities.WF_CasePropertyValues.FirstOrDefault(
                            p => p.CasePropertyValueId == prop.CasePropertyValueId && p.StatusId > 0);
                    if (target == null)
                        return false;
                    switch (prop.Type)
                    {
                        case 1:
                            target.StringValue = prop.Value;
                            break;
                        case 2:
                            target.IntValue = int.Parse(prop.Value);
                            break;
                        case 3:
                            target.DateTimeValue = DateTime.Parse(prop.Value);
                            break;
                        case 4:
                            target.NumericValue = decimal.Parse(prop.Value);
                            break;
                        case 5:
                            target.DateValue = DateTime.Parse(prop.Value);
                            break;
                        case 6:
                            target.UserNoValue = prop.Value;
                            break;
                        case 7:
                            target.TextValue = prop.Value;
                            break;
                    }
                }
                else
                {
                    var newProp = new WF_CasePropertyValues
                    {
                        PropertyId = prop.Id,
                        FlowCaseId = flowcaseid,
                        LastUpdated = DateTime.UtcNow,
                        Created = DateTime.UtcNow,
                        StatusId = 1
                    };
                    switch (prop.Type)
                    {
                        case 1:
                            newProp.StringValue = prop.Value;
                            break;
                        case 2:
                            newProp.IntValue = int.Parse(prop.Value);
                            break;
                        case 3:
                            newProp.DateTimeValue = DateTime.Parse(prop.Value);
                            break;
                        case 4:
                            newProp.NumericValue = decimal.Parse(prop.Value);
                            break;
                        case 5:
                            newProp.DateValue = DateTime.Parse(prop.Value);
                            break;
                        case 6:
                            newProp.UserNoValue = prop.Value;
                            break;
                        case 7:
                            newProp.TextValue = prop.Value;
                            break;
                    }
                }
            }
            return Entities.SaveChanges() > 0;
        }

        public int SelectFlow(int flowTypeId, CreateFlowCaseInfo caseInfo)
        {
            int[] flowIdArray =
                Entities.WF_Flows.Where(
                    p => p.WF_FlowGroups.FlowTypeId == flowTypeId && p.StatusId > 0 && p.WF_FlowGroups.StatusId > 0)
                    .Select(p => p.FlowId).ToArray();
            var conditions = Entities.WF_Flows.Where(p => flowIdArray.Contains(p.FlowId)).AsNoTracking().Select(p => new
            {
                FlowId = p.FlowId,
                Relation = p.ConditionRelation,
                FlowConditions = p.WF_FlowConditions.Where(q => q.StatusId > 0).Select(q => new
                {
                    Property = q.WF_FlowPropertys,
                    OtherProperty = q.OtherPropertyType,
                    Condition = q
                })
            }).ToArray();
            if (flowIdArray.Length == 0)
                return 0;
            int noCondition = 0;
            if (flowIdArray.Length > 0 && caseInfo.Properties?.Length > 0)
            {
                foreach (int flowId in flowIdArray)
                {
                    var conds = conditions.FirstOrDefault(p => p.FlowId == flowId);
                    if (conds != null)
                    {
                        bool isAnd = (conds.FlowConditions.Count() == 1 || string.IsNullOrWhiteSpace(conds.Relation) ||
                                      conds.Relation.EqualsIgnoreCaseAndBlank("And"));
                        bool isValid = isAnd;
                        foreach (var cond in conds.FlowConditions)
                        {
                            FlowCondition fc = cond.Property != null
                                ? new FlowCondition(cond.Property, cond.Condition.Operator,
                                    cond.Condition.Value)
                                : new FlowCondition(cond.OtherProperty, cond.Condition.Operator,
                                    cond.Condition.Value, CurrentUser);
                            if (isAnd && !fc.Check(caseInfo.Properties))
                            {
                                isValid = false;
                                break;
                            }
                            if (!isAnd && fc.Check(caseInfo.Properties))
                            {
                                isValid = true;
                                break;
                            }
                        }
                        if (isValid)
                        {
                            return flowId;
                        }
                    }
                    else
                    {
                        if (noCondition == 0)
                            noCondition = flowId;
                    }
                }
            }

            return noCondition;
        }

        public FlowInfo GetFlowAndCase(int flowCaseId)
        {
            FlowCaseInfo flowCase = GetFlowCaseInfo(flowCaseId);
            FlowInfo flow = null;
            if (flowCase != null)
            {
                flow = GetFlowInfo(flowCase.FlowId);
                flow.Initialize(flowCase,
                    Entities.WF_CaseSteps.Where(p => p.FlowCaseId == flowCaseId && p.StatusId > 0)
                        .AsNoTracking()
                        .ToArray()/*, 
                    Entities.WF_CaseUserActions.Where( p=> p.FlowCaseId == flowCaseId && p.StatusId > 0 && p.ActionId > 0).ToDictionary(p=>p.FlowStepId,p=> Entities.GlobalUserViews.FirstOrDefault( gu => p.UserNo.Equals(gu.EmployeeID))?.EmployeeName)*/);
            }
            return flow;
        }

        public PropertiesValue GetProperties(int flowCaseId)
        {
            var query = Entities.WF_CasePropertyValues.Where(p => p.FlowCaseId == flowCaseId && p.StatusId > 0).Select(
                p => new
                {
                    PropertyInfo = p.WF_FlowPropertys,
                    Values = p
                }).ToArray();
            return new PropertiesValue
            {
                PropertyInfo = query.Select(p => p.PropertyInfo),
                Values = query.Select(p => p.Values)
            };
        }

        public PropertiesValue GetStorePropertiesForClosure(string shopCode)
        {
            var flowCase =
                Entities.WF_FlowCases.Where(
                    p =>
                    p.StatusId > 0 &&
                    p.WF_CasePropertyValues.Any(
                        cv =>
                        cv.StatusId > 0 && cv.WF_FlowPropertys.PropertyName.Equals(Consts.ShopCodePropertyName) && cv.WF_FlowPropertys.StatusId == -1 &&
                        cv.TextValue.Equals(shopCode)));
            return flowCase.Select(p => new PropertiesValue
            {
                PropertyInfo = p.WF_Flows.WF_FlowGroups.WF_FlowPropertys.Where(q => q.StatusId == -1).OrderBy(q => q.OrderId),
                Values = p.WF_CasePropertyValues.Where(q => q.StatusId > 0)
            }).FirstOrDefault();
        }

        public WF_FlowCases_Attachments[] GetAttachments(int flowCaseId)
        {
            return
                Entities.WF_FlowCases_Attachments.Where(p => p.FlowCaseId == flowCaseId)
                    .Where(p => p.StatusId > 0).AsNoTracking()
                    .ToArray();
        }

        public bool SetCaseAsViewed(int flowCaseId, string viewer)
        {
            WF_CaseUserActions userAction =
                Entities.WF_CaseUserActions.FirstOrDefault(
                    p => p.FlowCaseId == flowCaseId && p.UserNo == viewer && p.StatusId == 1 && p.ActionId == 0);
            if (userAction != null)
            {
                userAction.StatusId = 2;
                Entities.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.WF_FlowCases SET LastApproverViewed =Getutcdate() WHERE FlowCaseId=@p0;", flowCaseId);
                AddCaseLog(viewer, flowCaseId, CaseLogType.AppViewed);
            }
            return Entities.SaveChanges() > 0;
        }

        public bool SetCaseAsUnread(int flowCaseId, string viewer)
        {
            WF_CaseUserActions userAction =
                Entities.WF_CaseUserActions.FirstOrDefault(
                    p => p.FlowCaseId == flowCaseId && p.UserNo == viewer && p.StatusId == 2 && p.ActionId == 0);
            if (userAction != null)
            {
                userAction.StatusId = 1;
            }
            return Entities.SaveChanges() > 0;
        }

        public int CountPending()
        {
            try
            {
                return PendingQuery.Count();
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public int CountNotifications()
        {
            try
            {
                return
                    Entities.WF_CaseNotificationReceivers.Count(
                        p =>
                            p.IsDissmissed == 0 && p.StatusId > 0 && p.Receiver == CurrentUser);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public int CountDraft()
        {
            try
            {
                return DraftQuery.Count();
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public IQueryable<WF_FlowCases> PendingQuery
        {
            //send back for revision的不再出现在pending里面
            get
            {
                return
                    Entities.WF_FlowCases.Where(
                        p => p.StatusId > 0 && ((p.Approved == null && p.Rejected == null) || p.ReviseAbort != null) &&
                             (p.UserNo == CurrentUser ||
                              p.WF_CaseUserActions.Any(q => q.UserNo == CurrentUser && q.StatusId > 0)));
            }
        }

        public IQueryable<WF_FlowCases> DraftQuery
        {
            get
            {
                return
                    Entities.WF_FlowCases.Where(
                        p => p.StatusId == 0 && ((p.Approved == null && p.Rejected == null) || p.ReviseAbort != null) &&
                             (p.UserNo == CurrentUser ||
                              p.WF_CaseUserActions.Any(q => q.UserNo == CurrentUser && q.StatusId > 0)));
            }
        }

        public string[] GetFinalNotifyUsers(int flowCaseId)
        {
            return Entities.WF_CaseNotificateUsers.Where(p => p.FlowCaseId == flowCaseId)
                .Where(p => p.StatusId > 0).AsNoTracking().Select(p => p.UserNo)
                .ToArray();
        }

        public CaseHistory[] GetCaseHistory(int caseId, int? baseId)
        {
            if (baseId.HasValue)
            {
                return Entities.WF_FlowCases.Where(p =>
                    p.FlowCaseId == baseId || (p.BaseFlowCaseId == baseId && p.FlowCaseId != caseId))
                    .OrderByDescending(p => p.FlowCaseId)
                    .Select(p => new CaseHistory
                    {
                        Ver = p.Ver,
                        Created = p.Created,
                        LastUpdated = p.LastUpdated,
                        Subject = p.Subject,
                        FlowCaseId = p.FlowCaseId
                    }).ToArray();
            }
            return null;
        }

        public CaseHistory[] GetCaseHistory(int caseId)
        {
            var flow = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == caseId);
            if (flow?.BaseFlowCaseId != null)
            {
                var baseId = flow.BaseFlowCaseId;
                return Entities.WF_FlowCases.Where(p =>
                    p.FlowCaseId == baseId || (p.BaseFlowCaseId == baseId && p.FlowCaseId != caseId))
                    .OrderByDescending(p => p.FlowCaseId)
                    .Select(p => new CaseHistory
                    {
                        Ver = p.Ver,
                        Created = p.Created,
                        LastUpdated = p.LastUpdated,
                        Subject = p.Subject,
                        FlowCaseId = p.FlowCaseId
                    }).ToArray();
            }
            return null;
        }

        public void AddNotificationToPrevApprovers(int flowCaseId, int currentFlowStepId,
            WF_CaseNotifications notification)
        {
            //#TODO
            foreach (
                var receiver in
                    Entities.WF_CaseUserActions.Where(
                        p =>
                            p.FlowCaseId == flowCaseId && p.FlowStepId != currentFlowStepId && p.StatusId > 0 &&
                            p.ActionId > 0).Select(p => p.UserNo).ToArray())
            {
                if (receiver.EqualsIgnoreCaseAndBlank(CurrentUser))
                    continue;
                NotificationManager.PushNotification(notification, receiver, NotificationSources.ToPrevApprovers);
            }
        }

        public void MarkFlowAsObsolete(int flowCaseId)
        {
            Entities.Database.ExecuteSqlCommand(
                @"UPDATE dbo.WF_FlowCases SET LastUpdated =Getutcdate(),StatusID =@p1 WHERE FlowCaseId=@p0;", flowCaseId,
                Consts.ApplicationObsoleted);
            AddCaseLog(CurrentUser, flowCaseId, CaseLogType.AppRevised);
        }

        public IQueryable<NotificationQueryRow> GetUndismissedNotifications()
        {
            return GetNotifications(false);
        }

        public IQueryable<NotificationQueryRow> GetDismissedNotifications()
        {
            return GetNotifications(true);
        }

        private IQueryable<NotificationQueryRow> GetNotifications(bool isDissmissed)
        {
            //statusid=2的表示comment的notification，这些不需要返回
            int value = isDissmissed ? 1 : 0;
            return Entities.WF_CaseNotificationReceivers.Where(
                p =>
                    p.IsDissmissed == value && p.StatusId == 1 && p.WF_CaseNotifications.StatusId > 0 &&
                    p.Receiver == CurrentUser)
                .Select(p => new NotificationQueryRow
                {
                    CaseNotificationReceiverId = p.CaseNotificationReceiverId,
                    IsRead = p.IsRead,
                    FlowId = p.WF_CaseNotifications.WF_FlowCases.FlowId,
                    NotificationType = p.WF_CaseNotifications.NotificationType,
                    Applicant = p.WF_CaseNotifications.WF_FlowCases.UserNo,
                    Created = p.Created,
                    Deadline = p.WF_CaseNotifications.WF_FlowCases.Deadline,
                    FlowCaseId = p.WF_CaseNotifications.WF_FlowCases.FlowCaseId,
                    Department = p.WF_CaseNotifications.WF_FlowCases.Department,
                    Approved = p.WF_CaseNotifications.WF_FlowCases.Approved,
                    Rejected = p.WF_CaseNotifications.WF_FlowCases.Rejected,
                    Cancelled = p.WF_CaseNotifications.WF_FlowCases.Canceled,
                    Aborted = p.WF_CaseNotifications.WF_FlowCases.ReviseAbort,
                    Subject = p.WF_CaseNotifications.WF_FlowCases.Subject,
                    Type = p.WF_CaseNotifications.WF_FlowCases.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    Ver = p.WF_CaseNotifications.WF_FlowCases.Ver,
                    LastUpdated = p.WF_CaseNotifications.WF_FlowCases.LastUpdated,
                    SourceType = p.SourceType,
                    Sender = p.WF_CaseNotifications.Sender,
                    HasAttachment =
                        p.WF_CaseNotifications.WF_FlowCases.WF_FlowCases_Attachments.Any(q => q.StatusId > 0)
                });
        }

        public NotificationQueryRow[] GetFinishedApplications()
        {
            return Entities.WF_FlowCases.Where(
                p => (p.Canceled != null || p.Approved != null || p.Rejected != null) && p.UserNo == CurrentUser)
                .Select(p => new NotificationQueryRow
                {
                    Source = "MyFinishedApplication",
                    CaseNotificationReceiverId = 0,
                    Applicant = p.UserNo,
                    Created = p.Created,
                    FlowCaseId = p.FlowCaseId,
                    Department = p.Department,
                    Approved = p.Approved,
                    Rejected = p.Rejected,
                    Cancelled = p.Canceled,
                    Aborted = p.ReviseAbort,
                    Subject = p.Subject,
                    Type = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    Ver = p.Ver
                }).ToArray();
        }

        public NotificationQueryRow[] GetParticipatedApplications()
        {
            return Entities.WF_CaseUserActions.Where(
                p => p.ActionId > 0 && p.StatusId > 0 && p.UserNo == CurrentUser)
                .Select(p => new NotificationQueryRow
                {
                    CaseNotificationReceiverId = 0,
                    Source = "Participated",
                    Applicant = p.WF_FlowCases.UserNo,
                    Created = p.WF_FlowCases.Created,
                    FlowCaseId = p.FlowCaseId,
                    Department = p.WF_FlowCases.Department,
                    Approved = p.WF_FlowCases.Approved,
                    Rejected = p.WF_FlowCases.Rejected,
                    Cancelled = p.WF_FlowCases.Canceled,
                    Aborted = p.WF_FlowCases.ReviseAbort,
                    Subject = p.WF_FlowCases.Subject,
                    Type = p.WF_FlowCases.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    Ver = p.WF_FlowCases.Ver
                }).ToArray();
        }


        public int CountInbox()
        {
            try
            {
                return InboxQuery.Count();
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public FlowCaseIdLastUpdatedPair[] GetAllRelatedFlowCaseId()
        {
            var inbox =
                InboxQuery.GroupBy(p => p.FlowCaseId).Select(
                    p =>
                        new FlowCaseIdLastUpdatedPair
                        {
                            flowCaseId = p.Key,
                            lastUpdated = p.FirstOrDefault().WF_FlowCases.LastUpdated
                        })
                    .ToArray();
            var notification =
                GetUndismissedNotifications().GroupBy(p => p.FlowCaseId)
                    .Select(p => new FlowCaseIdLastUpdatedPair
                    {
                        flowCaseId = p.Key,
                        lastUpdated = p.FirstOrDefault().LastUpdated
                    })
                    .ToArray();
            DateTime ignoreDate = DateTime.UtcNow.AddDays(-30);
            var archive =
                GetDismissedNotifications().Where(p => p.LastUpdated > ignoreDate).GroupBy(p => p.FlowCaseId)
                    .Select(p => new FlowCaseIdLastUpdatedPair
                    {
                        flowCaseId = p.Key,
                        lastUpdated = p.FirstOrDefault().LastUpdated
                    })
                    .ToArray();
            var pending =
                PendingQuery.GroupBy(p => p.FlowCaseId).Select(
                    p => new FlowCaseIdLastUpdatedPair
                    {
                        flowCaseId = p.Key,
                        lastUpdated = p.FirstOrDefault().LastUpdated
                    })
                    .ToArray();
            var draft = DraftQuery.GroupBy(p => p.FlowCaseId).Select(p => new FlowCaseIdLastUpdatedPair
            {
                flowCaseId = p.Key,
                lastUpdated = p.FirstOrDefault().LastUpdated
            }).ToArray();
            return inbox.Union(notification.Union(archive.Union(pending.Union(draft)))).ToArray();
        }

        public bool UpdateLastChecked(int flowCaseId, string viewer)
        {
            WF_FlowCases target =
                Entities.WF_CaseUserActions.Where(
                    q => q.FlowCaseId == flowCaseId && q.UserNo == viewer && q.StatusId > 0)
                    .Select(p => p.WF_FlowCases)
                    .FirstOrDefault();
            if (target != null)
            {
                target.LastChecked = DateTime.UtcNow;
            }
            return Entities.SaveChanges() > 0;
        }

        public int GetCurrentStepGroupId(int flowCaseId)
        {
            return
                Entities.WF_CaseUserActions.Where(
                    p => p.FlowCaseId == flowCaseId && p.UserNo == CurrentUser && p.StatusId > 0 && p.ActionId == 0)
                    .Select(p => p.WF_FlowSteps.StepGroupId)
                    .FirstOrDefault();
        }

        public NextStepData GetNextStepApprovers(int flowCaseId, int? currentStepGroupId = null)
        {
            var propertyValues = GetPropertyValues(flowCaseId);
            var flow =
                Entities.WF_FlowCases.Where(p => p.FlowCaseId == flowCaseId)
                    .Select(p => new { p.FlowId, p.UserNo, p.CurrentStepGroup })
                    .FirstOrDefault();
            return GetNextStepApprovers(flow.FlowId, propertyValues, flow.UserNo,
                currentStepGroupId ?? flow.CurrentStepGroup);
        }

        public PropertyInfo[] GetPropertyValues(int flowCaseId)
        {
            var propertyValues = Entities.WF_CasePropertyValues.Where(p => p.StatusId > 0 && p.FlowCaseId == flowCaseId)
                .Select(p => new
                {
                    CasePropertyValueId = p.CasePropertyValueId,
                    Id = p.PropertyId,
                    Type = p.WF_FlowPropertys.PropertyType,
                    StringValue = p.StringValue,
                    IntValue = p.IntValue,
                    NumericValue = p.NumericValue,
                    DateValue = p.DateValue,
                    DateTimeValue = p.DateTimeValue,
                    TextValue = p.TextValue,
                    UserNoValue = p.UserNoValue,
                }).ToArray().Select(p =>
                {
                    string value = p.StringValue;
                    switch (p.Type)
                    {
                        case 2:
                            value = p.IntValue?.ToString();
                            break;
                        case 3:
                            value = p.DateTimeValue?.ToString("yyyy-MM-dd HH:mm");
                            break;
                        case 4:
                            value = p.NumericValue?.ToString();
                            break;
                        case 5:
                            value = p.DateValue?.ToString("yyyy-MM-dd");
                            break;
                        case 6:
                            value = p.UserNoValue;
                            break;
                        case 7:
                            value = p.TextValue;
                            break;
                    }
                    return new PropertyInfo
                    {
                        CasePropertyValueId = p.CasePropertyValueId,
                        Id = p.Id,
                        Type = p.Type,
                        Value = value
                    };
                });
            return propertyValues.ToArray();
        }

        private bool IsApproverRequired(int groupCondition, WF_FlowSteps[] steps, PropertyInfo[] propertyValues, string applicant)
        {
            bool noNeed = false;
            foreach (WF_FlowSteps step in steps)
            {
                bool skip = false;
                var noApproverConditionses = step.WF_NoApproverConditions.Where(p => p.StatusId > 0);
                if (noApproverConditionses.Any())
                {
                    foreach (var c in noApproverConditionses)
                    {
                        FlowCondition fc = null;
                        if (c.NoApproverDataKey == (int)ExtraProperty.ApplicantGrade)
                        {
                            fc = new FlowCondition("grade", c.NoApproverOperator, c.NoApproverValue,
                                c.NoApproverMaxValue, applicant);
                        }
                        else if (c.NoApproverDataKey == (int)ExtraProperty.ApproverGrade)
                        {
                            fc = new FlowCondition("grade", c.NoApproverOperator, c.NoApproverValue,
                                c.NoApproverMaxValue, CurrentUser);
                        }
                        else if (c.NoApproverDataKey == (int)ExtraProperty.ApproverStaffNo)
                        {
                            fc = new FlowCondition("approverno", c.NoApproverOperator, c.NoApproverValue,
                                c.NoApproverMaxValue, CurrentUser);
                        }
                        else
                        {
                            WF_FlowPropertys property = Entities.WF_FlowPropertys.FirstOrDefault(t => t.FlowPropertyId == c.NoApproverDataKey);
                            fc = new FlowCondition(property, c.NoApproverOperator, c.NoApproverValue);
                        }
                        if (fc.Check(propertyValues))
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (groupCondition == 1)
                {
                    if (skip == false)
                    {
                        return false;
                    }
                    noNeed = true;
                }
                else
                {
                    if (skip)
                    {
                        return true;
                    }
                }
            }
            return noNeed;
        }

        public Dictionary<int, List<WF_FlowSteps>> GetJumpedSteps(int flowId, PropertyInfo[] propertyValues, string applicant,
            int? currentGroupId)
        {
            var query = Entities.WF_StepGroups.AsNoTracking().Where(p => p.FlowId == flowId && p.StatusId > 0);
            if (currentGroupId.HasValue)
            {
                query =
                     query.Where(
                         p =>
                             p.OrderId >
                             Entities.WF_StepGroups.Where(q => q.StepGroupId == currentGroupId.Value)
                                 .Select(q => q.OrderId)
                                 .FirstOrDefault());
            }
            var groups = query.OrderBy(p => p.OrderId).Select(p => new
            {
                p.IsAllowModify,
                p.StepGroupId,
                Conditions =
                    p.WF_StepGroupConditions.Where(q => q.StatusId > 0)
                        .Select(
                            q =>
                                new
                                {
                                    Property =
                                        Entities.WF_FlowPropertys.FirstOrDefault(t => t.FlowPropertyId == q.DataKey),
                                    Condition = q
                                }),
                Steps = p.WF_FlowSteps.Where(q => q.StatusId > 0).OrderBy(q => q.OrderId),
                //Condition == 1表示All 2表示Any
                Condition = p.ConditionId,
                p.OrderId
            }).ToArray();
            if (groups.Length == 0)
            {
                return null;
            }
            Dictionary<int, List<WF_FlowSteps>> result = new Dictionary<int, List<WF_FlowSteps>>();
            var @group = groups.FirstOrDefault();
            //查找NoApprover 的步骤起始OrderId
            var orderId = @group.OrderId;
            if (@group != null)
            {
                if (@group.Conditions.Any())
                {
                    var condictions = @group.Conditions.Where(p => p.Condition.NextStepGroupId.HasValue);
                    foreach (var con in condictions)
                    {
                        FlowCondition fc = null;
                        //条件选择的是属性以外的其它属性，比如applicant grade
                        if (con?.Property == null)
                        {
                            if (con.Condition.DataKey == (int)ExtraProperty.ApplicantGrade)
                            {
                                fc = new FlowCondition("grade", con.Condition.Operator, con.Condition.Value,
                                    con.Condition.MaxValue, applicant);
                            }
                            else if (con.Condition.DataKey == (int)ExtraProperty.ApproverGrade)
                            {
                                fc = new FlowCondition("grade", con.Condition.Operator, con.Condition.Value,
                                    con.Condition.MaxValue, CurrentUser);
                            }
                        }
                        else
                        {
                            fc = new FlowCondition(con.Property, con.Condition.Operator, con.Condition.Value);
                        }
                        if (fc.Check(propertyValues))
                        {
                            //条件通过，将此步骤加入到数组
                            result.Add(@group.StepGroupId, @group.Steps.ToList());
                            var nextStepGroup =
                                groups.FirstOrDefault(
                                    p => p.StepGroupId == con.Condition.NextStepGroupId.GetValueOrDefault());
                            if (nextStepGroup != null)
                            {
                                //将OrderId设置为下一步的OrderId
                                orderId = nextStepGroup.OrderId;
                                //将下一步和当前步骤间隔的步骤查询出来并加入到数组
                                var jumpedStepGroups =
                                    query.Where(
                                        p =>
                                            p.OrderId > @group.OrderId &&
                                            p.OrderId < orderId)
                                        .Select(
                                            p =>
                                                new
                                                {
                                                    p.StepGroupId,
                                                    Steps =
                                                        p.WF_FlowSteps.Where(q => q.StatusId > 0)
                                                            .OrderBy(q => q.OrderId),
                                                });
                                foreach (var item in jumpedStepGroups)
                                {
                                    result.Add(item.StepGroupId, item.Steps.ToList());
                                }
                            }
                            break;
                        }
                    }
                }
                //循环剩下的步骤
                foreach (var item in groups.Where(p => p.OrderId >= orderId))
                {
                    foreach (var step in item.Steps)
                    {
                        bool shouldAdd = false;
                        var noApproverConditionses = step.WF_NoApproverConditions.Where(p => p.StatusId > 0);
                        if (noApproverConditionses.Any())
                        {
                            foreach (var c in noApproverConditionses)
                            {
                                FlowCondition fc = null;
                                if (c.NoApproverDataKey == (int)ExtraProperty.ApplicantGrade)
                                {
                                    fc = new FlowCondition("grade", c.NoApproverOperator, c.NoApproverValue,
                                        c.NoApproverMaxValue, applicant);
                                }
                                else if (c.NoApproverDataKey == (int)ExtraProperty.ApproverGrade)
                                {
                                    fc = new FlowCondition("grade", c.NoApproverOperator, c.NoApproverValue,
                                        c.NoApproverMaxValue, CurrentUser);
                                }
                                else if (c.NoApproverDataKey == (int)ExtraProperty.ApproverStaffNo)
                                {
                                    fc = new FlowCondition("approverno", c.NoApproverOperator, c.NoApproverValue,
                                        c.NoApproverMaxValue, CurrentUser);
                                }
                                else
                                {
                                    var property =
                                        Entities.WF_FlowPropertys.FirstOrDefault(
                                            t => t.FlowPropertyId == c.NoApproverDataKey);
                                    fc = new FlowCondition(property, c.NoApproverOperator, c.NoApproverValue);
                                }
                                if (fc.Check(propertyValues))
                                {
                                    shouldAdd = true;
                                    break;
                                }
                            }
                        }
                        if (shouldAdd)
                        {
                            List<WF_FlowSteps> g = null;
                            if (result.ContainsKey(item.StepGroupId))
                            {
                                g = result[item.StepGroupId];
                            }
                            else
                            {
                                g = new List<WF_FlowSteps>();
                            }
                            g.Add(step);
                            result[item.StepGroupId] = g;
                        }
                    }
                    //如果本步骤不能通过NoApprover的验证，则跳出循环
                    if (!IsApproverRequired(item.Condition, item.Steps.ToArray(), propertyValues, applicant))
                    {
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取下一个审批步骤的steps，这个函数主要是为了处理连续出现Jump或者No Approver的情况
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="propertyValues"></param>
        /// <param name="applicant"></param>
        /// <param name="currentGroupId"></param>
        /// <returns></returns>
        private Pair<int, List<WF_FlowSteps>> GetValidNextSteps(int flowId, PropertyInfo[] propertyValues, string applicant, int? currentGroupId)
        {
            Pair<int, List<WF_FlowSteps>> result = null;
            List<WF_FlowSteps> steps = new List<WF_FlowSteps>();
            var query = Entities.WF_StepGroups.AsNoTracking().Where(p => p.FlowId == flowId && p.StatusId > 0);
            if (currentGroupId.HasValue)
            {
                int orderId = Entities.WF_StepGroups.Where(q => q.StepGroupId == currentGroupId.Value).Select(q => q.OrderId).FirstOrDefault();
                query = query.Where(p => p.OrderId > orderId);
            }
            var @group = query.OrderBy(p => p.OrderId).Select(p => new
            {
                p.IsAllowModify,
                p.StepGroupId,
                Conditions = p.WF_StepGroupConditions.Where(q => q.StatusId > 0).Select(q => new { Property = Entities.WF_FlowPropertys.FirstOrDefault(t => t.FlowPropertyId == q.DataKey), Condition = q }),
                Steps = p.WF_FlowSteps.Where(q => q.StatusId > 0).OrderBy(q => q.OrderId),
                //Condition == 1表示All 2表示Any
                Condition = p.ConditionId
            }).FirstOrDefault();
            if (@group != null)
            {
                if (@group.Conditions.Any())
                {
                    foreach (var con in @group.Conditions.Where(p => p.Condition.NextStepGroupId.HasValue).ToArray()) //All jump or no approver
                    {
                        FlowCondition fc = null;
                        if (con?.Property == null)
                        {
                            //条件选择的是属性以外的其它属性，比如applicant grade
                            if (con.Condition.DataKey == (int)ExtraProperty.ApplicantGrade)
                            {
                                fc = new FlowCondition("grade", con.Condition.Operator, con.Condition.Value, con.Condition.MaxValue, applicant);
                            }
                            else if (con.Condition.DataKey == (int)ExtraProperty.ApproverGrade)
                            {
                                fc = new FlowCondition("grade", con.Condition.Operator, con.Condition.Value, con.Condition.MaxValue, CurrentUser);
                            }
                        }
                        else
                        {
                            fc = new FlowCondition(con.Property, con.Condition.Operator, con.Condition.Value);
                        }
                        if (fc.Check(propertyValues))
                        {
                            var nextSteps = Entities.WF_FlowSteps.Where(q => q.StatusId > 0 && q.StepGroupId == con.Condition.NextStepGroupId.Value).OrderBy(q => q.OrderId);
                            if (IsApproverRequired(@group.Condition, nextSteps.ToArray(), propertyValues, applicant))
                            {
                                return GetValidNextSteps(flowId, propertyValues, applicant, con.Condition.NextStepGroupId);
                            }
                            return new Pair<int, List<WF_FlowSteps>>(con.Condition.NextStepGroupId ?? 0, nextSteps.ToList());
                        }
                    }
                }
                int goCount = 0;
                foreach (var step in @group.Steps)
                {
                    bool stepGo = false;
                    var noApproverConditionses = step.WF_NoApproverConditions.Where(p => p.StatusId > 0);
                    if (noApproverConditionses.Any())
                    {
                        foreach (var c in noApproverConditionses)
                        {
                            FlowCondition fc = null;
                            if (c.NoApproverDataKey == (int)ExtraProperty.ApplicantGrade)
                            {
                                fc = new FlowCondition("grade", c.NoApproverOperator, c.NoApproverValue,
                                    c.NoApproverMaxValue, applicant);
                            }
                            else if (c.NoApproverDataKey == (int)ExtraProperty.ApproverGrade)
                            {
                                fc = new FlowCondition("grade", c.NoApproverOperator, c.NoApproverValue,
                                    c.NoApproverMaxValue, CurrentUser);
                            }
                            else if (c.NoApproverDataKey == (int)ExtraProperty.ApproverStaffNo)
                            {
                                fc = new FlowCondition("approverno", c.NoApproverOperator, c.NoApproverValue,
                                    c.NoApproverMaxValue, CurrentUser);
                            }
                            else
                            {
                                var property =
                                    Entities.WF_FlowPropertys.FirstOrDefault(
                                        t => t.FlowPropertyId == c.NoApproverDataKey);
                                fc = new FlowCondition(property, c.NoApproverOperator, c.NoApproverValue);
                            }
                            if (fc.Check(propertyValues))
                            {
                                stepGo = true;
                                break;
                            }
                        }

                        if (stepGo)
                        {
                            goCount++;
                        }
                        else
                        {
                            steps.Add(step);
                        }
                    }
                    else
                    {
                        steps.Add(step);
                    }
                    if (stepGo && @group.Condition == 2)
                    {
                        return GetValidNextSteps(flowId, propertyValues, applicant, @group.StepGroupId);
                    }
                }
                if (@group.Condition == 1 && goCount == @group.Steps.Count())
                {
                    return GetValidNextSteps(flowId, propertyValues, applicant, @group.StepGroupId);
                }
                result = new Pair<int, List<WF_FlowSteps>>(@group.StepGroupId, steps);
            }
            return result;
        }

        public NextStepData GetNextStepApprovers(int flowId, PropertyInfo[] propertyValues, string applicant,
            int? currentGroupId = null)
        {
            NextStepData nsd = new NextStepData();
            var next = GetValidNextSteps(flowId, propertyValues, applicant, currentGroupId);
            if (next != null)
            {
                nsd.NextStepGroupId = next.First;
                nsd.NextSteps = next.Second.ToArray();
                foreach (WF_FlowSteps step in nsd.NextSteps)
                {
                    UserResolver ur = Singleton<IUnityContainer>.Instance.Resolve<UserResolver>();
                    ur.UserType = step.ApproverType;
                    ur.CurrentUser = CurrentUser;
                    ur.PropertyValues = propertyValues;
                    ur.SelectedUser = step.UserNo;
                    ur.SelectedUsername = Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == step.UserNo)?.EmployeeName;
                    ur.Operator = step.CriteriaGradeOperator;
                    ur.Grade = step.CriteriaGrade;
                    ur.Applicant = applicant;
                    ur.ManagerLevel = step.ManagerLevel;
                    ur.ManagerMaxLevel = step.ManagerMaxLevel;
                    ur.ManagerOption = step.ManagerOption;
                    ur.ManagerLevelOperator = step.ManagerLevelOperator;
                    ur.UserRole = step.UserRole;
                    ur.CountryType = step.CountryType;
                    ur.DeptType = step.DeptType;
                    ur.BrandType = step.BrandType;
                    ur.FixedCountry = step.FixedCountry;
                    ur.FixedBrand = step.FixedBrand;
                    ur.FixedDept = step.FixedDept;
                    ur.DeptTypeSource = step.DeptTypeSource;
                    ur.FixedDeptType = step.FixedDeptType;
                    ur.UserNameByNo = p => Entities.GlobalUserView.FirstOrDefault(u => u.EmployeeID == p)?.EmployeeName;
                    nsd.AddEmpoyees(ur.FindUser());
                }
            }
            return nsd;
        }

        /// <summary>
        ///用于自动审批通过
        ///a-b-c-b对于这种流程，需求要求b在后面还会出现，所以第一次b应该自动审核通过
        ///自动审批只适用于，后面流程里某个步骤的审批者只有一个并且跟当前下一个审批者是同一个人，并且将要自动审批通过的下一个步骤也仅有一个审批者
        /// </summary>
        /// <param name="flowId"></param>
        /// <param name="propertyValues"></param>
        /// <param name="applicant"></param>
        /// <param name="currentGroupId"></param>
        /// <returns></returns>
        public List<NextStepData> GetSubsequentSteps(int flowId, PropertyInfo[] propertyValues, string applicant, int? currentGroupId = null)
        {
            var query = Entities.WF_StepGroups
                .AsNoTracking()
                .Where(p => p.FlowId == flowId && p.StatusId > 0);
            if (currentGroupId.HasValue)
            {
                query = query
                    .Where(p => p.OrderId > Entities.WF_StepGroups
                        .Where(q => q.StepGroupId == currentGroupId.Value)
                        .Select(q => q.OrderId)
                        .FirstOrDefault());
            }
            var groups = query
                .OrderBy(p => p.OrderId)
                .Select(p => new
                {
                    p.StepGroupId,
                    Steps = p.WF_FlowSteps
                        .Where(q => q.StatusId > 0)
                        .OrderBy(q => q.OrderId)
                })
                .ToArray();
            List<NextStepData> nextStepsData = new List<NextStepData>();
            for (int i = 1; i < groups.Length; i++)
            {
                var @group = groups[i];
                NextStepData nsd = new NextStepData();
                nsd.NextStepGroupId = @group.StepGroupId;
                nsd.NextSteps = @group.Steps.ToArray();
                bool shouldAdd = false;
                foreach (WF_FlowSteps step in @group.Steps)
                {
                    UserResolver ur = Singleton<IUnityContainer>.Instance.Resolve<UserResolver>();
                    ur.UserType = step.ApproverType;
                    ur.CurrentUser = CurrentUser;
                    ur.PropertyValues = propertyValues;
                    ur.SelectedUser = step.UserNo;
                    //ur.SelectedUsername = Entities.Users.FirstOrDefault(p => p.UserNo == step.UserNo)?.Username;
                    ur.SelectedUsername =
                        Entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == step.UserNo)?.EmployeeName;
                    ur.Operator = step.CriteriaGradeOperator;
                    ur.Grade = step.CriteriaGrade;
                    ur.Applicant = applicant;
                    ur.ManagerLevel = step.ManagerLevel;
                    ur.ManagerMaxLevel = step.ManagerMaxLevel;
                    ur.ManagerOption = step.ManagerOption;
                    ur.ManagerLevelOperator = step.ManagerLevelOperator;
                    ur.UserRole = step.UserRole;
                    ur.CountryType = step.CountryType;
                    ur.DeptType = step.DeptType;
                    ur.BrandType = step.BrandType;
                    ur.FixedCountry = step.FixedCountry;
                    ur.FixedBrand = step.FixedBrand;
                    ur.FixedDept = step.FixedDept;
                    ur.DeptTypeSource = step.DeptTypeSource;
                    ur.FixedDeptType = step.FixedDeptType;
                    ur.UserNameByNo = p => Entities.GlobalUserView.FirstOrDefault(u => u.EmployeeID == p)?.EmployeeName;
                    var users = ur.FindUser();
                    if (users.Length == 1)
                    {
                        shouldAdd = true;
                    }
                    nsd.AddEmpoyees(users);
                }
                if (shouldAdd)
                {
                    nextStepsData.Add(nsd);
                }
            }
            return nextStepsData;
        }

        public bool HasSteps(int flowId)
        {
            int cnt =
                Entities.WF_StepGroups.Count(
                    sg => sg.FlowId == flowId && sg.StatusId > 0 && sg.WF_FlowSteps.Count(fs => fs.StatusId > 0) > 0);
            return cnt > 0;
        }

        public double GetUnApprovedBalance(string userNo)
        {
            var flowIds = Entities.WF_Flows.Where(
                    p =>
                        p.StatusId > 0 && p.WF_FlowGroups.StatusId > 0 && p.WF_FlowGroups.WF_FlowTypes.StatusId > 0 &&
                        p.WF_FlowGroups.WF_FlowTypes.TemplateType != null &&
                        p.WF_FlowGroups.WF_FlowTypes.TemplateType == (int)FlowTemplateType.LeaveTemplate)
                .Select(p => p.FlowId).ToArray();
            var flowCaseIds =
                Entities.WF_FlowCases.Where(p =>
                        p.StatusId > 0 && flowIds.Contains(p.FlowId) && p.UserNo.Equals(userNo) && p.Rejected == null &&
                        p.Approved == null && p.Canceled == null && p.RelatedFlowCaseId == null &&
                        p.WF_CasePropertyValues.Any(
                            q => q.WF_FlowPropertys.WF_TemplatePropCode.PropCode.Equals(PresetValues.LeaveType) &&
                                 q.StringValue.Equals(PresetValues.AnnualLeave) && q.StatusId > 0))
                    .Select(p => p.FlowCaseId)
                    .ToArray();
            if (flowCaseIds.Length == 0)
            {
                return 0;
            }

            var totalHoursPropertyIds = Entities.WF_FlowPropertys.Where(
                    p =>
                        p.StatusId == -1 && p.Code != null && p.WF_TemplatePropCode != null &&
                        p.WF_TemplatePropCode.TemplateType == (int)FlowTemplateType.LeaveTemplate &&
                        p.WF_TemplatePropCode.PropCode.Equals(PresetValues.TotalHours)).Select(p => p.FlowPropertyId)
                .ToArray();
            var properties =
                Entities.WF_CasePropertyValues.Where(
                        p =>
                            p.StatusId > 0 && totalHoursPropertyIds.Contains(p.PropertyId) &&
                            flowCaseIds.Contains(p.FlowCaseId))
                    .ToArray();
            decimal pendingHours = 0;
            foreach (var caseValue in properties)
            {
                if (caseValue.NumericValue.HasValue)
                {
                    pendingHours += caseValue.NumericValue.Value;
                }
                else if (caseValue.StringValue != null)
                {
                    pendingHours += decimal.Parse(caseValue.StringValue);
                }
            }

            return (double)pendingHours / 8;
        }

        public double GetNotStartedBalance(string userNo)
        {
            DateTime now = DateTime.Now;
            double addedDays = 0;

            var flowIds = Entities.WF_Flows.Where(
                    p =>
                        p.StatusId > 0 && p.WF_FlowGroups.StatusId > 0 && p.WF_FlowGroups.WF_FlowTypes.StatusId > 0 &&
                        p.WF_FlowGroups.WF_FlowTypes.TemplateType != null &&
                        p.WF_FlowGroups.WF_FlowTypes.TemplateType == (int)FlowTemplateType.LeaveTemplate)
                .Select(p => p.FlowId).ToArray();
            var flowCaseIds =
                Entities.WF_FlowCases.Where(p =>
                        p.StatusId > 0 && flowIds.Contains(p.FlowId) && p.UserNo.Equals(userNo) &&
                        p.Approved.HasValue && 
                        (p.RelatedFlowCaseId == null || p.RelatedFlowCaseId == 0) &&
                        p.WF_CasePropertyValues.Any(
                            q => q.WF_FlowPropertys.WF_TemplatePropCode.PropCode.Equals(PresetValues.LeaveType) &&
                                 q.StringValue.Equals(PresetValues.AnnualLeave) && q.StatusId > 0) &&
                        p.WF_CasePropertyValues.Any(q =>
                            q.WF_FlowPropertys.WF_TemplatePropCode.PropCode.Equals(PresetValues.FromDate) &&
                            q.DateValue > now && q.StatusId > 0))
                    .Select(p => p.FlowCaseId)
                    .ToArray();
            var cancelledCaseIds = Entities.WF_FlowCases
                .Where(p => p.StatusId > 0 && (p.RelatedFlowCaseId != null && p.RelatedFlowCaseId > 0) &&
                            p.Approved.HasValue)
                .Select(p => p.RelatedFlowCaseId).ToArray();
            flowCaseIds = flowCaseIds.Where(p => !cancelledCaseIds.Contains(p)).ToArray();

            var hours = Entities.WF_CasePropertyValues
                .Where(p => p.StatusId > 0 &&
                            p.WF_FlowPropertys.WF_TemplatePropCode.PropCode.Equals(PresetValues
                                .TotalHours) &&
                            flowCaseIds.Contains(p.FlowCaseId)).Sum(p => p.NumericValue);
            if (hours.HasValue)
            {
                addedDays = (double)hours.Value / 8;
            }
            return addedDays;
        }

        public bool CheckCancelLeaveBalance(PropertyInfo[] props, int flowtypeid)
        {
            if (Entities.WF_FlowTypes.First(p => p.FlowTypeId == flowtypeid && p.StatusId > 0).TemplateType != 2)
                return true;
            var group = Entities.WF_FlowGroups.FirstOrDefault(p =>
                p.StatusId > 0 && p.WF_FlowTypes.StatusId > 0 && p.WF_FlowTypes.TemplateType == 2);
            if (group == null)
                return false;
            int type_id = group.WF_FlowPropertys.First(p => p.PropertyName == "LeaveCode")
                .FlowPropertyId;
            if (props.First(p => p.Id == type_id).Value != "CANCEL LEAVE")
                return true;
            int fromdate_id = group.WF_FlowPropertys.First(p => p.PropertyName == "FromDate")
                .FlowPropertyId;
            int todate_id = group.WF_FlowPropertys.First(p => p.PropertyName == "ToDate")
                .FlowPropertyId;
            int fromtime_id = group.WF_FlowPropertys.First(p => p.PropertyName == "FromTime")
                .FlowPropertyId;
            int totime_id = group.WF_FlowPropertys.First(p => p.PropertyName == "ToTime")
                .FlowPropertyId;
            DateTime? start = GetLeaveDateTime(props.First(p => p.Id == fromdate_id).Value,
                props.First(p => p.Id == fromtime_id).Value, true);
            DateTime? end = GetLeaveDateTime(props.First(p => p.Id == todate_id).Value,
                props.First(p => p.Id == totime_id).Value, false);
            if (start == null || end == null)
                return false;
            var ids = Entities.WF_FlowCases
                .Where(p => p.StatusId > 0 && p.UserNo == CurrentUser && p.Rejected == null &&
                            p.WF_Flows.FlowGroupId == group.FlowGroupId).Select(p => p.FlowCaseId).ToArray();
            foreach (var flowcaseid in ids)
            {
                var type = Entities.WF_CasePropertyValues.FirstOrDefault(p =>
                    p.StatusId > 0 && p.FlowCaseId == flowcaseid && p.PropertyId == type_id);
                if (type.StringValue != "CANCEL LEAVE")
                    continue;
                var fd = Entities.WF_CasePropertyValues.FirstOrDefault(p =>
                    p.StatusId > 0 && p.FlowCaseId == flowcaseid && p.PropertyId == fromdate_id);
                var td = Entities.WF_CasePropertyValues.FirstOrDefault(p =>
                    p.StatusId > 0 && p.FlowCaseId == flowcaseid && p.PropertyId == todate_id);
                var ft = Entities.WF_CasePropertyValues.FirstOrDefault(p =>
                    p.StatusId > 0 && p.FlowCaseId == flowcaseid && p.PropertyId == fromtime_id);
                var tt = Entities.WF_CasePropertyValues.FirstOrDefault(p =>
                    p.StatusId > 0 && p.FlowCaseId == flowcaseid && p.PropertyId == totime_id);
                if (fd == null || td == null || ft == null || tt == null)
                    continue;
                DateTime? start1 = GetLeaveDateTime(fd.DateValue.Value, ft.StringValue, true);
                DateTime? end1 = GetLeaveDateTime(td.DateValue.Value, tt.StringValue, false);
                if (start1 == null || end1 == null)
                    continue;
                if (start.Value <= end1.Value && end.Value >= start1.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private DateTime? GetLeaveDateTime(string date, string time, bool isbegin)
        {
            DateTime.TryParse(date, out DateTime datepart);
            if (time == "AM" && isbegin)
            {
                return new DateTime(datepart.Year, datepart.Month, datepart.Day, 0, 0, 0);
            }
            if (time == "AM" && !isbegin)
            {
                return new DateTime(datepart.Year, datepart.Month, datepart.Day, 11, 59, 59);
            }
            if (time == "PM" && isbegin)
            {
                return new DateTime(datepart.Year, datepart.Month, datepart.Day, 12, 0, 0);
            }
            if (time == "PM" && !isbegin)
            {
                return new DateTime(datepart.Year, datepart.Month, datepart.Day, 23, 59, 59);
            }
            return null;
        }

        private DateTime? GetLeaveDateTime(DateTime date, string time, bool isbegin)
        {
            if (time == "AM" && isbegin)
            {
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            }
            if (time == "AM" && !isbegin)
            {
                return new DateTime(date.Year, date.Month, date.Day, 11, 59, 59);
            }
            if (time == "PM" && isbegin)
            {
                return new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            }
            if (time == "PM" && !isbegin)
            {
                return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
            }
            return null;
        }

        public WF_FlowTypes GetFlowTypeById(int flowTypeId)
        {
            return Entities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowTypeId);
        }
    }
}