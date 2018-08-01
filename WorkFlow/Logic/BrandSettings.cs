using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Dreamlab.Core;


namespace WorkFlow.Logic
{
    public class BrandSetting
    {
        public static readonly string[] DefaultBrands = { "HTN", "LEO", "ROS", "HCT", "HTJ", "APM" };
        public static string[] GetBrands(string country)
        {
            var brands = Singleton<Dictionary<string, BrandSetting>>.Instance;
            return brands.ContainsKey(country) ? brands[country].Brands: DefaultBrands;
        }

        public static void ReadFromXml()
        {
            XElement ele = XElement.Parse(File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/Brands.xml")));
            Singleton<Dictionary<string, BrandSetting>>.Instance = ele.Elements("CountryBrand").Select(p => new BrandSetting()
            {
                Country = p.Attribute("Country")?.Value,
                Brands = p.Attribute("Brands")?.Value?.Split(',')
            }).ToDictionary(p => p.Country, p => p);
        }
        public string Country { get; set; }
        public string[] Brands { get; set; }
    }

}