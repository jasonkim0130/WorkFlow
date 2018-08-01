using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Omnibackend.Api.Workflow.Logic;
using Dreamlab.Core;
using Dreamlab.Core.Logs;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using WorkFlowLib;

namespace Omnibackend.Api
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            GlobalConfiguration.Configuration.Formatters.Remove(GlobalConfiguration.Configuration.Formatters.First(p => p is XmlMediaTypeFormatter));
            RegisterUnity();
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        public static void RegisterUnity()
        {
            IUnityContainer container = new UnityContainer();
            Singleton<IUnityContainer>.Instance = container;
            container.RegisterType<IUserManager, ApiUserManager>(new TransientLifetimeManager());
            container.RegisterType<ILogWritter, DbLogWriter>(new ContainerControlledLifetimeManager(), new InjectionConstructor());
            Singleton<ILogWritter>.Instance = container.Resolve<ILogWritter>();
            Singleton<IMessageLog>.Instance = new FlatTextMessageLogger(HttpContext.Current.Server.MapPath("~/Logs/log.txt"));
        }
    }
}
