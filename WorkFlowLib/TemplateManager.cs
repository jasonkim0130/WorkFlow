using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Dreamlab.Core;
using Unity.Interception.Utilities;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;

namespace WorkFlowLib
{
    public class TemplateManager : ApplicationUser
    {
        public TemplateManager(WorkFlowEntities entities, string applicantUser) : base(entities, applicantUser)
        {
        }

        public FlowInfo[] GetFlowInfoByGroupId(int flowGroupId)
        {
            return GetFlowInfoQuery().Where(p => p.FlowGroupId == flowGroupId).ToArray();
        }

        public int? GetFlowIdByStepGroupId(int id)
        {
            return
                Entities.WF_StepGroups.Where(p => p.StepGroupId == id).Select(p => p.WF_Flows.FlowId).FirstOrDefault();
        }

        public int? GetFlowIdByStepId(int id)
        {
            return
                Entities.WF_FlowSteps.Where(p => p.FlowStepId == id).Select(p => p.WF_StepGroups.FlowId)
                    .FirstOrDefault();
        }

        public AppendStepResult AppendStep(UserSearchModel model, WFStepCondition condition)
        {
            try
            {
                WF_FlowSteps step = CreateStep(model);
                if (step == null)
                    return AppendStepResult.InvalidData;
                WF_StepGroups sg = new WF_StepGroups()
                {
                    Created = DateTime.UtcNow,
                    FlowId = model.flowId,
                    LastUpdated = DateTime.UtcNow,
                    OrderId = Entities.WF_StepGroups.Any(p => p.FlowId == model.flowId)
                        ? Entities.WF_StepGroups.Where(p => p.FlowId == model.flowId).Max(p => p.OrderId) + 1
                        : 1,
                    StatusId = 1,
                    ConditionId = Consts.ConditionAll
                };
                Entities.WF_StepGroups.Add(sg);
                step.WF_StepGroups = sg;
                Entities.WF_FlowSteps.Add(step);
                if (condition != null && condition.DataKey > 0 && !string.IsNullOrWhiteSpace(condition.Value))
                {
                    Entities.WF_StepGroupConditions.Add(new WF_StepGroupConditions()
                    {
                        Created = DateTime.UtcNow,
                        DataKey = condition.DataKey,
                        LastUpdated = DateTime.UtcNow,
                        Operator = condition.Operator,
                        StatusId = 1,
                        WF_StepGroups = sg,
                        Value = condition.Value,
                        MaxValue = condition.MaxValue,
                        NextStepGroupId = condition.NextStepGroupId
                    });
                }

                Entities.SaveChanges();
                return AppendStepResult.Success;
            }
            catch (Exception e)
            {
                return AppendStepResult.InvalidData;
            }
        }

        private WF_FlowSteps CreateStep(UserSearchModel model)
        {
            WF_FlowSteps step = new WF_FlowSteps
            {
                Created = DateTime.UtcNow,
                Department = "",
                LastUpdated = DateTime.UtcNow,
                OrderId = 1,
                StatusId = 1
            };
            step.ApproverType = (int)model.approverType;
            if (model.approverType == ApproverType.Person)
            {
                if (string.IsNullOrWhiteSpace(model.approver))
                    return null;
                UserStaffInfo staffInfo = SearStaffInfo(model.approver);
                if (staffInfo == null)
                {
                    return null;
                }
                else
                {
                    step.Department = staffInfo.DepartmentName;
                }

                step.UserNo = model.approver;
            }

            if (model.approverType == ApproverType.RoleCriteria)
            {
                step.CriteriaGradeOperator = model.rolecriteria.gradeoperator;
                step.CriteriaGrade = model.rolecriteria.grade;
                step.CountryType = model.rolecriteria.countrytype;
                step.DeptType = model.rolecriteria.depttype;
                step.FixedCountry = model.rolecriteria.fixedcountry;
                step.FixedDept = model.rolecriteria.fixeddept;
            }

            if (model.approverType == ApproverType.PredefinedReportingLine)
            {
                step.ManagerOption = model.manageroption.option;
                step.ManagerLevel = model.manageroption.level;
                step.ManagerLevelOperator = model.manageroption.manageroptionoperator;
                step.ManagerMaxLevel = model.manageroption.maxlevel;
            }

            if (model.approverType == ApproverType.PredefinedRole)
            {
                step.UserRole = model.predefinedrole.userrole;
                step.CountryType = model.predefinedrole.countrytype;
                step.DeptType = model.predefinedrole.depttype;
                step.BrandType = model.predefinedrole.brandtype;
                step.FixedCountry = model.predefinedrole.fixedcountry;
                step.FixedDept = model.predefinedrole.fixeddept;
                step.FixedBrand = model.predefinedrole.fixedbrand;
                step.DeptTypeSource = model.predefinedrole.depttypesource;
                step.FixedDeptType = model.predefinedrole.fixeddepttype;
            }

            return step;
        }

        public bool RemoveStep(int id)
        {
            //using (TransactionScope scope = new TransactionScope())
            //{
                var result =
                    Entities.Database.ExecuteSqlCommand(@"UPDATE dbo.WF_FlowSteps SET StatusId =0 WHERE FlowStepId=@p0
IF NOT EXISTS(SELECT 1 FROM dbo.WF_StepGroups AS SG 
INNER JOIN dbo.WF_FlowSteps AS FS ON SG.StepGroupId = FS.StepGroupId
WHERE SG.StepGroupId = (SELECT StepGroupId FROM dbo.WF_FlowSteps WHERE FlowStepId = @p0)
AND FS.StatusId>0)
UPDATE dbo.WF_StepGroups SET StatusId =0 WHERE StepGroupId = (SELECT StepGroupId FROM dbo.WF_FlowSteps WHERE FlowStepId = @p0)",
                        id);
                //scope.Complete();
                return result > 0;
            //}
        }

        public bool RemoveNotifyUser(int id)
        {
            var user = Entities.WF_StepNotificateUsers.FirstOrDefault(p => p.StepNotificateUserId == id);
            user.StatusId = 0;
            Entities.SaveChanges();
            return true;
        }

        public bool RemoveApplicantNotifyUser(int id)
        {
            var user = Entities.WF_ApplicantNotificationUsers.FirstOrDefault(p =>
                p.ApplicationNotificationUserId == id);
            user.StatusId = 0;
            Entities.SaveChanges();
            return true;
        }

