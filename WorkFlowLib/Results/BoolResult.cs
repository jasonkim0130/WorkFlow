using System;
namespace WorkFlowLib.Results
{
    public class BoolResult
    {
        public const string Success = "S";
        public const string Failure = "F";
        public string ret_code { get; set; }
        public string ret_msg { get; set; }
        public string[] GetErrorMsgPropertyName()
        {
            return new [] { "ret_msg" };
        }
        public string GetErrorMsg()
        {
            if (ret_msg != null && ret_msg.IndexOf("NOT FOUND DATA", StringComparison.InvariantCultureIgnoreCase) > 0)
            {
                return "NOT FOUND DATA";
            }
            return ret_msg;
        }
        public bool IsSuccess()
        {
            return ret_code == Success;
        }
    }
}
