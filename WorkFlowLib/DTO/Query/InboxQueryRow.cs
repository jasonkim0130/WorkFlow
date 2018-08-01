using System;

namespace WorkFlowLib.DTO.Query
{
    public class InboxQueryRow : FlowCaseInfo
    {
        public string Type { get; set; }
        public int Flag { get; set; }
        public int Priority { get; set; }
        public bool HasAttachment { get; set; }
        public bool IsRead { get; set; }
        public DateTime Received { get; set; }
    }
}