        public bool RemoveLastStepNotifyUser(int id)
        {
            var user = Entities.WF_LastStepNotifyUser.FirstOrDefault(p => p.LastStepNotifyUserId == id);
            user.StatusId = 0;
            Entities.SaveChanges();
            return true;
        }

        public AddNotifyUserResult AddLastNotifyNotifyUser(UserSearchModel model)
        {
            WF_LastStepNotifyUser notifyuser = new WF_LastStepNotifyUser()
            {
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                StatusId = 1,
                FlowId = model.flowId,
                NotifyUserType = (int)model.approverType
            };
            if (model.approverType == ApproverType.Person)
            {
                if (string.IsNullOrWhiteSpace(model.approver))
                    return AddNotifyUserResult.UserNotFound;
                GlobalUserView user = GetUser(model.approver);
                if (user == null)
                {
                    return AddNotifyUserResult.UserNotFound;
                }

                notifyuser.UserNo = model.approver;
            }

            if (model.approverType == ApproverType.RoleCriteria)
            {
                notifyuser.CriteriaGradeOperator = model.rolecriteria.gradeoperator;
                notifyuser.CriteriaGrade = model.rolecriteria.grade;
                notifyuser.CountryType = model.rolecriteria.countrytype;
                notifyuser.DeptType = model.rolecriteria.depttype;
                notifyuser.FixedCountry = model.rolecriteria.fixedcountry;
                notifyuser.FixedDept = model.rolecriteria.fixeddept;
            }

            if (model.approverType == ApproverType.PredefinedReportingLine)
            {
                notifyuser.ManagerOption = model.manageroption.option;
                notifyuser.ManagerLevel = model.manageroption.level;
                notifyuser.ManagerLevelOperator = model.manageroption.manageroptionoperator;
                notifyuser.ManagerMaxLevel = model.manageroption.maxlevel;
            }

            if (model.approverType == ApproverType.PredefinedRole)
            {
                notifyuser.UserRole = model.predefinedrole.userrole;
                notifyuser.CountryType = model.predefinedrole.countrytype;
                notifyuser.DeptType = model.predefinedrole.depttype;
                notifyuser.BrandType = model.predefinedrole.brandtype;
                notifyuser.FixedCountry = model.predefinedrole.fixedcountry;
                notifyuser.FixedDept = model.predefinedrole.fixeddept;
                notifyuser.FixedBrand = model.predefinedrole.fixedbrand;
                notifyuser.DeptTypeSource = model.predefinedrole.depttypesource;
                notifyuser.FixedDeptType = model.predefinedrole.fixeddepttype;
            }

            Entities.WF_LastStepNotifyUser.Add(notifyuser);
            Entities.SaveChanges();
            return AddNotifyUserResult.Success;
        }

        public AddNotifyUserResult AddNotifyUser(UserSearchModel model)
        {
            WF_ApplicantNotificationUsers notifyuser = new WF_ApplicantNotificationUsers()
            {
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                StatusId = 1,
                FlowId = model.flowId,
                NotifyUserType = (int)model.approverType
            };
            if (model.approverType == ApproverType.Person)
            {
                if (string.IsNullOrWhiteSpace(model.approver))
                    return AddNotifyUserResult.UserNotFound;
                GlobalUserView user = GetUser(model.approver);
                if (user == null)
                {
                    return AddNotifyUserResult.UserNotFound;
                }

                notifyuser.UserNo = model.approver;
            }

            if (model.approverType == ApproverType.RoleCriteria)
            {
                notifyuser.CriteriaGradeOperator = model.rolecriteria.gradeoperator;
                notifyuser.CriteriaGrade = model.rolecriteria.grade;
                notifyuser.CountryType = model.rolecriteria.countrytype;
                notifyuser.DeptType = model.rolecriteria.depttype;
                notifyuser.FixedCountry = model.rolecriteria.fixedcountry;
                notifyuser.FixedDept = model.rolecriteria.fixeddept;
            }

            if (model.approverType == ApproverType.PredefinedReportingLine)
            {
                notifyuser.ManagerOption = model.manageroption.option;
                notifyuser.ManagerLevel = model.manageroption.level;
                notifyuser.ManagerLevelOperator = model.manageroption.manageroptionoperator;
                notifyuser.ManagerMaxLevel = model.manageroption.maxlevel;
            }

            if (model.approverType == ApproverType.PredefinedRole)
            {
                notifyuser.UserRole = model.predefinedrole.userrole;
                notifyuser.CountryType = model.predefinedrole.countrytype;
                notifyuser.DeptType = model.predefinedrole.depttype;
                notifyuser.BrandType = model.predefinedrole.brandtype;
                notifyuser.FixedCountry = model.predefinedrole.fixedcountry;
                notifyuser.FixedDept = model.predefinedrole.fixeddept;
                notifyuser.FixedBrand = model.predefinedrole.fixedbrand;
                notifyuser.DeptTypeSource = model.predefinedrole.depttypesource;
                notifyuser.FixedDeptType = model.predefinedrole.fixeddepttype;
            }

            Entities.WF_ApplicantNotificationUsers.Add(notifyuser);
            Entities.SaveChanges();
            return AddNotifyUserResult.Success;
        }

