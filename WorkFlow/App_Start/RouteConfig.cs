using System.Web.Mvc;
using System.Web.Routing;
using WorkFlow.Ext;
using WorkFlowLib;

namespace WorkFlow
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.Add(new Route(
                  "{lang}/{controller}/{action}/{id}",
                  new RouteValueDictionary(new
                  {
                      lang = Codehelper.GetLang(Codehelper.DefaultCountry),
                      controller = "Account",
                      action = "Logon",
                      id = UrlParameter.Optional
                  }),
                  new MultiLangRouteHandler()));
        }
    }
}
