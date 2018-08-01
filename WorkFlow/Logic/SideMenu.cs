using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using WorkFlow.Ext;
using WorkFlowLib;

namespace WorkFlow.Logic
{
    public class SideMenu
    {
        public static Collection<SideMenu> AllNodes { get; private set; }
        static SideMenu()
        {
            XElement ele =
                XElement.Parse(File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/SideMenuInfo.xml")));
            AllNodes = ReadAllNodes(ele);
        }
        private static Collection<SideMenu> ReadAllNodes(XElement root)
        {
            Collection<SideMenu> childNodes = new Collection<SideMenu>();
            foreach (var p in root.Elements("Menu"))
            {
                SideMenu menu = new SideMenu
                {
                    Code = p.Attribute("Code")?.Value,
                    ParentCode = p.Attribute("ParentCode")?.Value,
                    Icon = p.Attribute("Icon")?.Value,
                    Action = p.Attribute("Action")?.Value,
                    Controller = p.Attribute("Controller")?.Value,
                    Area = p.Attribute("Area")?.Value,
                    DropSwitch = bool.Parse(p.Attribute("DropSwitch")?.Value ?? "false"),
                    Inframe = bool.Parse(p.Attribute("Inframe")?.Value ?? "false"),
                    FullScreen = bool.Parse(p.Attribute("FullScreen")?.Value ?? "false")
                };
                childNodes.Add(menu);
            }
            return childNodes;
        }

        public bool Inframe { get; set; }
        public bool DropSwitch { get; set; }
        public string Area { get; set; }
        public string Code { get; set; }
        public string ParentCode { get; set; }
        public string Icon { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public bool FullScreen { get; set; }
        public Collection<SideMenu> ChildNodes { get; set; }
    }

    public class AdminSideMenu
    {
        public static Collection<AdminSideMenu> GetAdminSideMenu()
        {
            XElement ele =
                XElement.Parse(File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/SideMenu.xml")));
            return ReadChildNodes(ele);
        }
        private static Collection<AdminSideMenu> ReadChildNodes(XElement root)
        {
            Collection<AdminSideMenu> childNodes = new Collection<AdminSideMenu>();
            foreach (var p in root.Elements("Menu").Where(p => string.IsNullOrWhiteSpace(p.Attribute("Country")?.Value)
            || p.Attribute("Country")?.Value == Codehelper.DefaultCountry))
            {
                AdminSideMenu menu = new AdminSideMenu
                {
                    Code = p.Attribute("Code")?.Value,
                    Name = p.Attribute("Name")?.Value,
                    Icon = p.Attribute("Icon")?.Value,
                    Action = p.Attribute("Action")?.Value,
                    Controller = p.Attribute("Controller")?.Value,
                    Area = p.Attribute("Area")?.Value,
                    AppendWarehouse = bool.Parse(p.Attribute("AppendWarehouse")?.Value ?? "false"),
                    DropSwitch = bool.Parse(p.Attribute("DropSwitch")?.Value ?? "false"),
                    Inframe = bool.Parse(p.Attribute("Inframe")?.Value ?? "false"),
                    FullScreen = bool.Parse(p.Attribute("FullScreen")?.Value ?? "false"),
                    ChildNodes = ReadChildNodes(p),
                    Type = SideItemType.Page
                };
                childNodes.Add(menu);
            }
            return childNodes;
        }
        public enum SideItemType
        {
            Page,
            Warehouse
        }
        public SideItemType Type { get; set; }
        public bool Inframe { get; set; }
        public bool DropSwitch { get; set; }
        public string Area { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public bool AppendWarehouse { get; set; }
        public bool FullScreen { get; set; }
        public Collection<AdminSideMenu> ChildNodes { get; set; }
    }
}