        public AddNotifyUserResult AddNotifyUser(int stepGroupId, UserSearchModel model)
        {
            WF_StepNotificateUsers notifyuser = new WF_StepNotificateUsers()
            {
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                StatusId = 1,
                StepGroupId = stepGroupId,
                NotifyUserType = (int)model.approverType
            };
            if (model.approverType == ApproverType.Person)
            {
                if (string.IsNullOrWhiteSpace(model.approver))
                    return AddNotifyUserResult.UserNotFound;
                GlobalUserView user = GetUser(model.approver);
                if (user == null)
                {
                    return AddNotifyUserResult.UserNotFound;
                }

                notifyuser.UserNo = model.approver;
            }

            if (model.approverType == ApproverType.RoleCriteria)
            {
                notifyuser.CriteriaGradeOperator = model.rolecriteria.gradeoperator;
                notifyuser.CriteriaGrade = model.rolecriteria.grade;
                notifyuser.CountryType = model.rolecriteria.countrytype;
                notifyuser.DeptType = model.rolecriteria.depttype;
                notifyuser.FixedCountry = model.rolecriteria.fixedcountry;
                notifyuser.FixedDept = model.rolecriteria.fixeddept;
            }

            if (model.approverType == ApproverType.PredefinedReportingLine)
            {
                notifyuser.ManagerOption = model.manageroption.option;
                notifyuser.ManagerLevel = model.manageroption.level;
                notifyuser.ManagerLevelOperator = model.manageroption.manageroptionoperator;
                notifyuser.ManagerMaxLevel = model.manageroption.maxlevel;
            }

            if (model.approverType == ApproverType.PredefinedRole)
            {
                notifyuser.UserRole = model.predefinedrole.userrole;
                notifyuser.CountryType = model.predefinedrole.countrytype;
                notifyuser.DeptType = model.predefinedrole.depttype;
                notifyuser.BrandType = model.predefinedrole.brandtype;
                notifyuser.FixedCountry = model.predefinedrole.fixedcountry;
                notifyuser.FixedDept = model.predefinedrole.fixeddept;
                notifyuser.FixedBrand = model.predefinedrole.fixedbrand;
                notifyuser.DeptTypeSource = model.predefinedrole.depttypesource;
                notifyuser.FixedDeptType = model.predefinedrole.fixeddepttype;
            }

            //if (!AnyUsers(model,CurrentUser, GetPropertyValuesByStepGroupId(stepGroupId)))
            //    return AddNotifyUserResult.UserNotFound;
            Entities.WF_StepNotificateUsers.Add(notifyuser);
            Entities.SaveChanges();
            return AddNotifyUserResult.Success;
        }

        public AddNotifyUserResult EditNotifyUser(int notifyUserId, UserSearchModel model)
        {
            var notifyuser =
                Entities.WF_StepNotificateUsers.FirstOrDefault(p => p.StepNotificateUserId == notifyUserId);
            if (notifyuser == null)
            {
                return AddNotifyUserResult.InvalidData;
            }

            if (model.approverType == ApproverType.Person)
            {
                if (string.IsNullOrWhiteSpace(model.approver))
                    return AddNotifyUserResult.UserNotFound;
                GlobalUserView user = GetUser(model.approver);
                if (user == null)
                {
                    return AddNotifyUserResult.UserNotFound;
                }
            }

            notifyuser.UserNo = model.approver;
            notifyuser.CriteriaGradeOperator = model.rolecriteria?.gradeoperator;
            notifyuser.CriteriaGrade = model.rolecriteria?.grade;
            notifyuser.ManagerOption = model.manageroption?.option;
            notifyuser.ManagerLevel = model.manageroption?.level;
            notifyuser.ManagerLevelOperator = model.manageroption?.manageroptionoperator;
            notifyuser.ManagerMaxLevel = model.manageroption?.maxlevel;
            notifyuser.UserRole = model.predefinedrole?.userrole;
            notifyuser.BrandType = model.predefinedrole?.brandtype;
            notifyuser.FixedBrand = model.predefinedrole?.fixedbrand;
            notifyuser.DeptTypeSource = model.predefinedrole?.depttypesource;
            notifyuser.FixedDeptType = model.predefinedrole?.fixeddepttype;

            if (model.approverType == ApproverType.RoleCriteria)
            {
                notifyuser.CountryType = model.rolecriteria?.countrytype;
                notifyuser.DeptType = model.rolecriteria?.depttype;
                notifyuser.FixedCountry = model.rolecriteria?.fixedcountry;
                notifyuser.FixedDept = model.rolecriteria?.fixeddept;
            }
            else if (model.approverType == ApproverType.PredefinedRole)
            {
                notifyuser.CountryType = model.predefinedrole?.countrytype;
                notifyuser.DeptType = model.predefinedrole?.depttype;
                notifyuser.FixedCountry = model.predefinedrole?.fixedcountry;
                notifyuser.FixedDept = model.predefinedrole?.fixeddept;
            }

            notifyuser.NotifyUserType = (int)model.approverType;
            notifyuser.LastUpdated = DateTime.UtcNow;
            Entities.SaveChanges();
            return AddNotifyUserResult.Success;
        }

        public AddNotifyUserResult EditApplicantNotifyUser(int notifyUserId, UserSearchModel model)
        {
            var notifyuser =
                Entities.WF_ApplicantNotificationUsers.FirstOrDefault(p =>
                    p.ApplicationNotificationUserId == notifyUserId);
            if (notifyuser == null)
            {
                return AddNotifyUserResult.InvalidData;
            }

            if (model.approverType == ApproverType.Person)
            {
                if (string.IsNullOrWhiteSpace(model.approver))
                    return AddNotifyUserResult.UserNotFound;
                GlobalUserView user = GetUser(model.approver);
                if (user == null)
                {
                    return AddNotifyUserResult.UserNotFound;
                }
            }

            notifyuser.UserNo = model.approver;
            notifyuser.CriteriaGradeOperator = model.rolecriteria?.gradeoperator;
            notifyuser.CriteriaGrade = model.rolecriteria?.grade;
            notifyuser.ManagerOption = model.manageroption?.option;
            notifyuser.ManagerLevel = model.manageroption?.level;
            notifyuser.ManagerLevelOperator = model.manageroption?.manageroptionoperator;
            notifyuser.ManagerMaxLevel = model.manageroption?.maxlevel;
            notifyuser.UserRole = model.predefinedrole?.userrole;
            notifyuser.BrandType = model.predefinedrole?.brandtype;
            notifyuser.FixedBrand = model.predefinedrole?.fixedbrand;
            notifyuser.DeptTypeSource = model.predefinedrole?.depttypesource;
            notifyuser.FixedDeptType = model.predefinedrole?.fixeddepttype;

            if (model.approverType == ApproverType.RoleCriteria)
            {
                notifyuser.CountryType = model.rolecriteria?.countrytype;
                notifyuser.DeptType = model.rolecriteria?.depttype;
                notifyuser.FixedCountry = model.rolecriteria?.fixedcountry;
                notifyuser.FixedDept = model.rolecriteria?.fixeddept;
            }
            else if (model.approverType == ApproverType.PredefinedRole)
            {
                notifyuser.CountryType = model.predefinedrole?.countrytype;
                notifyuser.DeptType = model.predefinedrole?.depttype;
                notifyuser.FixedCountry = model.predefinedrole?.fixedcountry;
                notifyuser.FixedDept = model.predefinedrole?.fixeddept;
            }

            notifyuser.NotifyUserType = (int)model.approverType;
            notifyuser.LastUpdated = DateTime.UtcNow;
            Entities.SaveChanges();
            return AddNotifyUserResult.Success;
        }

