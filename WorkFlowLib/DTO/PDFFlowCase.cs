using WorkFlowLib.Data;
using System.Collections.Generic;

namespace WorkFlowLib.DTO
{
    public class PDFFlowCase
    {
        public FlowInfo FlowInfo { get; set; }
        public PropertiesValue PropertiesValue { get; set; }
        public WF_FlowCases_Attachments[] Attachments { get; set; }
        public CaseLog[] CaseLogs { get; set; }
        public WF_FlowTypes FlowType { get; set; }
        public CaseNotification[] Comments { get; set; }
        public Dictionary<string, string> BLSShopViews { get; set; }
        
    }
}
