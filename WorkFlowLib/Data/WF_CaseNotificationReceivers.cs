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
    
    public partial class WF_CaseNotificationReceivers
    {
        public int CaseNotificationReceiverId { get; set; }
        public int CaseNotificationId { get; set; }
        public Nullable<int> SourceType { get; set; }
        public string Receiver { get; set; }
        public int IsRead { get; set; }
        public int IsDissmissed { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public System.DateTime Created { get; set; }
        public int StatusId { get; set; }
    
        public virtual WF_CaseNotifications WF_CaseNotifications { get; set; }
    }
}