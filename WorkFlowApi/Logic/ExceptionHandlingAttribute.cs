using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using Newtonsoft.Json;

namespace Omnibackend.Api.Logic
{
    public class ExceptionHandlingAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            string msg = string.IsNullOrEmpty(context.Exception.Message) ? "接口出现了错误，请重试或者联系管理员" : context.Exception.Message;
            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new ApiResult { ret_msg = msg, ret_code = "F" })),
            });
        }
    }
}