        public AddNotifyUserResult EditLastStepNotifyUser(int notifyUserId, UserSearchModel model)
        {
            var notifyuser = Entities.WF_LastStepNotifyUser.FirstOrDefault(p => p.LastStepNotifyUserId == notifyUserId);
            if (notifyuser == null)
            {
                return AddNotifyUserResult.InvalidData;
            }

            if (model.approverType == ApproverType.Person)
            {
                if (string.IsNullOrWhiteSpace(model.approver))
                    return AddNotifyUserResult.UserNotFound;
                GlobalUserView user = GetUser(model.approver);
                if (user == null)
                {
                    return AddNotifyUserResult.UserNotFound;
                }
            }

            notifyuser.UserNo = model.approver;
            notifyuser.CriteriaGradeOperator = model.rolecriteria?.gradeoperator;
            notifyuser.CriteriaGrade = model.rolecriteria?.grade;
            notifyuser.ManagerOption = model.manageroption?.option;
            notifyuser.ManagerLevel = model.manageroption?.level;
            notifyuser.ManagerLevelOperator = model.manageroption?.manageroptionoperator;
            notifyuser.ManagerMaxLevel = model.manageroption?.maxlevel;
            notifyuser.UserRole = model.predefinedrole?.userrole;
            notifyuser.BrandType = model.predefinedrole?.brandtype;
            notifyuser.FixedBrand = model.predefinedrole?.fixedbrand;
            notifyuser.DeptTypeSource = model.predefinedrole?.depttypesource;
            notifyuser.FixedDeptType = model.predefinedrole?.fixeddepttype;

            if (model.approverType == ApproverType.RoleCriteria)
            {
                notifyuser.CountryType = model.rolecriteria?.countrytype;
                notifyuser.DeptType = model.rolecriteria?.depttype;
                notifyuser.FixedCountry = model.rolecriteria?.fixedcountry;
                notifyuser.FixedDept = model.rolecriteria?.fixeddept;
            }
            else if (model.approverType == ApproverType.PredefinedRole)
            {
                notifyuser.CountryType = model.predefinedrole?.countrytype;
                notifyuser.DeptType = model.predefinedrole?.depttype;
                notifyuser.FixedCountry = model.predefinedrole?.fixedcountry;
                notifyuser.FixedDept = model.predefinedrole?.fixeddept;
            }

            notifyuser.NotifyUserType = (int)model.approverType;
            notifyuser.LastUpdated = DateTime.UtcNow;
            Entities.SaveChanges();
            return AddNotifyUserResult.Success;
        }

        public AppendStepResult AppendSubStep(int stepGroupId, UserSearchModel model)
        {
            //using (TransactionScope scope = new TransactionScope())
            //{
                WF_FlowSteps step = CreateStep(model);
                if (step == null)
                    return AppendStepResult.InvalidData;
                step.StepGroupId = stepGroupId;
                step.OrderId = Entities.WF_StepGroups.Where(p => p.StepGroupId == stepGroupId)
                                   .Max(p => (int?)p.OrderId) ?? 0 + 1;
                Entities.WF_FlowSteps.Add(step);
                Entities.WF_NoApproverConditions.AddRange(model.NoApproverModel.Select(p => new WF_NoApproverConditions
                {
                    WF_FlowSteps = step,
                    NoApproverDataKey = p.NoApproverDataKey,
                    NoApproverOperator = p.NoApproverOperator,
                    NoApproverValue = p.NoApproverValue,
                    NoApproverMaxValue = p.NoApproverMaxValue ?? "",
                    StatusId = 1
                }));
                Entities.WF_Secretary.AddRange(model.secretaryRules.Select(p => new WF_Secretary
                {
                    WF_FlowSteps = step,
                    UserId = p.UserId,
                    SecretaryId = p.SecretaryId,
                    StatusId = 1
                }));
                Entities.SaveChanges();
                //scope.Complete();
                return AppendStepResult.Success;
            //}
        }

        public AppendStepResult EditSubStep(int flowstepid, UserSearchModel model)
        {
            var target = Entities.WF_FlowSteps.First(p => p.FlowStepId == flowstepid);
            WF_FlowSteps step = CreateStep(model);
            if (target == null || step == null)
                return AppendStepResult.InvalidData;
            target.LastUpdated = DateTime.UtcNow;
            target.Department = step.Department;
            target.UserNo = step.UserNo;
            target.ApproverType = step.ApproverType;
            target.UserRole = step.UserRole;
            target.CriteriaGrade = step.CriteriaGrade;
            target.CriteriaGradeOperator = step.CriteriaGradeOperator;
            target.ManagerOption = step.ManagerOption;
            target.ManagerLevel = step.ManagerLevel;
            target.ManagerMaxLevel = step.ManagerMaxLevel;
            target.ManagerLevelOperator = step.ManagerLevelOperator;
            target.CountryType = step.CountryType;
            target.DeptType = step.DeptType;
            target.BrandType = step.BrandType;
            target.FixedCountry = step.FixedCountry;
            target.FixedDept = step.FixedDept;
            target.FixedBrand = step.FixedBrand;
            target.DeptTypeSource = step.DeptTypeSource;
            target.FixedDeptType = step.FixedDeptType;
            //target.NoApprover = step.NoApprover;
            //target.NoApproverDataKey = step.NoApproverDataKey;
            //target.NoApproverValue = step.NoApproverValue;
            //target.NoApproverOperator = step.NoApproverOperator;
            //target.NoApproverMaxValue = step.NoApproverMaxValue;
            target.WF_NoApproverConditions.ForEach(p => p.StatusId = 0);
            if (model?.NoApproverModel?.Length > 0)
            {
                Entities.WF_NoApproverConditions.AddRange(model.NoApproverModel.Select(p => new WF_NoApproverConditions
                {
                    FlowStepId = target.FlowStepId,
                    NoApproverDataKey = p.NoApproverDataKey,
                    NoApproverOperator = p.NoApproverOperator,
                    NoApproverValue = p.NoApproverValue,
                    NoApproverMaxValue = p.NoApproverMaxValue ?? "",
                    StatusId = 1
                }));
            }
            target.WF_Secretary.ForEach(p => p.StatusId = 0);
            if (model?.secretaryRules?.Length > 0)
            {
                Entities.WF_Secretary.AddRange(model.secretaryRules.Select(p => new WF_Secretary
                {
                    FlowStepId = target.FlowStepId,
                    UserId = p.UserId,
                    SecretaryId = p.SecretaryId,
                    StatusId = 1
                }));
            }
            Entities.SaveChanges();
            return AppendStepResult.Success;
        }

