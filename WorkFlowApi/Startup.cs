using Microsoft.Owin;
using Omnibackend.Api;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace Omnibackend.Api
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
