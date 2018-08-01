using System;
using System.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dreamlab.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorkFlowLib
{
    public class Codehelper
    {
        public static CultureInfo EnUS = new CultureInfo("en-US");
        public static readonly string DefaultCountry = ConfigurationManager.AppSettings["Country"];
        public static readonly bool IsUat = ConfigurationManager.AppSettings["IsUAT"].EqualsIgnoreCaseAndBlank("true");
        public static readonly string ConnectionStr = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;

        public static string GetLang(string lang)
        {
            string result;
            switch (lang)
            {
                case "CHN":
                    result = "SC";
                    break;
                case "HKG":
                case "TWN":
                    result = "TC";
                    break;
                case "KOR":
                    result = "KO";
                    break;
                case "MYS":
                case "SGP":
                case "ENG":
                    result = "EN";
                    break;
                default:
                    result = "TC";
                    break;
            }
            return result;
        }

        public static string GetCountryByLang(string lang)
        {
            string country = "ENG";
            if (lang == "SC")
            {
                country = "CHN";
            }
            else if (lang == "TC")
            {
                country = "TWN";
            }
            else if (lang == "KO")
            {
                country = "KOR";
            }
            return country;
        }

        public static string GetTransporter(string json)
        {
            try
            {
                JObject obj = JObject.Parse(JsonConvert.DeserializeObject<string>(json));
                return ((JObject)obj["data"]).Value<string>("cpCode");
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static object DeserializeString(string itemTxtInfo)
        {
            try
            {
                return JsonConvert.DeserializeObject<string>(itemTxtInfo);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string Get3011ApiUrl(string country)
        {
            if (IsUat)
            {
                string url = "http://10.188.48.141:3011/";
                if (country.EqualsIgnoreCaseAndBlank("TWN") || country.EqualsIgnoreCaseAndBlank("HKG"))
                    url = "https://10.188.18.141:3011/";
                else if (country.EqualsIgnoreCaseAndBlank("SGP") || country.EqualsIgnoreCaseAndBlank("MYS"))
                    url = "http://10.188.58.141:3011/";
                else if (country.EqualsIgnoreCaseAndBlank("KOR"))
                    url = "http://10.188.28.141:3011/";
                return url;
            }
            else
            {
                string url = "http://10.188.48.208:3011/";
                if (country.EqualsIgnoreCaseAndBlank("TWN") || country.EqualsIgnoreCaseAndBlank("HKG"))
                    url = "http://10.188.23.88:3011/";
                else if (country.EqualsIgnoreCaseAndBlank("SGP") || country.EqualsIgnoreCaseAndBlank("MYS"))
                    url = "http://10.188.58.111:3011/";
                else if (country.EqualsIgnoreCaseAndBlank("KOR"))
                    url = "http://10.188.28.223:3011/";
                return url;
            }
        }

        public static string MD5(string str)
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] output = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
                string[] hexoutPut = new string[output.Length];
                for (int i = 0; i < hexoutPut.Length; i++)
                {
                    hexoutPut[i] = output[i].ToString("x2");
                }
                return string.Join(string.Empty, hexoutPut);
            }
        }
    }
}