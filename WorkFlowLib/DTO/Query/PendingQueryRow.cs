using System;

namespace WorkFlowLib.DTO.Query
{
    public class PendingQueryRow : FlowCaseInfo
    {
        public string Type { get; set; }
        public int TotalGroup { get; set; }
        public int CompletedGroup { get; set; }
        public DateTime SubmitDate { get; set; }
        public int IsFlagged { get; set; }
        public bool HasAttachment { get; set; }
    }
}