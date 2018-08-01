using System.Web;
using Dreamlab.Core;

namespace WorkFlow.Models
{
    public class SubMenu
    {
        public string Name { get; set; }
        public bool Active { get; set; }
        public string ActionName { get; set; }
        public bool Loaded { get; set; }
        public object RouteValues { get; set; }
        public int? Badge { get; set; }
        public static string GetSubLayout(HttpRequestBase request)
        {
            string s = request.QueryString["source"];
            if (s.EqualsIgnoreCaseAndBlank("main"))
            {
                return "~/Views/Shared/_SubLayout.cshtml";
            }
            return null;
        }
    }
}