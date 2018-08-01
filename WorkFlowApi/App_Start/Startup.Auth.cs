using System;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Omnibackend.Api.Providers;
using Owin;

namespace Omnibackend.Api
{
    public partial class Startup
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public void ConfigureAuth(IAppBuilder app)
        {
            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/Token"),
                Provider = new OmniOAuthProvider(),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(10),
                // In production mode set AllowInsecureHttp = false
                AllowInsecureHttp = true
            };
            app.UseOAuthBearerTokens(OAuthOptions);
        }
    }
}
