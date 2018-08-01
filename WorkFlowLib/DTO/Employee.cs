namespace WorkFlowLib.DTO
{
    /**
    * Created by jeremy on 3/8/2017 12:43:22 PM.
    */
    public class Employee
    {
        public Employee(string userNo, string name)
        {
            UserNo = userNo;
            Name = name;
        }

        public Employee()
        {

        }

        public string UserNo { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// 用来排序
        /// </summary>
        public string Country { get; set; }
    }
}