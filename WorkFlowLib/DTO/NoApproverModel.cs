namespace WorkFlowLib.DTO
{
    public enum ExtraProperty
    {
        ApplicantGrade = -1,
        ApproverGrade = -2,
        ApproverStaffNo = -3
    }

    public class NoApproverModel
    {
        public int NoApproverDataKey { get; set; }
        public string NoApproverOperator { get; set; }
        public string NoApproverValue { get; set; }
        public string NoApproverMaxValue { get; set; }
    }
}
