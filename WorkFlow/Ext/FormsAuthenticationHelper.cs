using System.Web;
using System.Web.Security;

namespace WorkFlow.Ext
{
    public class FormsAuthenticationHelper
    {
        public static void SetAuthCookie(string username, bool persistent, string userInfo)
        {
            HttpCookie cookie = FormsAuthentication.GetAuthCookie(username, persistent);
            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
            FormsAuthenticationTicket newTicket = new FormsAuthenticationTicket(ticket.Version, ticket.Name, ticket.IssueDate, ticket.Expiration, ticket.IsPersistent, userInfo, ticket.CookiePath);
            cookie.Value = FormsAuthentication.Encrypt(newTicket);
            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
}