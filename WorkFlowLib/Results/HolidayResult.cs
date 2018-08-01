namespace WorkFlowLib.Results
{
    public class HolidayResult
    {
        public string DATE { get; set; }
        //TIME的取值有ALL / AM / PM
        //意思是返回ALL表示一整天都是假期
        public string TIME { get; set; }
        public string REMARK { get; set; }
    }
}
