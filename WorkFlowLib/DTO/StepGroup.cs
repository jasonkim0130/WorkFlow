using System.Collections.Generic;

namespace WorkFlowLib.DTO
{
    public class StepGroup
    {
        public int StepConditionId { get; set; }
        public int StepGroupId { get; set; }
        public int OrderId { get; set; }
        public IEnumerable<FlowStep> Steps { get; set; }
        public IEnumerable<StepNotificationUser> NotificationUsers { get; set; }
        public StepStatus StepStatus { get; set; }
    }
}