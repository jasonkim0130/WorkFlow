using System;

namespace WorkFlowLib.DTO
{
    public class CaseHistory
    {
        public int FlowCaseId { get; set; }
        public int? Ver { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Subject { get; set; }
    }
}