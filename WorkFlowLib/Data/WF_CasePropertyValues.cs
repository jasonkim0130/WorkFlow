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
    
    public partial class WF_CasePropertyValues
    {
        public int CasePropertyValueId { get; set; }
        public int PropertyId { get; set; }
        public int FlowCaseId { get; set; }
        public string StringValue { get; set; }
        public Nullable<int> IntValue { get; set; }
        public Nullable<System.DateTime> DateTimeValue { get; set; }
        public Nullable<decimal> NumericValue { get; set; }
        public Nullable<System.DateTime> DateValue { get; set; }
        public string UserNoValue { get; set; }
        public string TextValue { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public System.DateTime Created { get; set; }
        public int StatusId { get; set; }
    
        public virtual WF_FlowPropertys WF_FlowPropertys { get; set; }
        public virtual WF_FlowCases WF_FlowCases { get; set; }
    }
}
