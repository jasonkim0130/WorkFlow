namespace Omnibackend.Api.Logic
{
    public class ApiResult
    {
        public string ret_code { get; set; }
        public string ret_msg { get; set; }
        public string ret_action { get; set; }

        public static implicit operator ApiResult(string error)
        {
            return new ApiResult() { ret_code = "F", ret_msg = error };
        }

        public static implicit operator ApiResult(bool result)
        {
            return new ApiResult() { ret_code = result ? "S" : "F" };
        }
    }

    public class ApiResult<T>
    {
        public string ret_code { get; set; }
        public string ret_msg { get; set; }
        public T data { get; set; }

        public static implicit operator ApiResult<T>(string error)
        {
            return new ApiResult<T>() { ret_code = "F", ret_msg = error };
        }
    }
}