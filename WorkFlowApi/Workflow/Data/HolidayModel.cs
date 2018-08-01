namespace Omnibackend.Api.Workflow.Data
{
    public class HolidayModel
    {
        public string country_code { get; set; }
        //查询的起始日期，格式yyyyMMdd
        public string from_date { get; set; }
        public string to_date { get; set; }
    }
}
