namespace WorkFlowLib.DTO
{
    public class BaseNotificationUser
    {
        public int NotificateUserId { get; set; }
        public string UserNo { get; set; }
        public int? NotifyUserType { get; set; }
        public string CriteriaGradeOperator { get; set; }
        public int? CriteriaGrade { get; set; }

        public int? UserRole { get; set; }

        public int? ManagerOption { get; set; }
        public int? ManagerLevel { get; set; }
        public string ManagerLevelOperator { get; set; }
        public int? ManagerMaxLevel { get; set; }

        public int? CountryType { get; set; }
        public int? DeptType { get; set; }
        public int? BrandType { get; set; }
        public string FixedCountry { get; set; }
        public string FixedDept { get; set; }
        public string FixedBrand { get; set; }
        public int? DeptTypeSource { get; set; }
        public string FixedDeptType { get; set; }

        public int StatusId { get; set; }

        public string GetNotifyUser()
        {
            if (NotifyUserType == (int)ApproverType.PredefinedRole)
            {
                return "Predefined Role";
            }
            if (NotifyUserType == (int)ApproverType.RoleCriteria)
            {
                return string.Join(" ", "Grade", CriteriaGradeOperator, CriteriaGrade?.ToString());
            }
            if (NotifyUserType == (int)ApproverType.PredefinedReportingLine)
            {
                return "Predefined Reporting Line";
            }
            return UserNo;
        }
    }
}
