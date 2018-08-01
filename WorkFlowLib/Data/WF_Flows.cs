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
    
    public partial class WF_Flows
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public WF_Flows()
        {
            this.WF_ApplicantNotificationUsers = new HashSet<WF_ApplicantNotificationUsers>();
            this.WF_FlowConditions = new HashSet<WF_FlowConditions>();
            this.WF_StepGroups = new HashSet<WF_StepGroups>();
            this.WF_LastStepNotifyUser = new HashSet<WF_LastStepNotifyUser>();
            this.WF_FlowCases = new HashSet<WF_FlowCases>();
        }
    
        public int FlowId { get; set; }
        public int FlowVersion { get; set; }
        public string UserNo { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public System.DateTime Created { get; set; }
        public int StatusId { get; set; }
        public string Title { get; set; }
        public Nullable<int> FlowGroupId { get; set; }
        public Nullable<int> BaseFlowId { get; set; }
        public string ConditionRelation { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<WF_ApplicantNotificationUsers> WF_ApplicantNotificationUsers { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<WF_FlowConditions> WF_FlowConditions { get; set; }
        public virtual WF_FlowGroups WF_FlowGroups { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<WF_StepGroups> WF_StepGroups { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<WF_LastStepNotifyUser> WF_LastStepNotifyUser { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<WF_FlowCases> WF_FlowCases { get; set; }
    }
}
