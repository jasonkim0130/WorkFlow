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
    
    public partial class WF_LastStepNotifyUser
    {
        public int LastStepNotifyUserId { get; set; }
        public int FlowId { get; set; }
        public Nullable<int> NotifyUserType { get; set; }
        public string UserNo { get; set; }
        public Nullable<int> UserRole { get; set; }
        public Nullable<int> ManagerOption { get; set; }
        public Nullable<int> ManagerLevel { get; set; }
        public Nullable<int> ManagerMaxLevel { get; set; }
        public string ManagerLevelOperator { get; set; }
        public string CriteriaGradeOperator { get; set; }
        public Nullable<int> CriteriaGrade { get; set; }
        public Nullable<int> CountryType { get; set; }
        public Nullable<int> DeptType { get; set; }
        public Nullable<int> BrandType { get; set; }
        public Nullable<int> DeptTypeSource { get; set; }
        public string FixedCountry { get; set; }
        public string FixedDept { get; set; }
        public string FixedBrand { get; set; }
        public string FixedDeptType { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public System.DateTime Created { get; set; }
        public int StatusId { get; set; }
    
        public virtual WF_Flows WF_Flows { get; set; }
    }
}