        public WF_FlowPropertys GetProperty(int propertyId)
        {
            return Entities.WF_FlowPropertys.FirstOrDefault(p => p.FlowPropertyId == propertyId);
        }

        public WF_FlowGroups CopyGroupFromOldVersion(WF_FlowGroups group)
        {
            WF_FlowGroups newGroup = null;
            Entities.Configuration.AutoDetectChangesEnabled = false;
            //using (TransactionScope scope = new TransactionScope())
           // {
                newGroup = new WF_FlowGroups()
                {
                    Created = DateTime.UtcNow,
                    FlowTypeId = group.FlowTypeId,
                    LastUpdated = DateTime.UtcNow,
                    StatusId = -1,
                    Version = group.Version + 1,
                    ApprovedArchivePath = group.ApprovedArchivePath
                };
                Entities.WF_FlowGroups.Add(newGroup);
                Dictionary<int, WF_FlowPropertys> newProperties = new Dictionary<int, WF_FlowPropertys>();
                foreach (var prop in Entities.WF_FlowPropertys.AsNoTracking()
                    .Where(p => p.FlowGroupId == group.FlowGroupId && p.StatusId != 0))
                {
                    WF_FlowPropertys newProp = new WF_FlowPropertys
                    {
                        WF_FlowGroups = newGroup,
                        Code = prop.Code,
                        PropertyType = prop.PropertyType,
                        PropertyName = prop.PropertyName,
                        Compulsory = prop.Compulsory,
                        DataSource = prop.DataSource,
                        LastUpdated = DateTime.UtcNow,
                        Created = DateTime.UtcNow,
                        StatusId = prop.StatusId,
                        OrderId = prop.OrderId
                    };
                    newProperties.Add(prop.FlowPropertyId, newProp);
                    Entities.WF_FlowPropertys.Add(newProp);
                }

                Entities.WF_CountryArchivePaths.AddRange(
                    group.WF_CountryArchivePaths.Select(p => new WF_CountryArchivePaths
                    {
                        WF_FlowGroups = newGroup,
                        CountryCode = p.CountryCode,
                        ApprovedArchivePath = p.ApprovedArchivePath
                    }));

                foreach (var flow in Entities.WF_Flows.Where(p => p.FlowGroupId == group.FlowGroupId && p.StatusId > 0))
                {
                    WF_Flows newflow = new WF_Flows
                    {
                        WF_FlowGroups = newGroup,
                        UserNo = CurrentUser,
                        LastUpdated = flow.LastUpdated,
                        Created = DateTime.UtcNow,
                        StatusId = flow.StatusId,
                        Title = flow.Title,
                        BaseFlowId = flow.FlowId,
                        ConditionRelation = flow.ConditionRelation
                    };
                    Entities.WF_Flows.Add(newflow);

                    foreach (var fc in Entities.WF_FlowConditions.Where(p => p.FlowId == flow.FlowId && p.StatusId > 0))
                    {
                        WF_FlowConditions newFlowCondition = new WF_FlowConditions
                        {
                            WF_Flows = newflow,
                            Operator = fc.Operator,
                            Value = fc.Value,
                            LastUpdated = fc.LastUpdated,
                            Created = fc.Created,
                            StatusId = fc.StatusId
                        };
                        if (fc.FlowPropertyId.HasValue)
                        {
                            newFlowCondition.WF_FlowPropertys = newProperties[fc.FlowPropertyId.Value];
                        }
                        else
                        {
                            newFlowCondition.OtherPropertyType = fc.OtherPropertyType;
                        }

                        Entities.WF_FlowConditions.Add(newFlowCondition);
                    }

                    foreach (var sg in Entities.WF_StepGroups.Where(p => p.FlowId == flow.FlowId && p.StatusId > 0))
                    {
                        var newStepGroup = new WF_StepGroups
                        {
                            WF_Flows = newflow,
                            ConditionId = sg.ConditionId, //ALL,ANY
                            OrderId = sg.OrderId,
                            IsAllowModify = sg.IsAllowModify,
                            LastUpdated = sg.LastUpdated,
                            Created = sg.Created,
                            StatusId = sg.StatusId
                        };
                        Entities.WF_StepGroups.Add(newStepGroup);

                        foreach (var fs in sg.WF_FlowSteps)
                        {
                            WF_FlowSteps newFlowStep = new WF_FlowSteps
                            {
                                WF_StepGroups = newStepGroup,
                                Department = fs.Department,
                                UserNo = fs.UserNo,
                                ApproverType = fs.ApproverType,
                                UserRole = fs.UserRole,
                                CriteriaGradeOperator = fs.CriteriaGradeOperator,
                                CriteriaGrade = fs.CriteriaGrade,
                                ManagerOption = fs.ManagerOption,
                                ManagerLevel = fs.ManagerLevel,
                                ManagerLevelOperator = fs.ManagerLevelOperator,
                                ManagerMaxLevel = fs.ManagerMaxLevel,
                                CountryType = fs.CountryType,
                                DeptType = fs.DeptType,
                                BrandType = fs.BrandType,
                                FixedCountry = fs.FixedCountry,
                                FixedDept = fs.FixedDept,
                                FixedBrand = fs.FixedBrand,
                                DeptTypeSource = fs.DeptTypeSource,
                                FixedDeptType = fs.FixedDeptType,
                                OrderId = fs.OrderId,
                                LastUpdated = fs.LastUpdated,
                                Created = fs.Created,
                                StatusId = fs.StatusId
                            };
                            Entities.WF_FlowSteps.Add(newFlowStep);

                            Entities.WF_NoApproverConditions.AddRange(fs.WF_NoApproverConditions
                                .Where(p => p.StatusId > 0).Select(p => new WF_NoApproverConditions
                                {
                                    WF_FlowSteps = newFlowStep,
                                    NoApproverDataKey = p.NoApproverDataKey,
                                    NoApproverOperator = p.NoApproverOperator,
                                    NoApproverValue = p.NoApproverValue,
                                    NoApproverMaxValue = p.NoApproverMaxValue ?? "",
                                    StatusId = 1
                                }));
                            Entities.WF_Secretary.AddRange(fs.WF_Secretary
                                .Where(p => p.StatusId > 0).Select(p => new WF_Secretary
                                {
                                    WF_FlowSteps = newFlowStep,
                                    UserId = p.UserId,
                                    SecretaryId = p.SecretaryId,
                                    StatusId = 1
                                }));
                        }

                        foreach (var sn in sg.WF_StepNotificateUsers)
                        {
                            var newNotification = new WF_StepNotificateUsers
                            {
                                WF_StepGroups = newStepGroup,
                                NotifyUserType = sn.NotifyUserType,
                                UserNo = sn.UserNo,
                                CriteriaGradeOperator = sn.CriteriaGradeOperator,
                                CriteriaGrade = sn.CriteriaGrade,
                                LastUpdated = sn.LastUpdated,
                                Created = sn.Created,
                                StatusId = sn.StatusId,
                                UserRole = sn.UserRole,
                                ManagerOption = sn.ManagerOption,
                                ManagerLevel = sn.ManagerLevel,
                                ManagerLevelOperator = sn.ManagerLevelOperator,
                                ManagerMaxLevel = sn.ManagerMaxLevel,
                                CountryType = sn.CountryType,
                                DeptType = sn.DeptType,
                                BrandType = sn.BrandType,
                                FixedCountry = sn.FixedCountry,
                                FixedDept = sn.FixedDept,
                                FixedBrand = sn.FixedBrand,
                                DeptTypeSource = sn.DeptTypeSource,
                                FixedDeptType = sn.FixedDeptType
                            };
                            Entities.WF_StepNotificateUsers.Add(newNotification);
                        }

                        foreach (var sgc in sg.WF_StepGroupConditions)
                        {
                            var newStepGroupCondition = new WF_StepGroupConditions
                            {
                                WF_StepGroups = newStepGroup,
                                DataKey = sgc.DataKey,
                                Operator = sgc.Operator,
                                Value = sgc.Value,
                                MaxValue = sgc.MaxValue,
                                LastUpdated = sgc.LastUpdated,
                                Created = sgc.Created,
                                StatusId = sgc.StatusId,
                                NextStepGroupId = sgc.NextStepGroupId
                            };
                            Entities.WF_StepGroupConditions.Add(newStepGroupCondition);
                        }
                    }

                    foreach (var user in Entities.WF_ApplicantNotificationUsers.Where(p =>
                        p.FlowId == flow.FlowId && p.StatusId > 0))
                    {
                        var newNotifyUser = new WF_ApplicantNotificationUsers
                        {
                            WF_Flows = newflow,
                            NotifyUserType = user.NotifyUserType,
                            UserNo = user.UserNo,
                            CriteriaGradeOperator = user.CriteriaGradeOperator,
                            CriteriaGrade = user.CriteriaGrade,
                            LastUpdated = user.LastUpdated,
                            Created = user.Created,
                            StatusId = user.StatusId,
                            UserRole = user.UserRole,
                            ManagerOption = user.ManagerOption,
                            ManagerLevel = user.ManagerLevel,
                            ManagerLevelOperator = user.ManagerLevelOperator,
                            ManagerMaxLevel = user.ManagerMaxLevel,
                            CountryType = user.CountryType,
                            DeptType = user.DeptType,
                            BrandType = user.BrandType,
                            FixedCountry = user.FixedCountry,
                            FixedDept = user.FixedDept,
                            FixedBrand = user.FixedBrand,
                            DeptTypeSource = user.DeptTypeSource,
                            FixedDeptType = user.FixedDeptType
                        };
                        Entities.WF_ApplicantNotificationUsers.Add(newNotifyUser);
                    }

                    foreach (var user in Entities.WF_LastStepNotifyUser.Where(p =>
                        p.FlowId == flow.FlowId && p.StatusId > 0))
                    {
                        var newNotifyUser = new WF_LastStepNotifyUser
                        {
                            WF_Flows = newflow,
                            NotifyUserType = user.NotifyUserType,
                            UserNo = user.UserNo,
                            CriteriaGradeOperator = user.CriteriaGradeOperator,
                            CriteriaGrade = user.CriteriaGrade,
                            LastUpdated = user.LastUpdated,
                            Created = user.Created,
                            StatusId = user.StatusId,
                            UserRole = user.UserRole,
                            ManagerOption = user.ManagerOption,
                            ManagerLevel = user.ManagerLevel,
                            ManagerLevelOperator = user.ManagerLevelOperator,
                            ManagerMaxLevel = user.ManagerMaxLevel,
                            CountryType = user.CountryType,
                            DeptType = user.DeptType,
                            BrandType = user.BrandType,
                            FixedCountry = user.FixedCountry,
                            FixedDept = user.FixedDept,
                            FixedBrand = user.FixedBrand,
                            DeptTypeSource = user.DeptTypeSource,
                            FixedDeptType = user.FixedDeptType
                        };
                        Entities.WF_LastStepNotifyUser.Add(newNotifyUser);
                    }
                }

                Entities.ChangeTracker.DetectChanges();
                if (Entities.SaveChanges() > 0)
                {
                  //  scope.Complete();
                }
           // }

            if (newGroup != null)
            {
                foreach (
                    var flow in Entities.WF_Flows.Where(p => p.FlowGroupId == newGroup.FlowGroupId && p.StatusId > 0))
                {
                    var stepGroups = Entities.WF_StepGroups.Where(p => p.FlowId == flow.FlowId && p.StatusId > 0);
                    foreach (var sg in stepGroups)
                    {
                        foreach (var sgc in sg.WF_StepGroupConditions.Where(p => p.StatusId > 0))
                        {
                            if (sgc.NextStepGroupId.HasValue)
                            {
                                var oldNextStep =
                                    Entities.WF_StepGroups.FirstOrDefault(p =>
                                        p.StepGroupId == sgc.NextStepGroupId.Value);
                                var newNextStep = stepGroups.FirstOrDefault(p => p.OrderId == oldNextStep.OrderId);
                                sgc.NextStepGroupId = newNextStep.StepGroupId;
                                Entities.Entry(sgc).State = EntityState.Modified;
                            }
                        }
                    }
                }

                Entities.SaveChanges();
            }

            return newGroup;
        }

