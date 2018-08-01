namespace WorkFlowLib.Results
{
    public class UserStaffSearchResult : UserRoleSearchResult
    {
        public string POSITION { get; set; }
        public string POSITION_NAME { get; set; }
        public string EMAIL { get; set; }
        public string IPT { get; set; }
        public string CELLPHONE { get; set; }
        public string COUNTRY { get; set; }
        public double? LEAVEBALANCE { get; set; }
    }
}
