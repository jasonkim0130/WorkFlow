using System;
using System.Collections.Generic;

namespace WorkFlowLib.DTO
{
    /**
    * Created by jeremy on 1/3/2017 9:25:01 PM.
    */
    public class FlowCaseInfo
    {
        public int FlowCaseId { get; set; }
        public int FlowId { get; set; }
        public IEnumerable<StepResult> StepResults { get; set; }
        public string Department { get; set; }
        public string Applicant { get; set; }
        public string Subject { get; set; }
        public DateTime? Approved { get; set; }
        public DateTime? Rejected { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? Aborted { get; set; }
        public DateTime? Cancelled { get; set; }
        public int? Ver { get; set; }
        public int? BaseFlowCaseId { get; set; }
        public int? RelatedFlowCaseId { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime? LastApproverViewed { get; set; }
        public DateTime? LastChecked { get; set; }
        public bool IsDraft { get; set; }
        public IEnumerable<string> CoverDuties { get; set; }
    }
}