        public void UpdateLastUpdated(int flowId)
        {
            Entities.Database.ExecuteSqlCommand("UPDATE dbo.WF_Flows SET LastUpdated = GETUTCDATE() WHERE FlowId = @p0",
                flowId);
        }

        public int CreateNewTemplate(string name, string dep, int? grade, int? templateType, string iconUrl,
            string platform, string tabs)
        {
            string iconName = "Test.png";
            if (templateType.HasValue)
            {
                switch (templateType.Value)
                {
                    case 1: //store approval form
                        iconName = "StoreApproval.png";
                        break;
                    case 2: //leave application
                        iconName = "LeaveApp.png";
                        break;
                    case 3: //overseas business trip application
                        iconName = "OverseasBusinessTripApp.png";
                        break;
                    case 4: //overseas business trip claim
                        iconName = "OverseasBusinessTripClaim.png";
                        break;
                    case 5: //employee location expense claim
                        iconName = "EmployeeLocationExpenseClaim.png";
                        break;
                    case 6: //staff requisition
                        iconName = "StaffRequisitionApp.png";
                        break;
                    case 7: //store closure

                        break;
                    case 8: //personal data change form
                        iconName = "PersonalDataChangeApp.png";
                        break;
                }
            }

            //using (TransactionScope scope = new TransactionScope())
            {
                WF_FlowTypes newType = new WF_FlowTypes
                {
                    Name = name,
                    Icon = iconName,
                    LastUpdated = DateTime.UtcNow,
                    Creator = CurrentUser,
                    Dep = dep,
                    Grade = grade,
                    Created = DateTime.UtcNow,
                    StatusId = 1,
                    TemplateType = templateType,
                    IconUrl = iconUrl,
                    Platform = platform,
                    Tabs = tabs
                };
                Entities.WF_FlowTypes.Add(newType);
                WF_FlowGroups newGroup = new WF_FlowGroups
                {
                    WF_FlowTypes = newType,
                    Version = 0,
                    LastUpdated = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    StatusId = -1
                };
                Entities.WF_FlowGroups.Add(newGroup);
                if (templateType.HasValue)
                {
                    SpecialPropertyModel[] specialProps = GetSpecialPropertiesFromXml(templateType.Value);
                    foreach (var prop in specialProps.Where(p => p.Required))
                    {
                        WF_FlowPropertys entity = new WF_FlowPropertys
                        {
                            WF_FlowGroups = newGroup,
                            PropertyType = prop.FieldTypeId,
                            PropertyName = prop.FieldName,
                            Compulsory = prop.Compulsory,
                            WF_TemplatePropCode =
                                Entities.WF_TemplatePropCode.FirstOrDefault(q => q.PropCode == prop.Code),
                            LastUpdated = DateTime.UtcNow,
                            Created = DateTime.UtcNow,
                            StatusId = -1
                        };
                        Entities.WF_FlowPropertys.Add(entity);
                    }
                }

                Entities.SaveChanges();
               // scope.Complete();
                return newType.FlowTypeId;
            }
        }

