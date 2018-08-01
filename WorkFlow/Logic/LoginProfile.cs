using Dreamlab.Core;
using System;
using System.Web;
using WorkFlowLib;

namespace WorkFlow.Logic
{
    /**
    * Created by jeremy on 2/14/2017 9:12:41 AM.
    */

    public class LoginProfile
    {
        public string Lang { get; set; }
        public string Username { get; set; }
        public string Country { get; set; }
        public DateTime Expires { get; set; }
        public const string SwitchSiteSignKey = "90ff7e7a-2c5b-44d2-97ef-c41bee30ccd2";

        public LoginProfile(string username, string country, string lang)
        {
            Username = username;
            Country = country;
            Lang = lang;
            Expires = DateTime.UtcNow.AddMinutes(20);
        }

        public override string ToString()
        {
            string str = Username + "," + Country + "," + Lang + "," + Expires.ToString("yyyy-MM-dd HH:mm:ss");
            return Convert.ToBase64String(RijndaelHelper.EncryptStringToBytes(str, RijndaelHelper.KeyArray, RijndaelHelper.IVArray))
                + "-" + Codehelper.MD5(str + SwitchSiteSignKey);
        }

        public static LoginProfile Parse(string toDecrypt)
        {
            string[] segs = toDecrypt.Split('-');
            if (segs.Length < 2)
                return null;
            byte[] toEncryptArray = Convert.FromBase64String(segs[0]);
            string decryptStr = RijndaelHelper.DecryptStringFromBytes(toEncryptArray, RijndaelHelper.KeyArray, RijndaelHelper.IVArray);
            if (Codehelper.MD5(decryptStr + SwitchSiteSignKey) != segs[1])
                return null;
            string[] items = decryptStr.Split(',');
            LoginProfile login = new LoginProfile(items[0], items[1], items[2]) { Expires = DateTime.Parse(items[3]) };
            if (login.IsExpired())
            {
                return null;
            }
            return login;
        }

        private bool IsExpired()
        {
            return (DateTime.UtcNow - Expires).TotalMinutes > 40; //KOR TIME ZONE
        }



        public string GetCountrySiteUrl()
        {
            if (HttpContext.Current.IsDebuggingEnabled)
            {
                return "http://localhost:9071/sc/account/LogOn?token=" + HttpUtility.UrlEncode(this.ToString());
            }

            if (Codehelper.IsUat)
            {
                if (Country.EqualsIgnoreCaseAndBlank("chn"))
                    return "http://chnapp.blsretail.com:8809/intranetuat/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("twn"))
                    return "http://twnapp.blsretail.com:8809/intranetuat/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("sgp"))
                    return "http://sgpapp.blsretail.com:8809/intranetuat/sgp/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("mys"))
                    return "http://sgpapp.blsretail.com:8809/intranetuat/mys/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("kor"))
                    return "http://korapps.blsretail.com:8809/intranetuat?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("hkg"))
                    return "http://twnapp.blsretail.com:8809/Intranet-hkg-uat?token=" +
                           HttpUtility.UrlEncode(this.ToString());
            }
            else
            {
                if (Country.EqualsIgnoreCaseAndBlank("chn"))
                    return "http://chnapp.blsretail.com:8809/intranet/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("twn"))
                    return "http://twnapp.blsretail.com:8809/intranet/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("sgp"))
                    return "http://sgpapp.blsretail.com:8809/intranet/sgp/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("mys"))
                    return "http://sgpapp.blsretail.com:8809/intranet/mys/?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("kor"))
                    return "http://korapps.blsretail.com:8809/intranet?token=" +
                           HttpUtility.UrlEncode(this.ToString());
                if (Country.EqualsIgnoreCaseAndBlank("hkg"))
                    return "http://twnapp.blsretail.com:8809/Intranet-hkg?token=" +
                           HttpUtility.UrlEncode(this.ToString());
            }
            return null;
        }
    }
}