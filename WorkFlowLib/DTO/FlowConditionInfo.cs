using System.Collections.Generic;
using WorkFlowLib.Data;

namespace WorkFlowLib.DTO
{
    public class FlowConditionInfo
    {
        public IEnumerable<WF_FlowConditions> FlowConditions { get; set; }
        public IEnumerable<WF_StepGroupConditions> StepGroupConditons { get; set; }
        public int FlowId { get; set; }
    }
}