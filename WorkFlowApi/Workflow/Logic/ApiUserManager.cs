using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using Dreamlab.Core;
using WorkFlowLib;

namespace Omnibackend.Api.Workflow.Logic
{
    public class ApiUserManager : UserManager
    {
        public static string GetCountry(IPrincipal user)
        {
            try
            {
                string country = ((ClaimsIdentity)user.Identity).Claims.FirstOrDefault(
                    p => p.Type.EqualsIgnoreCaseAndBlank("Country"))?.Value;
                country = country ?? "TWN";
                country = country.ToUpper().Trim();
                return country;
            }
            catch (Exception ex)
            {
            }
            return "TWN";
        }

        public override ApiClient CreateApiClient()
        {
            string country = ApiCountry ?? GetCountry(HttpContext.Current?.User);
            return new ApiClient($"{ConfigurationManager.AppSettings[country + "ApiHost"].Trim('/')}:1011");
        }

        public string ApiCountry { get; set; }
    }
}