        public WF_FlowGroups GetEditableFlowGroup(int flowTypeId)
        {
            //SHOULD BE REMOVED AFTER RELEASED
            if (Entities.WF_FlowTypes.Any(p => p.StatusId > 0 && p.FlowTypeId == flowTypeId && p.TemplateType == 9))
            {
                return Entities.WF_FlowGroups.FirstOrDefault(p => p.FlowTypeId == flowTypeId && p.StatusId > 0) ??
                       Entities.WF_FlowGroups.FirstOrDefault(p => p.FlowTypeId == flowTypeId && p.StatusId == -1);
            }

            return Entities.WF_FlowGroups.FirstOrDefault(p => p.FlowTypeId == flowTypeId && p.StatusId == -1) ??
                   Entities.WF_FlowGroups.FirstOrDefault(p => p.FlowTypeId == flowTypeId && p.StatusId > 0);
        }

        public bool AnyFlow(int flowGroupId)
        {
            return Entities.WF_Flows.Where(p => p.FlowGroupId == flowGroupId).Any(p => p.StatusId > 0);
        }

        public void SetFlowGroupEffective(int flowGroupId)
        {
           // using (TransactionScope scope = new TransactionScope())
            {
                Entities.Database.ExecuteSqlCommand(@"DECLARE @FlowTypeId INT
SELECT @FlowTypeId = FlowTypeId FROM dbo.WF_FlowGroups WHERE FlowGroupId = @p0
IF(@FlowTypeId IS NOT NULL)
BEGIN
UPDATE dbo.WF_FlowGroups SET StatusId = 0,LastUpdated = GETUTCDATE() WHERE FlowTypeId = @FlowTypeId AND FlowGroupId!=@p0
UPDATE dbo.WF_FlowGroups SET StatusId = 1,LastUpdated = GETUTCDATE() WHERE FlowGroupId = @p0 AND StatusId=-1
END", flowGroupId);
                //scope.Complete();
            }
        }

        public WF_FlowTypes GetFlowTypeByGroupId(int groupId)
        {
            return Entities.WF_FlowGroups.Where(p => p.FlowGroupId == groupId).Select(p => p.WF_FlowTypes)
                .FirstOrDefault();
        }

        public WF_FlowTypes GetFlowTypeById(int flowTypeId)
        {
            return Entities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowTypeId);
        }

        public WF_FlowGroups GetFlowGroup(int flowGroupId)
        {
            return Entities.WF_FlowGroups.FirstOrDefault(p => p.FlowGroupId == flowGroupId);
        }

