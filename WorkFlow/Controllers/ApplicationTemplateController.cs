using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LinqToExcel;
using Newtonsoft.Json.Linq;
using Dreamlab.Core;
using Resources;
using WorkFlow.Ext;
using WorkFlowLib;
using WorkFlowLib.DTO;
using WorkFlowLib.Data;
using WorkFlowLib.Logic;

namespace WorkFlow.Controllers
{
    public enum FindUserType
    {
        Step = 0,
        NotifyUser = 1
    }

    public class ApplicationTemplateController : WFController
    {
        public ActionResult Index()
        {
            return PartialView("Index",
                WFEntities.WF_FlowTypes.Where(p => p.StatusId > 0).AsNoTracking()
                    .Select(p => new FlowTypeModel
                    {
                        Template = p,
                        Version = p.WF_FlowGroups.Where(q => p.StatusId > 0 || p.StatusId == -1).Max(q => q.Version),
                        IsEditing = p.WF_FlowGroups.Any(q => q.StatusId == -1)
                    })
                    .ToArray());
        }

        public ActionResult Edit(int flowTypeId, bool propmt = true)
        {
            TemplateManager user = new TemplateManager(WFEntities, this.Username);
            WF_FlowGroups group = user.GetEditableFlowGroup(flowTypeId);
            if (group == null)
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.UNABLE_TO_EDIT_TEMPLATE);
            }
            bool hasFlow = user.AnyFlow(group.FlowGroupId);
            WF_FlowTypes flowtype = user.GetFlowTypeByGroupId(group.FlowGroupId);
            //#TODO FOR DEBUG
            if (!(flowtype.TemplateType == 9))
            {
                if (propmt && group.StatusId > 0 && hasFlow)
                {
                    ViewBag.FlowTypeId = flowTypeId;
                    return View("_EditAlert", "~/Views/Shared/_ModalLayout.cshtml");
                }
                if (group.StatusId > 0 && hasFlow)
                {
                    group = user.CopyGroupFromOldVersion(@group);
                    if (group == null)
                    {
                        ViewBag.DisplayButtons = false;
                        return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml",
                            StringResource.UNABLE_TO_COPY_TEMPLATE);
                    }
                }
            }

            ViewBag.FlowType = flowtype;
            ViewBag.FlowGroup = group;
            ViewBag.FlowsConditions = user.GetFlowConditionsByGroupId(group.FlowGroupId);
            ViewBag.Properties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId > 0 && p.FlowGroupId == group.FlowGroupId).AsNoTracking().ToArray();
            if (flowtype.TemplateType.HasValue)
            {
                ViewBag.SpecialProperties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId < 0 && p.FlowGroupId == group.FlowGroupId).AsNoTracking().ToArray();
                ViewBag.AllSpecialProperties = user.GetSpecialPropertiesFromXml(flowtype.TemplateType.Value);
            }

            ViewBag.Countries = Consts.Countries;
            ViewBag.CountryArchivePaths = user.GetArchivePathsByGroupId(group.FlowGroupId);
            return PartialView("Edit", user.GetFlowInfoByGroupId(group.FlowGroupId));
        }

        public ActionResult ViewHistory(int flowTypeId)
        {
            TemplateManager user = new TemplateManager(WFEntities, this.Username);
            ViewBag.FlowType = user.GetFlowTypeById(flowTypeId);
            ViewBag.History = user.GetFlowGroupHistory(flowTypeId);
            return PartialView("History");
        }

        public ActionResult ViewDetails(int flowTypeId)
        {
            TemplateManager user = new TemplateManager(WFEntities, this.Username);
            WF_FlowGroups group = user.GetEditableFlowGroup(flowTypeId);
            if (group == null)
            {
                ViewBag.DisplayButtons = false;
                return this.ShowErrorModal(StringResource.UNABLE_TO_VIEW_TEMPLATE);
            }
            ViewBag.FlowGroup = group;
            ViewBag.FlowType = user.GetFlowTypeByGroupId(group.FlowGroupId);
            ViewBag.FlowsConditions = user.GetFlowConditionsByGroupId(group.FlowGroupId);
            ViewBag.Properties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId != 0 && p.FlowGroupId == group.FlowGroupId).AsNoTracking().ToArray();
            ViewBag.CountryArchivePaths = user.GetArchivePathsByGroupId(group.FlowGroupId);
            return PartialView("ViewDetails", user.GetFlowInfoByGroupId(group.FlowGroupId));
        }

        public ActionResult ViewGroupDetails(int flowGroupId)
        {
            TemplateManager user = new TemplateManager(WFEntities, this.Username);
            WF_FlowGroups group = user.GetFlowGroup(flowGroupId);
            if (group == null)
            {
                ViewBag.DisplayButtons = false;
                return View("_PartialError", "~/Views/Shared/_ModalLayout.cshtml", StringResource.UNABLE_TO_VIEW_TEMPLATE);
            }
            ViewBag.FlowGroup = group;
            ViewBag.FlowType = user.GetFlowTypeByGroupId(group.FlowGroupId);
            ViewBag.FlowsConditions = user.GetFlowConditionsByGroupId(group.FlowGroupId);
            ViewBag.Properties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId != 0 && p.FlowGroupId == group.FlowGroupId).AsNoTracking().ToArray();
            ViewBag.CountryArchivePaths = user.GetArchivePathsByGroupId(group.FlowGroupId);
            return PartialView("ViewDetails", user.GetFlowInfoByGroupId(group.FlowGroupId));
        }

        public ActionResult Create()
        {
            return PartialView("Create");
        }

        public ActionResult RemoveStep(int id, int flowId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.RemoveStep(id);
            manager.UpdateLastUpdated(flowId);
            ViewBag.Conditions = manager.GetFlowConditions(flowId);
            return PartialView("_FlowSteps", manager.GetFlowInfo(flowId));
        }

        public ActionResult ViewSubStep(int flowstepid, int flowId)
        {
            WF_FlowSteps step = WFEntities.WF_FlowSteps.FirstOrDefault(p => p.FlowStepId == flowstepid);
            ViewBag.Title = StringResource.VIEW_APPROVER;

            ViewBag.UserTypeId = step?.ApproverType;
            ViewBag.CriteriaGrade = step?.CriteriaGrade;
            ViewBag.CriteriaGradeOperator = step?.CriteriaGradeOperator;
            ViewBag.ManagerLevel = step?.ManagerLevel;
            ViewBag.ManagerMaxLevel = step?.ManagerMaxLevel;
            ViewBag.ManagerOption = step?.ManagerOption;
            ViewBag.ManagerLevelOperator = step?.ManagerLevelOperator;
            ViewBag.UserRole = step?.UserRole;
            ViewBag.Approver = step?.UserNo;
            ViewBag.CountryType = step?.CountryType;
            ViewBag.DeptType = step?.DeptType;
            ViewBag.BrandType = step?.BrandType;
            ViewBag.FixedCountry = step?.FixedCountry;
            ViewBag.FixedBrand = step?.FixedBrand;
            ViewBag.FixedDept = step?.FixedDept;
            ViewBag.DeptTypeSource = step?.DeptTypeSource;
            ViewBag.FixedDeptType = step?.FixedDeptType;

            ViewBag.NoApproverConditions = step?.WF_NoApproverConditions.Where(p => p.StatusId > 0).Select(p => new NoApproverModel
            {
                NoApproverDataKey = p.NoApproverDataKey,
                NoApproverOperator = p.NoApproverOperator,
                NoApproverValue = p.NoApproverValue,
                NoApproverMaxValue = p.NoApproverMaxValue
            }).ToArray();
            ViewBag.SecretaryRules = step?.WF_Secretary.Where(p => p.StatusId > 0).ToArray().Select(p => new SecretaryRule
            {
                UserId = p.UserId,
                UserName = WebCacheHelper.GetUsernames().GetValue(p.UserId),
                SecretaryId = p.SecretaryId,
                SecretaryName = WebCacheHelper.GetUsernames().GetValue(p.SecretaryId)
            }).ToArray();

            ViewBag.DisplayButtons = false;

            ViewBag.Properties = WFEntities.WF_Flows.Where(p => p.FlowId == flowId).SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys.Where(q => q.StatusId != 0)).AsNoTracking().ToArray();
            ViewBag.Type = FindUserType.Step;
            return View("_ViewApprover", "~/Views/Shared/_LargeModalLayout.cshtml");
        }

        public ActionResult EditSubStep(int flowstepid, int flowid)
        {
            WF_FlowSteps step = WFEntities.WF_FlowSteps.FirstOrDefault(p => p.FlowStepId == flowstepid);
            ViewBag.FlowStepId = flowstepid;
            ViewBag.FlowId = flowid;
            ViewBag.ActionName = "EditSubStep";
            ViewBag.Title = StringResource.EDIT_APPROVER;

            ViewBag.UserTypeId = step?.ApproverType;
            ViewBag.CriteriaGrade = step?.CriteriaGrade;
            ViewBag.CriteriaGradeOperator = step?.CriteriaGradeOperator;
            ViewBag.ManagerLevel = step?.ManagerLevel;
            ViewBag.ManagerMaxLevel = step?.ManagerMaxLevel;
            ViewBag.ManagerOption = step?.ManagerOption;
            ViewBag.ManagerLevelOperator = step?.ManagerLevelOperator;
            ViewBag.UserRole = step?.UserRole;
            ViewBag.Approver = step?.UserNo;
            ViewBag.CountryType = step?.CountryType;
            ViewBag.DeptType = step?.DeptType;
            ViewBag.BrandType = step?.BrandType;
            ViewBag.FixedCountry = step?.FixedCountry;
            ViewBag.FixedBrand = step?.FixedBrand;
            ViewBag.FixedDept = step?.FixedDept;
            ViewBag.DeptTypeSource = step?.DeptTypeSource;
            ViewBag.FixedDeptType = step?.FixedDeptType;

            ViewBag.NoApproverConditions = step?.WF_NoApproverConditions.Where(p => p.StatusId > 0).Select(p => new NoApproverModel
            {
                NoApproverDataKey = p.NoApproverDataKey,
                NoApproverOperator = p.NoApproverOperator,
                NoApproverValue = p.NoApproverValue,
                NoApproverMaxValue = p.NoApproverMaxValue
            }).ToArray();

            ViewBag.SecretaryRules = step?.WF_Secretary.Where(p => p.StatusId > 0).ToArray().Select(p => new SecretaryRule
            {
                UserId = p.UserId,
                UserName = WebCacheHelper.GetUsernames().GetValue(p.UserId),
                SecretaryId = p.SecretaryId,
                SecretaryName = WebCacheHelper.GetUsernames().GetValue(p.SecretaryId)
            }).ToArray();

            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            ViewBag.UserSearchProperties = tm.GetUserSearchProperties(flowid);

            ViewBag.Properties = WFEntities.WF_Flows.Where(p => p.FlowId == flowid).SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys.Where(q => q.StatusId != 0)).AsNoTracking().ToArray();
            ViewBag.Type = FindUserType.Step;
            return View("FindUser", "~/Views/Shared/_LargeModalLayout.cshtml");
        }

        /// <summary>
        /// 查看通知用户
        /// </summary>
        /// <param name="notifyUserId"></param>
        /// <param name="flowId"></param>
        /// <param name="stepGroupId">-2 Last step notify user; -1 Application notify user; 0 Step notify user</param>
        /// <returns></returns>
        public ActionResult ViewNotifyUser(int notifyUserId, int flowId, int stepGroupId)
        {
            if (stepGroupId == -2)
            {
                WF_LastStepNotifyUser notifyUser =
                WFEntities.WF_LastStepNotifyUser.FirstOrDefault(p => p.LastStepNotifyUserId == notifyUserId);
                ViewBag.UserTypeId = notifyUser?.NotifyUserType;
                ViewBag.CriteriaGrade = notifyUser?.CriteriaGrade;
                ViewBag.CriteriaGradeOperator = notifyUser?.CriteriaGradeOperator;
                ViewBag.ManagerLevel = notifyUser?.ManagerLevel;
                ViewBag.ManagerMaxLevel = notifyUser?.ManagerMaxLevel;
                ViewBag.ManagerOption = notifyUser?.ManagerOption;
                ViewBag.ManagerLevelOperator = notifyUser?.ManagerLevelOperator;
                ViewBag.UserRole = notifyUser?.UserRole;
                ViewBag.Approver = notifyUser?.UserNo;
                ViewBag.CountryType = notifyUser?.CountryType;
                ViewBag.DeptType = notifyUser?.DeptType;
                ViewBag.BrandType = notifyUser?.BrandType;
                ViewBag.FixedCountry = notifyUser?.FixedCountry;
                ViewBag.FixedBrand = notifyUser?.FixedBrand;
                ViewBag.FixedDept = notifyUser?.FixedDept;
                ViewBag.DeptTypeSource = notifyUser?.DeptTypeSource;
                ViewBag.FixedDeptType = notifyUser?.FixedDeptType;
            }
            else if (stepGroupId == -1)
            {
                WF_ApplicantNotificationUsers notifyUser =
                WFEntities.WF_ApplicantNotificationUsers.FirstOrDefault(
                    p => p.ApplicationNotificationUserId == notifyUserId);
                ViewBag.UserTypeId = notifyUser?.NotifyUserType;
                ViewBag.CriteriaGrade = notifyUser?.CriteriaGrade;
                ViewBag.CriteriaGradeOperator = notifyUser?.CriteriaGradeOperator;
                ViewBag.ManagerLevel = notifyUser?.ManagerLevel;
                ViewBag.ManagerMaxLevel = notifyUser?.ManagerMaxLevel;
                ViewBag.ManagerOption = notifyUser?.ManagerOption;
                ViewBag.ManagerLevelOperator = notifyUser?.ManagerLevelOperator;
                ViewBag.UserRole = notifyUser?.UserRole;
                ViewBag.Approver = notifyUser?.UserNo;
                ViewBag.CountryType = notifyUser?.CountryType;
                ViewBag.DeptType = notifyUser?.DeptType;
                ViewBag.BrandType = notifyUser?.BrandType;
                ViewBag.FixedCountry = notifyUser?.FixedCountry;
                ViewBag.FixedBrand = notifyUser?.FixedBrand;
                ViewBag.FixedDept = notifyUser?.FixedDept;
                ViewBag.DeptTypeSource = notifyUser?.DeptTypeSource;
                ViewBag.FixedDeptType = notifyUser?.FixedDeptType;
            }
            else
            {
                WF_StepNotificateUsers notifyUser =
                WFEntities.WF_StepNotificateUsers.FirstOrDefault(p => p.StepNotificateUserId == notifyUserId);
                ViewBag.UserTypeId = notifyUser?.NotifyUserType;
                ViewBag.CriteriaGrade = notifyUser?.CriteriaGrade;
                ViewBag.CriteriaGradeOperator = notifyUser?.CriteriaGradeOperator;
                ViewBag.ManagerLevel = notifyUser?.ManagerLevel;
                ViewBag.ManagerMaxLevel = notifyUser?.ManagerMaxLevel;
                ViewBag.ManagerOption = notifyUser?.ManagerOption;
                ViewBag.ManagerLevelOperator = notifyUser?.ManagerLevelOperator;
                ViewBag.UserRole = notifyUser?.UserRole;
                ViewBag.Approver = notifyUser?.UserNo;
                ViewBag.CountryType = notifyUser?.CountryType;
                ViewBag.DeptType = notifyUser?.DeptType;
                ViewBag.BrandType = notifyUser?.BrandType;
                ViewBag.FixedCountry = notifyUser?.FixedCountry;
                ViewBag.FixedBrand = notifyUser?.FixedBrand;
                ViewBag.FixedDept = notifyUser?.FixedDept;
                ViewBag.DeptTypeSource = notifyUser?.DeptTypeSource;
                ViewBag.FixedDeptType = notifyUser?.FixedDeptType;
            }

            ViewBag.Title = StringResource.LOOK_FOR_NOTIFY_USER;

            ViewBag.DisplayButtons = false;

            ViewBag.Type = FindUserType.NotifyUser;
            return View("_ViewApprover", "~/Views/Shared/_LargeModalLayout.cshtml");
        }

        /// <summary>
        /// 编辑通知用户
        /// </summary>
        /// <param name="notifyUserId"></param>
        /// <param name="flowId"></param>
        /// <param name="stepGroupId">-2 Last step notify user; -1 Application notify user; 0 Step notify user</param>
        /// <returns></returns>
        public ActionResult EditNotifyUser(int notifyUserId, int flowId, int stepGroupId)
        {
            if (stepGroupId == -2)
            {
                WF_LastStepNotifyUser notifyUser =
                WFEntities.WF_LastStepNotifyUser.FirstOrDefault(p => p.LastStepNotifyUserId == notifyUserId);
                ViewBag.UserTypeId = notifyUser?.NotifyUserType;
                ViewBag.CriteriaGrade = notifyUser?.CriteriaGrade;
                ViewBag.CriteriaGradeOperator = notifyUser?.CriteriaGradeOperator;
                ViewBag.ManagerLevel = notifyUser?.ManagerLevel;
                ViewBag.ManagerMaxLevel = notifyUser?.ManagerMaxLevel;
                ViewBag.ManagerOption = notifyUser?.ManagerOption;
                ViewBag.ManagerLevelOperator = notifyUser?.ManagerLevelOperator;
                ViewBag.UserRole = notifyUser?.UserRole;
                ViewBag.Approver = notifyUser?.UserNo;
                ViewBag.CountryType = notifyUser?.CountryType;
                ViewBag.DeptType = notifyUser?.DeptType;
                ViewBag.BrandType = notifyUser?.BrandType;
                ViewBag.FixedCountry = notifyUser?.FixedCountry;
                ViewBag.FixedBrand = notifyUser?.FixedBrand;
                ViewBag.FixedDept = notifyUser?.FixedDept;
                ViewBag.DeptTypeSource = notifyUser?.DeptTypeSource;
                ViewBag.FixedDeptType = notifyUser?.FixedDeptType;
            }
            else if (stepGroupId == -1)
            {
                WF_ApplicantNotificationUsers notifyUser =
                WFEntities.WF_ApplicantNotificationUsers.FirstOrDefault(
                    p => p.ApplicationNotificationUserId == notifyUserId);
                ViewBag.UserTypeId = notifyUser?.NotifyUserType;
                ViewBag.CriteriaGrade = notifyUser?.CriteriaGrade;
                ViewBag.CriteriaGradeOperator = notifyUser?.CriteriaGradeOperator;
                ViewBag.ManagerLevel = notifyUser?.ManagerLevel;
                ViewBag.ManagerMaxLevel = notifyUser?.ManagerMaxLevel;
                ViewBag.ManagerOption = notifyUser?.ManagerOption;
                ViewBag.ManagerLevelOperator = notifyUser?.ManagerLevelOperator;
                ViewBag.UserRole = notifyUser?.UserRole;
                ViewBag.Approver = notifyUser?.UserNo;
                ViewBag.CountryType = notifyUser?.CountryType;
                ViewBag.DeptType = notifyUser?.DeptType;
                ViewBag.BrandType = notifyUser?.BrandType;
                ViewBag.FixedCountry = notifyUser?.FixedCountry;
                ViewBag.FixedBrand = notifyUser?.FixedBrand;
                ViewBag.FixedDept = notifyUser?.FixedDept;
                ViewBag.DeptTypeSource = notifyUser?.DeptTypeSource;
                ViewBag.FixedDeptType = notifyUser?.FixedDeptType;
            }
            else
            {
                WF_StepNotificateUsers notifyUser =
                WFEntities.WF_StepNotificateUsers.FirstOrDefault(p => p.StepNotificateUserId == notifyUserId);
                ViewBag.UserTypeId = notifyUser?.NotifyUserType;
                ViewBag.CriteriaGrade = notifyUser?.CriteriaGrade;
                ViewBag.CriteriaGradeOperator = notifyUser?.CriteriaGradeOperator;
                ViewBag.ManagerLevel = notifyUser?.ManagerLevel;
                ViewBag.ManagerMaxLevel = notifyUser?.ManagerMaxLevel;
                ViewBag.ManagerOption = notifyUser?.ManagerOption;
                ViewBag.ManagerLevelOperator = notifyUser?.ManagerLevelOperator;
                ViewBag.UserRole = notifyUser?.UserRole;
                ViewBag.Approver = notifyUser?.UserNo;
                ViewBag.CountryType = notifyUser?.CountryType;
                ViewBag.DeptType = notifyUser?.DeptType;
                ViewBag.BrandType = notifyUser?.BrandType;
                ViewBag.FixedCountry = notifyUser?.FixedCountry;
                ViewBag.FixedBrand = notifyUser?.FixedBrand;
                ViewBag.FixedDept = notifyUser?.FixedDept;
                ViewBag.DeptTypeSource = notifyUser?.DeptTypeSource;
                ViewBag.FixedDeptType = notifyUser?.FixedDeptType;
            }

            ViewBag.NotifyUserId = notifyUserId;
            ViewBag.FlowId = flowId;
            ViewBag.ActionName = "EditNotifyUserInfo";
            ViewBag.StepGroupId = stepGroupId;
            ViewBag.Title = StringResource.LOOK_FOR_NOTIFY_USER;

            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            ViewBag.UserSearchProperties = tm.GetUserSearchProperties(flowId);

            ViewBag.Type = FindUserType.NotifyUser;
            return View("FindUser", "~/Views/Shared/_LargeModalLayout.cshtml");
        }

        public ActionResult EditNotifyUserInfo(int notifyUserId, int stepGroupId, UserSearchModel model)
        {
            if (!model.IsValid())
            {
                return Json(new { error = StringResource.PLEASE_SELECT_A_VALUE });
            }
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            AddNotifyUserResult result;
            if (stepGroupId == -1)
            {
                result = manager.EditApplicantNotifyUser(notifyUserId, model);
            }
            else if (stepGroupId == -2)
            {
                result = manager.EditLastStepNotifyUser(notifyUserId, model);
            }
            else
            {
                result = manager.EditNotifyUser(notifyUserId, model);
            }
            if (result == AddNotifyUserResult.Success)
            {
                manager.UpdateLastUpdated(model.flowId);
                FlowInfo flow = null;
                if (stepGroupId == -1 || stepGroupId == -2)
                {
                    flow = manager.GetFlowInfo(model.flowId);
                }
                else
                {
                    flow = manager.GetFlowInfo(manager.GetFlowIdByStepGroupId(stepGroupId) ?? 0);
                }
                ViewBag.Conditions = manager.GetFlowConditions(flow?.FlowId ?? 0);
                return PartialView("_FlowSteps", flow);
            }
            if (result == AddNotifyUserResult.UserDuplicate)
                return this.ShowErrorInModal(StringResource.DUPLICATE_NOTIFY_USER);
            if (result == AddNotifyUserResult.UserNotFound)
                return this.ShowErrorInModal(StringResource.NOTIFY_USER_NOT_FOUND);
            return this.ShowErrorInModal(StringResource.UNABLE_TO_ADD_NOTIFY_USER);
        }

        [HttpPost]
        public ActionResult EditSubStep(int flowstepid, UserSearchModel model)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            if (!model.IsValid())
            {
                return Json(new { error = StringResource.PLEASE_SELECT_A_VALUE });
            }
            AppendStepResult result = manager.EditSubStep(flowstepid, model);
            if (result == AppendStepResult.Success)
            {
                manager.UpdateLastUpdated(model.flowId);
                ViewBag.Conditions = manager.GetFlowConditions(model.flowId);
                return PartialView("_FlowSteps", manager.GetFlowInfo(model.flowId));
            }
            else if (result == AppendStepResult.ApproverDuplicateInPrevious)
                return Json(new { error = StringResource.APPROVER_EXISTED_IN_PREVIOUS_STEPS });
            else
                return Json(new { error = StringResource.NO_APPROVER_FOUND });
        }

        public ActionResult AddSubStep(int id, int flowId)
        {
            ViewBag.StepGroupId = id;
            ViewBag.FlowId = flowId;
            ViewBag.ActionName = "AddSubStep";
            ViewBag.Title = StringResource.LOOK_FOR_APPROVER;
            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            ViewBag.UserSearchProperties = tm.GetUserSearchProperties(flowId);

            ViewBag.Properties = WFEntities.WF_Flows.Where(p => p.FlowId == flowId).SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys.Where(q => q.StatusId != 0)).AsNoTracking().ToArray();
            ViewBag.Type = FindUserType.Step;
            return View("FindUser", "~/Views/Shared/_LargeModalLayout.cshtml");
        }

        [HttpPost]
        public ActionResult AddSubStep(int stepGroupId, UserSearchModel model)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            if (!model.IsValid())
            {
                return Json(new { error = StringResource.PLEASE_SELECT_A_VALUE });
            }
            AppendStepResult result = manager.AppendSubStep(stepGroupId, model);
            if (result == AppendStepResult.Success)
            {
                manager.UpdateLastUpdated(model.flowId);
                ViewBag.Conditions = manager.GetFlowConditions(model.flowId);
                return PartialView("_FlowSteps", manager.GetFlowInfo(model.flowId));
            }
            else if (result == AppendStepResult.ApproverDuplicateInPrevious)
                return Json(new { error = StringResource.APPROVER_EXISTED_IN_PREVIOUS_STEPS });
            else
                return Json(new { error = StringResource.NO_APPROVER_FOUND });
        }

        public ActionResult RemoveNotifyUser(int id, int flowId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.RemoveNotifyUser(id);
            manager.UpdateLastUpdated(flowId);
            ViewBag.Conditions = manager.GetFlowConditions(flowId);
            return PartialView("_FlowSteps", manager.GetFlowInfo(flowId));
        }

        public ActionResult AddNotifyUser(int id)
        {
            TemplateManager user = new TemplateManager(WFEntities, this.Username);
            ViewBag.StepGroupId = id;
            int? flowId = user.GetFlowIdByStepGroupId(id);
            ViewBag.FlowId = flowId;
            ViewBag.ActionName = "AddNotifyUser";
            ViewBag.Title = StringResource.LOOK_FOR_NOTIFY_USER;
            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            ViewBag.UserSearchProperties = tm.GetUserSearchProperties(flowId ?? 0);
            ViewBag.Type = FindUserType.NotifyUser;
            return this.LargeModalView("FindUser");
        }

        public ActionResult AddApplicantNotifyUser(int flowId)
        {
            ViewBag.StepGroupId = -1;
            ViewBag.FlowId = flowId;
            ViewBag.ActionName = "AddNotifyUser";
            ViewBag.Title = StringResource.LOOK_FOR_NOTIFY_USER;
            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            ViewBag.UserSearchProperties = tm.GetUserSearchProperties(flowId);
            ViewBag.Type = FindUserType.NotifyUser;
            return this.LargeModalView("FindUser");
        }

        public ActionResult AddLastStepNotifyUser(int flowId)
        {
            ViewBag.StepGroupId = -2;
            ViewBag.FlowId = flowId;
            ViewBag.ActionName = "AddNotifyUser";
            ViewBag.Title = StringResource.LOOK_FOR_NOTIFY_USER;
            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            ViewBag.UserSearchProperties = tm.GetUserSearchProperties(flowId);
            ViewBag.Type = FindUserType.NotifyUser;
            return this.LargeModalView("FindUser");
        }

        public ActionResult RemoveApplicantNotifyUser(int id, int flowId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.RemoveApplicantNotifyUser(id);
            manager.UpdateLastUpdated(flowId);
            ViewBag.Conditions = manager.GetFlowConditions(flowId);
            return PartialView("_FlowSteps", manager.GetFlowInfo(flowId));
        }

        public ActionResult RemoveLastNotifyUser(int id, int flowId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.RemoveLastStepNotifyUser(id);
            manager.UpdateLastUpdated(flowId);
            ViewBag.Conditions = manager.GetFlowConditions(flowId);
            return PartialView("_FlowSteps", manager.GetFlowInfo(flowId));
        }

        [HttpPost]
        public ActionResult AddNotifyUser(int stepGroupId, UserSearchModel model)
        {
            if (!model.IsValid())
            {
                return this.ShowErrorInModal(StringResource.PLEASE_SELECT_A_VALUE);
            }
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            AddNotifyUserResult result;
            if (stepGroupId == -1)
            {
                result = manager.AddNotifyUser(model);
            }
            else if (stepGroupId == -2)
            {
                result = manager.AddLastNotifyNotifyUser(model);
            }
            else
            {
                result = manager.AddNotifyUser(stepGroupId, model);
            }
            if (result == AddNotifyUserResult.Success)
            {
                manager.UpdateLastUpdated(model.flowId);
                FlowInfo flow = null;
                if (stepGroupId == -1 || stepGroupId == -2)
                {
                    flow = manager.GetFlowInfo(model.flowId);
                }
                else
                {
                    flow = manager.GetFlowInfo(manager.GetFlowIdByStepGroupId(stepGroupId) ?? 0);
                }
                ViewBag.Conditions = manager.GetFlowConditions(flow?.FlowId ?? 0);
                return PartialView("_FlowSteps", flow);
            }
            if (result == AddNotifyUserResult.UserDuplicate)
                return this.ShowErrorInModal(StringResource.DUPLICATE_NOTIFY_USER);
            if (result == AddNotifyUserResult.UserNotFound)
                return this.ShowErrorInModal(StringResource.NOTIFY_USER_NOT_FOUND);
            return this.ShowErrorInModal(StringResource.UNABLE_TO_ADD_NOTIFY_USER);
        }

        public ActionResult AddNewStep(int flowId, WFStepCondition condition)
        {
            ViewBag.FlowId = flowId;
            ViewBag.Condition = condition;
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            if (condition.DataKey > 0)
            {
                FlowCondition fc = new FlowCondition(manager.GetProperty(condition.DataKey), condition.Operator, condition.Value.Trim());
                if (!fc.IsValid())
                {
                    return Json(new { error = StringResource.INVALID_VALUE });
                }
            }
            ViewBag.ActionName = "SaveNewStep";
            ViewBag.Title = StringResource.LOOK_FOR_APPROVER;
            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            ViewBag.UserSearchProperties = tm.GetUserSearchProperties(flowId);

            ViewBag.Properties = WFEntities.WF_Flows.Where(p => p.FlowId == flowId).SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys.Where(q => q.StatusId != 0)).AsNoTracking().ToArray();
            ViewBag.Type = FindUserType.Step;
            return View("FindUser", "~/Views/Shared/_LargeModalLayout.cshtml");
        }

        [HttpPost]
        public ActionResult SaveNewStep(UserSearchModel model, WFStepCondition condition)
        {
            ModelState.Remove("StepGroupId");
            ModelState.Remove("PropertyId");
            ModelState.Remove("Operator");
            ModelState.Remove("Value");
            if (!model.IsValid())
            {
                return Json(new { error = StringResource.PLEASE_SELECT_A_VALUE });
            }
            if (!ModelState.IsValid)
                return Json(new { error = StringResource.INVALID_PARAMETER });
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            AppendStepResult result = manager.AppendStep(model, condition);
            if (result == AppendStepResult.Success)
            {
                manager.UpdateLastUpdated(model.flowId);
                ViewBag.Conditions = manager.GetFlowConditions(model.flowId);
                return PartialView("_FlowSteps", manager.GetFlowInfo(model.flowId));
            }
            else if (result == AppendStepResult.ApproverDuplicateInPrevious)
                return Json(new { error = StringResource.APPROVER_EXISTED_IN_PREVIOUS_STEPS });
            else
                return Json(new { error = StringResource.NO_APPROVER_FOUND });
        }

        public ActionResult AddNewProperty(int flowGroupId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            WF_FlowTypes flowtype = manager.GetFlowTypeByGroupId(flowGroupId);
            ViewBag.CodeList = WFEntities.WF_TemplatePropCode.Where(
                p => p.TemplateType == flowtype.TemplateType && p.StatusId > 0)
                .Select(p => new SelectListItem
                {
                    Text = p.PropCode,
                    Value = p.TemplatePropCodeId.ToString()
                }).ToArray();
            ViewBag.FlowGroupId = flowGroupId;
            ViewBag.FlowType = flowtype;
            return View("AddNewProperty", "~/Views/Shared/_ModalLayout.cshtml");
        }

        [HttpPost]
        public ActionResult AddNewProperty(NewPropertyModel model)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            WF_FlowGroups group = manager.GetFlowGroup(model.flowGroupId);
            WF_FlowTypes flowtype = manager.GetFlowTypeByGroupId(group.FlowGroupId);
            if (flowtype.TemplateType == 9)
            {
                model.fieldName = "";
                model.code = null;
            }
            WF_FlowPropertys property = manager.SaveNewProperty(model);
            if (property != null)
            {
                ViewBag.FlowGroup = group;
                ViewBag.Properties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId > 0 && p.FlowGroupId == @group.FlowGroupId).AsNoTracking().ToArray();

                ViewBag.FlowType = flowtype;
                if (flowtype.TemplateType.HasValue)
                {
                    ViewBag.SpecialProperties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId < 0 && p.FlowGroupId == group.FlowGroupId).AsNoTracking().ToArray();
                    ViewBag.AllSpecialProperties = manager.GetSpecialPropertiesFromXml(flowtype.TemplateType.Value);
                }
                return PartialView("_ProperyTable");
            }
            return Json(new { error = StringResource.EXISTED_TYPE_OF_PROPERTY });
        }

        [HttpPost]
        public ActionResult RemoveProperty(int propertyId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.RemoveProperty(propertyId);
            int flowTypeId = WFEntities.WF_FlowPropertys.Where(p => p.FlowPropertyId == propertyId).Select(p => p.WF_FlowGroups.FlowTypeId).FirstOrDefault();
            return RedirectToAction("Edit", new { flowTypeId = flowTypeId });
        }

        [HttpPost]
        public ActionResult RemoveTemplate(int flowTypeId)
        {
            //#TODO all should be deleted.//NEED DELETE OTHER INFO ?
            var target = WFEntities.WF_FlowTypes.FirstOrDefault(p => p.FlowTypeId == flowTypeId);
            if (target != null)
            {
                target.StatusId = 0;
                return Json(new { success = WFEntities.SaveChanges() > 0, flowTypeId });
            }
            return Json(new { success = false });
        }

        public ActionResult AddNewTemplate()
        {
            return this.ModalView("AddNewTemplate");
        }

        [HttpPost]
        public string UploadIcon()
        {
            string iconUrl = string.Empty;
            HttpPostedFileBase file = Request.Files.Count > 0 ? Request.Files[0] : null;
            if (file != null)
            {
                try
                {
                    using (WorkFlowApiClient client = new WorkFlowApiClient())
                    {
                        byte[] fileBytes = new byte[file.InputStream.Length];
                        int byteCount = file.InputStream.Read(fileBytes, 0, (int)file.InputStream.Length);
                        string fileContent = Convert.ToBase64String(fileBytes);
                        JObject obj = JObject.Parse(client.UploadImg(file.FileName, fileContent));
                        if (int.Parse(obj["ret_code"].ToString()) == 1)
                        {
                            iconUrl = obj["fileName"].ToString();
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
            return iconUrl;
        }

        [HttpPost]
        public ActionResult AddNewTemplate(NewTemplateModel model)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            int flowTypeId = manager.CreateNewTemplate(model.name, model.dep, model.grade, model.templateType, model.iconUrl, model.platform, model.tabs);
            if (flowTypeId > 0)
            {
                return RedirectToAction("Edit", new { flowTypeId = flowTypeId });
            }
            return this.ShowErrorInModal(StringResource.UNABLE_CREATE_NEW_TEMPLATE);
        }

        [HttpPost]
        public ActionResult AddFlow(int flowGroupId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            var newflow = new WF_Flows
            {
                FlowGroupId = flowGroupId,
                UserNo = this.Username,
                Title = string.Empty,
                ConditionRelation = "AND",
                LastUpdated = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                StatusId = 1
            };
            WFEntities.WF_Flows.Add(newflow);
            WFEntities.SaveChanges();
            ViewBag.Conditions = manager.GetFlowConditions(newflow.FlowId);
            return PartialView("_FlowSteps", manager.GetFlowInfo(newflow.FlowId));
        }

        public ActionResult RemoveFlow(int flowId)
        {
            return Json(new { success = ExecuteRemoveFlow(flowId) > 0, flowId });
        }

        private int ExecuteRemoveFlow(int id)
        {
            int result = WFEntities.Database.ExecuteSqlCommand(
                @"UPDATE dbo.WF_Flows SET StatusId = 0, LastUpdated = GETUTCDATE() WHERE FlowId = @p0
UPDATE dbo.WF_StepGroups SET StatusId = 0 WHERE FlowId = @p0
UPDATE dbo.WF_FlowSteps SET StatusId = 0 WHERE StepGroupId IN (SELECT StepGroupId FROM dbo.WF_StepGroups WHERE FlowId = @p0)
UPDATE dbo.WF_FlowConditions SET StatusId = 0 WHERE FlowId = @p0
UPDATE dbo.WF_StepGroupConditions SET StatusId = 0 WHERE StepGroupId IN (SELECT StepGroupId FROM dbo.WF_StepGroups WHERE FlowId = @p0)
UPDATE dbo.WF_StepNotificateUsers SET StatusId = 0 WHERE StepGroupId IN (SELECT StepGroupId FROM dbo.WF_StepGroups WHERE FlowId = @p0)",
                id);
            return result;
        }

        public ActionResult AddFlowCondition(int flowId)
        {
            ViewBag.Properties = WFEntities.WF_Flows.Where(p => p.FlowId == flowId).SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys.Where(q => q.StatusId != 0)).AsNoTracking().ToArray();
            ViewBag.FlowId = flowId;
            return this.ModalView("_Condition");
        }

        [HttpPost]
        public ActionResult AddFlowCondition(WFCondition model)
        {
            if (!ModelState.IsValid)
                return this.ShowErrorInModal(StringResource.INVALID_PARAMETER);
            if (!model.ComparedData.EqualsIgnoreCaseAndBlank("id") &&
                !model.ComparedData.EqualsIgnoreCaseAndBlank("grade"))
            {
                int propertyid;
                if (int.TryParse(model.ComparedData, out propertyid))
                {
                    WF_FlowPropertys property =
                        WFEntities.WF_FlowPropertys.FirstOrDefault(p => p.StatusId != 0 && p.FlowPropertyId == propertyid);
                    if (property == null)
                        return this.ShowErrorInModal(StringResource.FIELD_NOT_FOUND);
                    FlowCondition condition = new FlowCondition(property, model.Operator, model.Value.Trim());
                    if (!condition.IsValid())
                        return this.ShowErrorInModal(StringResource.INVALID_CONDITION_VALUE);
                    //if (WFEntities.WF_FlowConditions.Any(
                    //    p =>
                    //        p.FlowId == model.FlowId && p.StatusId > 0 && p.FlowPropertyId == propertyid &&
                    //        p.Operator == model.Operator))
                    //    return this.ShowErrorInModal(StringResource.DUPLICATE_CONDITIONS);
                }
            }
            else
            {
                int temp;
                if (model.ComparedData.EqualsIgnoreCaseAndBlank("grade") && !int.TryParse(model.Value, out temp))
                    return this.ShowErrorInModal(StringResource.INVALID_CONDITION_VALUE);
                //if (WFEntities.WF_FlowConditions.Any(
                //        p =>
                //            p.FlowId == model.FlowId && p.StatusId > 0 && p.OtherPropertyType == model.ComparedData &&
                //            p.Operator == model.Operator))
                //    return this.ShowErrorInModal(StringResource.DUPLICATE_CONDITIONS);
            }
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            if (manager.SaveFlowCondition(model))
            {
                manager.UpdateLastUpdated(model.FlowId);
                ViewBag.Conditions = manager.GetFlowConditions(model.FlowId);
                return PartialView("_FlowSteps", manager.GetFlowInfo(model.FlowId));
            }
            return this.ShowErrorInModal(StringResource.SAVE_CONDITION_FAILED);
        }

        public ActionResult AddStepCondition(int stepGroupId, int flowId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            ViewBag.Properties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId != 0 && p.WF_FlowGroups.WF_Flows.Any(q => q.FlowId == flowId)).AsNoTracking().ToArray();
            ViewBag.StepGroupId = stepGroupId;
            ViewBag.FlowId = flowId;
            ViewBag.GroupConditions = manager.GetStepGroupConditions(stepGroupId);

            var nextGroup =
                WFEntities.WF_StepGroups.AsNoTracking()
                    .Where(
                        p =>
                            p.FlowId == flowId && p.StatusId > 0 && p.StatusId > 0)
                    .OrderBy(p => p.OrderId).ToArray();
            List<SelectListItem> nextGroupList = new List<SelectListItem>();
            for (int i = 0; i < nextGroup.Length; i++)
            {
                nextGroupList.Add(new SelectListItem
                {
                    Text = $"Step {i + 2}",
                    Value = nextGroup[i].StepGroupId.ToString()
                });
            }
            ViewBag.NextGroupList = nextGroupList;

            return View("_StepCondition", "~/Views/Shared/_ModalLayout.cshtml");
        }

        [HttpPost]
        public ActionResult AddStepCondition(WFStepCondition model)
        {
            if (!ModelState.IsValid)
                return Json(new { error = StringResource.INVALID_PARAMETER });
            //目前只验证属性，applicantgrade和approvergrade不需要验证
            if (model.DataKey >= 0)
            {
                WF_FlowPropertys property = WFEntities.WF_FlowPropertys.FirstOrDefault(p => p.StatusId != 0 && p.FlowPropertyId == model.DataKey);
                if (property == null)
                    return Json(new { error = StringResource.FIELD_NOT_FOUND });

                FlowCondition condition = new FlowCondition(property, model.Operator, model.Value.Trim());
                if (!condition.IsValid())
                    return Json(new { error = StringResource.INVALID_VALUE });
            }
            if (WFEntities.WF_StepGroupConditions.Any(p => p.StepGroupId == model.StepGroupId && p.StatusId > 0 && p.DataKey == model.DataKey && p.Operator == model.Operator))
                return Json(new { error = StringResource.DUPLICATE_CONDITIONS });
            WFEntities.WF_StepGroupConditions.Add(new WF_StepGroupConditions
            {
                Created = DateTime.UtcNow,
                StepGroupId = model.StepGroupId,
                DataKey = model.DataKey,
                LastUpdated = DateTime.UtcNow,
                Operator = model.Operator,
                StatusId = 1,
                Value = model.Value,
                MaxValue = model.MaxValue,
                NextStepGroupId = model.NextStepGroupId
            });
            WFEntities.SaveChanges();
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.UpdateLastUpdated(model.FlowId);
            ViewBag.Conditions = manager.GetFlowConditions(model.FlowId);
            return PartialView("_FlowSteps", manager.GetFlowInfo(model.FlowId));
        }

        public ActionResult AddNewStepCondition(int flowId)
        {
            ViewBag.Properties = WFEntities.WF_Flows.Where(p => p.StatusId != 0 && p.FlowId == flowId).SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys).Where(p => p.StatusId != 0).AsNoTracking().ToArray();
            ViewBag.FlowId = flowId;
            ViewBag.DisplayButtons = false;
            ViewBag.DisplaySelfButtons = true;

            var nextGroup =
                WFEntities.WF_StepGroups.AsNoTracking().Where(p => p.FlowId == flowId && p.StatusId > 0).OrderBy(p => p.OrderId).ToArray();
            List<SelectListItem> nextGroupList = new List<SelectListItem>();
            for (int i = 0; i < nextGroup.Length; i++)
            {
                nextGroupList.Add(new SelectListItem
                {
                    Text = $"Step {i + 2}",
                    Value = nextGroup[i].StepGroupId.ToString()
                });
            }
            ViewBag.NextGroupList = nextGroupList;

            return View("_NewStepCondition", "~/Views/Shared/_ModalLayout.cshtml");
        }

        public ActionResult ChangeGroupCondition(int id, int flowId)
        {
            ViewBag.StepGroupId = id;
            ViewBag.FlowId = flowId;
            ViewBag.CondtitionId = WFEntities.WF_StepGroups.FirstOrDefault(p => p.StepGroupId == id)?.ConditionId;
            return View("SelectGroupCondition", "~/Views/Shared/_LargeModalLayout.cshtml");
        }

        [HttpPost]
        public ActionResult ChangeGroupCondition(int stepGroupId, int flowId, int condition)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            var target = WFEntities.WF_StepGroups.First(p => p.StepGroupId == stepGroupId);
            if (target != null)
            {
                target.ConditionId = condition;
                WFEntities.SaveChanges();
                manager.UpdateLastUpdated(flowId);
                ViewBag.Conditions = manager.GetFlowConditions(flowId);
                return PartialView("_FlowSteps", manager.GetFlowInfo(flowId));
            }
            return Json(new { error = StringResource.UNABLE_CHANGE_CONDITION });
        }

        public bool RemoveStepCondition(int id, int flowId)
        {
            var target = WFEntities.WF_StepGroupConditions.First(p => p.StepGroupConditionId == id);
            if (target != null)
            {
                target.StatusId = 0;
                if (WFEntities.SaveChanges() > 0)
                {
                    TemplateManager manager = new TemplateManager(WFEntities, this.Username);
                    manager.UpdateLastUpdated(flowId);
                    return true;
                }
            }
            return false;
        }

        public ActionResult RemoveFlowCondition(int flowConditionId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            var target = WFEntities.WF_FlowConditions.Where(p => p.FlowConditionId == flowConditionId).Select(p => new { Condition = p, FlowId = p.WF_Flows.FlowId }).FirstOrDefault();
            target.Condition.StatusId = 0;
            WFEntities.SaveChanges();
            manager.UpdateLastUpdated(target.FlowId);
            ViewBag.Conditions = manager.GetFlowConditions(target.FlowId);
            return PartialView("_FlowSteps", manager.GetFlowInfo(target.FlowId));
        }

        public ActionResult Save(int flowGroupId)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.SetFlowGroupEffective(flowGroupId);
            return RedirectToAction("Index");
        }

        public ActionResult EditProperty(int propertyid, int flowgroupid)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            WF_FlowTypes flowtype = manager.GetFlowTypeByGroupId(flowgroupid);
            ViewBag.FlowType = flowtype;
            ViewBag.PropertyId = propertyid;
            WF_FlowPropertys prop = manager.GetProperty(propertyid);
            ViewBag.CodeList = WFEntities.WF_TemplatePropCode.Where(
                p => p.TemplateType == flowtype.TemplateType && p.StatusId > 0)
                .Select(p => new SelectListItem
                {
                    Text = p.PropCode,
                    Value = p.TemplatePropCodeId.ToString(),
                    Selected = p.TemplatePropCodeId == prop.Code
                }).ToArray();
            return View("EditProperty", "~/Views/Shared/_ModalLayout.cshtml", new NewPropertyModel
            {
                fieldTypeId = prop.PropertyType,
                compulsory = prop.Compulsory,
                datasource = prop.DataSource,
                fieldName = prop.PropertyName,
                RowIndex = prop.RowIndex,
                ColumnIndex = prop.ColumnIndex,

                Width = prop.Width,
                Height = prop.Height,
                FontSize = prop.FontSize,
                FontColor = prop.FontColor,
                Validation = prop.Validation,
                HAlign = prop.HAlign,

                ViewType = prop.ViewType,
                Tab = prop.Tab,
                Text = prop.Text,
            });
        }

        [HttpPost]
        public ActionResult EditProperty(int propertyid, NewPropertyModel model)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            WF_FlowGroups @group = manager.GetFlowGroupByPropertyId(propertyid);
            WF_FlowTypes flowtype = manager.GetFlowTypeByGroupId(@group.FlowGroupId);
            if (flowtype.TemplateType == 9)
            {
                model.fieldName = "";
                model.code = null;
            }
            manager.UpdateProperty(propertyid, model);
            ViewBag.FlowGroup = @group;
            ViewBag.FlowType = flowtype;
            ViewBag.Properties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId > 0 && p.FlowGroupId == @group.FlowGroupId).AsNoTracking().ToArray();
            return PartialView("_ProperyTable");
        }

        public ActionResult ChangeFlowConditionRelation(int flowId)
        {
            TemplateManager tm = new TemplateManager(WFEntities, this.Username);
            var flow = tm.GetFlowInfo(flowId);
            ViewBag.FlowId = flowId;
            ViewBag.Relation = flow.ConditionRelation;
            return this.ModalView("SelectFlowCondition");
        }

        [HttpPost]
        public ActionResult ChangeFlowConditionRelation(int flowId, string relation)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            var target = WFEntities.WF_Flows.First(p => p.FlowId == flowId);
            if (target != null)
            {
                target.ConditionRelation = relation.EqualsIgnoreCaseAndBlank("OR") ? "OR" : "AND";
                WFEntities.SaveChanges();
                manager.UpdateLastUpdated(flowId);
                ViewBag.Conditions = manager.GetFlowConditions(flowId);
                return PartialView("_FlowSteps", manager.GetFlowInfo(flowId));
            }
            return this.ShowErrorInModal(StringResource.UNABLE_CHANGE_CONDITION_RELATION);
        }

        [HttpPost]
        public ActionResult UpdateSpecialProperty(NewPropertyModel model, bool enable)
        {
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            WF_FlowPropertys property = manager.UpdateSpecialProperty(model, enable);
            if (property != null)
            {
                WF_FlowGroups @group = manager.GetFlowGroupByPropertyId(property.FlowPropertyId);
                ViewBag.FlowGroup = @group;
                ViewBag.Properties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId > 0 && p.FlowGroupId == @group.FlowGroupId).AsNoTracking().ToArray();
                WF_FlowTypes flowtype = manager.GetFlowTypeByGroupId(@group.FlowGroupId);
                ViewBag.FlowType = flowtype;
                if (flowtype.TemplateType.HasValue)
                {
                    ViewBag.SpecialProperties = WFEntities.WF_FlowPropertys.Where(p => p.StatusId < 0 && p.FlowGroupId == group.FlowGroupId).AsNoTracking().ToArray();
                    ViewBag.AllSpecialProperties = manager.GetSpecialPropertiesFromXml(flowtype.TemplateType.Value);
                }
                return PartialView("_ProperyTable");
            }
            return Json(new { error = StringResource.UPDATE_FIELD_FAILED });
        }

        public ActionResult CountryArchivePathRow(string countryCode, string path)
        {
            var model = new KeyValuePair<string, string>(countryCode, path);
            return PartialView("_CountryArchivePathRow", model);
        }

        [HttpPost]
        public bool UpdateConfiguration(TemplateConfigurationModel model)
        {
            WF_FlowGroups group = WFEntities.WF_FlowGroups.FirstOrDefault(p => p.FlowGroupId == model.FlowGroupId);
            if (group != null)
            {
                group.HasCoverUsers = model.HasCoverUsers;
                group.ApprovedArchivePath = model.ApprovedArchivePath;

                var contryCodes = model.CountryArchivePaths.Select(x => x.CountryCode.ToLower()).ToArray();
                var delete = WFEntities.WF_CountryArchivePaths.Where(x => x.FlowGroupId == model.FlowGroupId && !contryCodes.Contains(x.CountryCode.ToLower()));
                WFEntities.WF_CountryArchivePaths.RemoveRange(delete);

                WF_CountryArchivePaths path;
                foreach (var item in model.CountryArchivePaths)
                {
                    path = WFEntities.WF_CountryArchivePaths.FirstOrDefault(x => x.FlowGroupId == model.FlowGroupId && x.CountryCode == item.CountryCode);
                    if (path == null)
                    {
                        path = new WF_CountryArchivePaths() { FlowGroupId = model.FlowGroupId, CountryCode = item.CountryCode };
                        WFEntities.WF_CountryArchivePaths.Add(path);
                    }
                    path.ApprovedArchivePath = item.ApprovedArchivePath;
                }

                WFEntities.SaveChanges();
                return true;
            }
            return false;
        }

        public ActionResult DownloadSampleExcel()
        {
            return File(Server.MapPath("~/App_Data/DynamicFromDesign.xlsx"),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DynamicFromDesign.xlsx");
        }

        [HttpPost]
        public ActionResult RecieveFieldsFile(int flowGroupId)
        {
            string error;
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            WF_FlowTypes flowtype = manager.GetFlowTypeByGroupId(flowGroupId);
            var properties = ReadFromExcel(row => new NewPropertyModel
            {
                flowGroupId = flowGroupId,
                Tab = row[0].ToString().Trim(),
                RowIndex = ParseInt(row[1]),
                ColumnIndex = ParseInt(row[2]),
                ViewType = row[3].ToString().Trim(),
                fieldTypeId = GetFieldType(row[4]),
                compulsory = GetBool(row[5]),
                Validation = row[6].ToString().Trim(),
                Width = GetInt(row[7]),
                Height = GetInt(row[8]),
                HAlign = row[9].ToString().Trim(),
                Text = row[10].ToString().Trim(),
                FontSize = GetInt(row[11]),
                FontColor = row[12].ToString(),
                datasource = row[13].ToString().Trim(),
                Multiple = GetBool(row[14])
            }, out error);
            foreach (NewPropertyModel model in properties)
            {
                if (flowtype.TemplateType == 9)
                {
                    model.fieldName = "";
                    model.code = null;
                }
                manager.SaveNewProperty(model);
            }
            return RedirectToAction("Edit", new { flowTypeId = flowtype.FlowTypeId });
        }

        private int? GetInt(Cell p0)
        {
            int a;
            if (int.TryParse(p0.ToString().Trim('X', 'x'), out a))
            {
                return a;
            }
            return null;
        }

        private bool GetBool(Cell cell)
        {
            return cell.ToString().EqualsIgnoreCaseAndBlank("yes");
        }

        private int GetFieldType(Cell cell)
        {
            if (cell.ToString().EqualsIgnoreCaseAndBlank("int"))
                return (int)PropertyTypes.Int;
            if (cell.ToString().EqualsIgnoreCaseAndBlank("datetime"))
                return (int)PropertyTypes.DateTime;
            if (cell.ToString().EqualsIgnoreCaseAndBlank("date"))
                return (int)PropertyTypes.Date;
            return (int)PropertyTypes.Text;
        }

        private int? ParseInt(Cell cell)
        {
            int p;
            if (int.TryParse(cell.ToString().Trim(), out p))
            {
                return p;
            }
            return null;
        }

        public ActionResult RemovePropertys(int[] propertyIds)
        {
            if (propertyIds == null || propertyIds.Length == 0)
                return Json(new { error = StringResource.PLEASE_SELECT_A_VALUE });
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            manager.RemoveProperty(propertyIds);
            int first = propertyIds[0];
            int flowTypeId = WFEntities.WF_FlowPropertys.Where(p => p.FlowPropertyId == first).Select(p => p.WF_FlowGroups.FlowTypeId).FirstOrDefault();
            return RedirectToAction("Edit", new { flowTypeId = flowTypeId });
        }
        [HttpGet]
        public ActionResult EditTemplatePropertyies(int flowTypeId)
        {
            ViewBag.FlowTypeId = flowTypeId;
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            WF_FlowTypes flowType = manager.GetFlowTypeById(flowTypeId);
            return (this).ModalView("EditTemplate",new NewTemplateModel
            {
                tabs = flowType.Tabs,
                dep = flowType.Dep,
                grade = flowType.Grade,
                iconUrl = flowType.IconUrl,
                name = flowType.Name,
                platform = flowType.Platform,
                templateType = flowType.TemplateType,
            });
        }
        [HttpPost]
        public ActionResult EditTemplatePropertyies(int flowTypeId,NewTemplateModel model)
        {
            ViewBag.FlowTypeId = flowTypeId;
            TemplateManager manager = new TemplateManager(WFEntities, this.Username);
            bool result = manager.SaveTemplatePropertyies(flowTypeId, model);
            if (!result)
            {
                return this.ShowErrorModal("修改失败");
            }

            return this.CloseModalView();

        }

        public ActionResult AddNoApproverCondition(int flowid)
        {
            var nNoApproverDataItems = WFEntities.WF_Flows.Where(p => p.FlowId == flowid)
                .SelectMany(p => p.WF_FlowGroups.WF_FlowPropertys.Where(q => q.StatusId != 0)).AsNoTracking().Select(
                    p => new SelectListItem
                    {
                        Text = p.PropertyName,
                        Value = p.FlowPropertyId.ToString()
                    })
                .ToList();
            nNoApproverDataItems.Insert(0, new SelectListItem
            {
                Text = "Applicant Grade",
                Value = $"{ExtraProperty.ApplicantGrade:d}"
            });
            nNoApproverDataItems.Insert(0, new SelectListItem
            {
                Text = "Approver Grade",
                Value = $"{ExtraProperty.ApproverGrade:d}"
            });
            nNoApproverDataItems.Insert(0, new SelectListItem
            {
                Text = "Approver Staff No",
                Value = $"{ExtraProperty.ApproverStaffNo:d}"
            });
            ViewData["NoApproverDataItems"] = nNoApproverDataItems;
            ViewData["Operators"] = new List<string>(FlowCondition.Operators);
            ViewData["Editable"] = true;
            return PartialView("_NoApproverCondition");
        }

        public ActionResult AddSecretaryRule()
        {
            ViewData["Editable"] = true;
            return PartialView("_secretaryRule");
        }
    }
}