//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace WorkFlowLib.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class WF_StepGroupConditions
    {
        public int StepGroupConditionId { get; set; }
        public int StepGroupId { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public System.DateTime Created { get; set; }
        public int StatusId { get; set; }
        public int DataKey { get; set; }
        public string MaxValue { get; set; }
        public Nullable<int> NextStepGroupId { get; set; }
    
        public virtual WF_StepGroups WF_StepGroups { get; set; }
    }
}