        public void RemoveProperty(params int[] propertyIds)
        {
            //using (TransactionScope scope = new TransactionScope())
            {
                foreach (int propertyId in propertyIds)
                {
                    Entities.Database.ExecuteSqlCommand(@"
UPDATE WF_FlowPropertys SET StatusId = 0,LastUpdate = @p1 WHERE FlowPropertyId = @p0;
UPDATE WF_CasePropertyValues SET StatusId = 0 WHERE PropertyId = @p0;
UPDATE WF_FlowConditions SET StatusId = 0 WHERE FlowPropertyId = @p0;", propertyId, DateTime.UtcNow);
                }
               // scope.Complete();
            }
        }

        public WF_FlowGroups[] GetFlowGroupHistory(int flowTypeId)
        {
            return Entities.WF_FlowGroups.Where(p => p.FlowTypeId == flowTypeId).AsNoTracking().ToArray();
        }

        public void UpdateProperty(int propertyid, NewPropertyModel model)
        {
            var prop = GetProperty(propertyid);
            if (prop != null)
            {
                prop.PropertyName = model.fieldName;
                prop.Compulsory = model.compulsory;
                prop.Code = model.code;
                prop.RowIndex = model.RowIndex;
                prop.Tab = model.Tab;
                prop.ViewType = model.ViewType;
                prop.ColumnIndex = model.ColumnIndex;
                prop.Text = model.Text;
                prop.PropertyType = model.fieldTypeId;
                prop.DataSource = model.datasource;
                prop.Validation = model.Validation;
                prop.ValidationMsg = model.ValidationMsg;
                prop.BgColor = model.BgColor;
                prop.FontSize = model.FontSize;
                prop.FontColor = model.FontColor;
                prop.Width = model.Width;
                prop.Height = model.Height;
                prop.HAlign = model.HAlign;
                prop.AllowMultiple = model.Multiple;
            }

            Entities.SaveChanges();
        }

        public WF_FlowGroups GetFlowGroupByPropertyId(int propertyid)
        {
            return
                Entities.WF_FlowPropertys.Where(p => p.FlowPropertyId == propertyid)
                    .Select(p => p.WF_FlowGroups).AsNoTracking()
                    .FirstOrDefault();
        }

        public WF_FlowPropertys UpdateSpecialProperty(NewPropertyModel model, bool enable)
        {
            var target =
                Entities.WF_FlowPropertys.FirstOrDefault(
                    p =>
                        p.PropertyType == model.fieldTypeId && p.PropertyName == model.fieldName &&
                        p.Compulsory == model.compulsory && p.Code == model.code &&
                        p.FlowGroupId == model.flowGroupId && p.StatusId == (enable ? 0 : -1));
            if (target != null)
            {
                target.StatusId = enable ? -1 : 0;
                target.LastUpdated = DateTime.UtcNow;
                Entities.SaveChanges();
                return target;
            }

            WF_FlowPropertys entity = new WF_FlowPropertys
            {
                FlowGroupId = model.flowGroupId,
                PropertyType = model.fieldTypeId,
                PropertyName = model.fieldName,
                Compulsory = model.compulsory,
                Code = model.code,
                LastUpdated = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                StatusId = -1
            };
            Entities.WF_FlowPropertys.Add(entity);
            Entities.SaveChanges();
            return entity;
        }

        public WF_FlowPropertys SaveNewProperty(NewPropertyModel model)
        {
            if (model.code != null)
            {
                if (model.fieldTypeId == 9 || model.fieldTypeId == 10 || model.fieldTypeId == 11 || model.fieldTypeId == 12)
                {
                    if (
                        Entities.WF_FlowPropertys.Any(
                            p => p.FlowGroupId == model.flowGroupId && p.PropertyType == model.fieldTypeId &&
                                 p.StatusId > 0))
                        return null;
                }

            }

            WF_FlowPropertys entity = new WF_FlowPropertys
            {
                FlowGroupId = model.flowGroupId,
                PropertyType = model.fieldTypeId,
                PropertyName = model.fieldName,
                Compulsory = model.compulsory,
                Code = model.code,
                Tab = model.Tab,
                RowIndex = model.RowIndex,
                ColumnIndex = model.ColumnIndex,
                ViewType = model.ViewType,
                LastUpdated = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                DataSource = model.datasource,
                Text = model.Text,
                Validation = model.Validation,
                ValidationMsg = model.ValidationMsg,
                BgColor = model.BgColor,
                FontSize = model.FontSize,
                FontColor = model.FontColor,
                Width = model.Width,
                Height = model.Height,
                HAlign = model.HAlign,
                AllowMultiple = model.Multiple,
                StatusId = 1
            };
            Entities.WF_FlowPropertys.Add(entity);
            Entities.SaveChanges();
            return entity;
        }

        public WF_FlowPropertys[] GetUserSearchProperties(int flowId)
        {
            int[] idArray = new int[] { 9, 10, 11, 12 };
            return
                Entities.WF_Flows.Where(p => p.FlowId == flowId)
                    .SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys)
                    .Where(p => p.StatusId > 0 && idArray.Contains(p.PropertyType)).AsNoTracking()
                    .ToArray();
        }

        public SpecialPropertyModel[] GetSpecialPropertiesFromXml(int typeid)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\SpecialWFTemplate.xml");
            XElement ele = XElement.Parse(File.ReadAllText(path));
            return
                ele.Elements()
                    .Where(p => p.Attribute("TypeId").Value == typeid.ToString())
                    .Elements("Property")
                    .Select(p => new SpecialPropertyModel
                    {
                        FieldTypeId = int.Parse(p.Attribute("Type").Value),
                        FieldName = p.Attribute("Name").Value,
                        Compulsory = bool.Parse(p.Attribute("Compulsory").Value),
                        Required = bool.Parse(p.Attribute("Required").Value),
                        Code = p.Attribute("Code")?.Value
                    }).ToArray();
        }

        public bool SaveFlowCondition(WFCondition condition)
        {
            WF_FlowConditions newCondition = new WF_FlowConditions
            {
                Created = DateTime.UtcNow,
                FlowId = condition.FlowId,
                LastUpdated = DateTime.UtcNow,
                Operator = condition.Operator,
                StatusId = 1,
                Value = condition.Value,
            };
            if (condition.ComparedData.EqualsIgnoreCaseAndBlank("id") ||
                condition.ComparedData.EqualsIgnoreCaseAndBlank("grade"))
            {
                newCondition.OtherPropertyType = condition.ComparedData;
            }
            else
            {
                int propertyid;
                if (int.TryParse(condition.ComparedData, out propertyid))
                {
                    newCondition.FlowPropertyId = propertyid;
                }
                else
                {
                    return false;
                }
            }

            Entities.WF_FlowConditions.Add(newCondition);
            return Entities.SaveChanges() > 0;
        }

        public bool SaveTemplatePropertyies(int flowTypeId, NewTemplateModel model)
        {
            var result = Entities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowTypeId);
            if (result == null)
            {
                return false;
            }
            result.Tabs = model.tabs;
            result.Name = model.name;
            result.Grade = model.grade;
            result.TemplateType = model.templateType;
            result.IconUrl = model.iconUrl;
            result.Platform = model.platform;
            result.Dep = model.dep;
            result.LastUpdated = DateTime.Now;
            return Entities.SaveChanges() > 0;

        }
    }
}