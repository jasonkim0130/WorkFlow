using System.Collections.Generic;
using System.Linq;
using WorkFlowLib.Data;

namespace WorkFlowLib.DTO
{
    /**
    * Created by jeremy on 1/3/2017 8:37:06 PM.
    */
    public class FlowInfo
    {
        public FlowCaseInfo CaseInfo { get; set; }
        public int FlowId { get; set; }
        public IEnumerable<StepGroup> StepGroups { get; set; }
        public string TypeName { get; set; }
        public string Title { get; set; }
        public int FlowTypeId { get; set; }
        public int GroupVersion { get; set; }
        public int? BaseFlowId { get; set; }
        public int FlowGroupId { get; set; }
        public string ConditionRelation { get; set; }
        public IEnumerable<ApplicantNotificationUser> ApplicantNotificationUsers { get; set; }
        public IEnumerable<LastStepNotifyUser> LastStepNotifyUsers { get; set; }

        public void Initialize(FlowCaseInfo caseinfo, WF_CaseSteps[] caseSteps/*, Dictionary<int,string>opUsersDict*/)
        {
            if (caseinfo == null)
                return;
            CaseInfo = caseinfo;
            foreach (StepGroup @group in StepGroups)
            {
                foreach (FlowStep step in @group.Steps)
                {
                    WF_CaseSteps casestep = caseSteps.FirstOrDefault(p => p.FlowStepId == step.FlowStepId);
                    if (casestep != null)
                    {
                        step.FinalDepartment = casestep.Department;
                        step.FinalApprover = casestep.UserNo;
                        step.CaseStepId = casestep.CaseStepId;
                    }
                    StepResult stepValue = caseinfo.StepResults.FirstOrDefault(p => p.FlowStepId == step.FlowStepId);
                    if (stepValue != null)
                    {
                        if (stepValue.Status == 1)
                            step.StepStatus = StepStatus.Approved;
                        else if (stepValue.Status == 2)
                            step.StepStatus = StepStatus.Rejected;
                        else if (stepValue.Status == 3)
                            step.StepStatus = StepStatus.Abort;
                        else
                            step.StepStatus = StepStatus.None;
                    }
                }
                if (@group.StepConditionId == 1 && @group.Steps.All(p => p.StepStatus == StepStatus.Approved))
                {
                    @group.StepStatus = StepStatus.Approved;
                }
                else if (@group.StepConditionId == 2 && @group.Steps.Any(p => p.StepStatus == StepStatus.Approved))
                {
                    @group.StepStatus = StepStatus.Approved;
                }
                else if (@group.StepConditionId == 1 && @group.Steps.Any(p => p.StepStatus == StepStatus.Rejected))
                {
                    @group.StepStatus = StepStatus.Rejected;
                }
                else if (@group.StepConditionId == 2 && @group.Steps.All(p => p.StepStatus == StepStatus.Rejected))
                {
                    @group.StepStatus = StepStatus.Rejected;
                }
            }
        }
        public bool IsAllApproved()
        {
            return StepGroups.All(@group => @group.StepStatus == StepStatus.Approved);
        }
        public StepGroup GetNextStepGroup(int flowStepId)
        {
            StepGroup @group = StepGroups.FirstOrDefault(p => p.Steps.Any(q => q.FlowStepId == flowStepId));
            if (@group != null)
            {
                var nextGroup = StepGroups.OrderBy(p => p.OrderId).FirstOrDefault(p => p.OrderId > @group.OrderId);
                if (nextGroup != null)
                {
                    return nextGroup;
                }
            }
            return null;
        }
    }
}