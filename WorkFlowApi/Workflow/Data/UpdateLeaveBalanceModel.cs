namespace Omnibackend.Api.Workflow.Data
{
    public class UpdateLeaveBalanceModel
    {
        public string country_code { get; set; }
        public string staff_id { get; set; }
        //请假的天数
        public float days { get; set; }
    }
}
