namespace WorkFlowLib.DTO
{
    public class FlowStep
    {
        public string Approver { get; set; }
        public int FlowStepId { get; set; }
        public string Department { get; set; }
        public int OrderId { get; set; }
        public StepStatus StepStatus { get; set; }
        public int CaseStepId { get; set; }
        public int? ApproverType { get; set; }
        public string CriteriaGradeOperator { get; set; }
        public int? CriteriaGrade { get; set; }
        public string FinalDepartment { get; set; }
        public string FinalApprover { get; set; }
        //public string OperationUser { get; set; }
        public bool? NoApprover { get; set; }

        public string GetApprover()
        {
            //if ((StepStatus == StepStatus.Abort || StepStatus == StepStatus.Approved || StepStatus == StepStatus.Rejected) && !string.IsNullOrWhiteSpace(OperationUser))
            //{
            //    return OperationUser;
            //}
            if (ApproverType == (int)DTO.ApproverType.PredefinedRole)
            {
                return "Predefined Role";
            }
            if (ApproverType == (int)DTO.ApproverType.RoleCriteria)
            {
                return string.Join(" ", "Grade", CriteriaGradeOperator, CriteriaGrade?.ToString());
            }
            if (ApproverType == (int)DTO.ApproverType.PredefinedReportingLine)
            {
                return "Predefined Reporting Line";
            }
            return Approver;
        }
    }
}