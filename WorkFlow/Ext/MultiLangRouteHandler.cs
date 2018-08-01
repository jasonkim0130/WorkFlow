using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Dreamlab.Core;

namespace WorkFlow.Ext
{
    public class MultiLangRouteHandler : MvcRouteHandler
    {
        internal class CultureDesc
        {
            public string Name;
            public string Culture;
        }

        static readonly CultureDesc[] SupportedLanguages = new[]
        {
            new CultureDesc{ Name="SC",Culture="zh-CN"},
            new CultureDesc{ Name="TC",Culture="zh-TW"},
            new CultureDesc{ Name="KO",Culture="ko-KR"},
            new CultureDesc{ Name="EN",Culture="en-US"}
        };

        protected override IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            string lang = requestContext.RouteData.Values["lang"].ToString();
            CultureDesc item = SupportedLanguages.FirstOrDefault(p => p.Name.EqualsIgnoreCase(lang));
            if (item != null)
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(item.Culture);
            }
            return base.GetHttpHandler(requestContext);
        }
    }
}