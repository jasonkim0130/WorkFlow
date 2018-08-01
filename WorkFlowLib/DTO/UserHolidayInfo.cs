namespace WorkFlowLib.DTO
{
    public class UserHolidayInfo
    {
        public string Date { get; set; }
        //可能的值有AM PM ALL,ALL表示全天都是假期
        public string Time { get; set; }
        public string Remark { get; set; }
    }
}
