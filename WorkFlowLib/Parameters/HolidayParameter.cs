namespace WorkFlowLib.Parameters
{
    public class HolidayParameter
    {
        //国家编号
        public string AS_CUTY { get; set; }
        //查询的起始日期
        //格式如:20170101
        public string AS_FRDT { get; set; }
        //查询的结束日期，格式同上
        public string AS_TODT { get; set; }
    }
}
