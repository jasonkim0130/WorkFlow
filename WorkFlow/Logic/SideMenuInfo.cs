using System.Collections.ObjectModel;

namespace WorkFlow.Logic
{
    public class SideMenuInfo : SideMenu
    {
        public SideItemType Type { get; set; }
        public string Name { get; set; }
        public int MenuId { get; set; }
        public int? Badge { get; set; }
        public object Parameters { get; set; }
        public string BadgeId { get; set; }
        public new Collection<SideMenuInfo> ChildNodes { get; set; }
        public static SideMenuInfo CreateFrom(SideMenu sm)
        {
            return new SideMenuInfo
            {
                Inframe = sm.Inframe,
                DropSwitch = sm.DropSwitch,
                Area = sm.Area,
                Code = sm.Code,
                Icon = sm.Icon,
                Action = sm.Action,
                Controller = sm.Controller,
                FullScreen = sm.FullScreen,
                ChildNodes = new Collection<SideMenuInfo>()
            };
        }
    }

    public enum SideItemType
    {
        Page,
        Warehouse
    }

    public class AdminSideMenuInfo : AdminSideMenu
    {
        public int MenuId { get; set; }
        public int? Badge { get; set; }
        public object Parameters { get; set; }
        public string BadgeId { get; set; }
        public string ID { get; set; }
        public new Collection<AdminSideMenuInfo> ChildNodes { get; set; }
        public static AdminSideMenuInfo CreateFrom(AdminSideMenu sm)
        {
            return new AdminSideMenuInfo
            {
                Type = sm.Type,
                Inframe = sm.Inframe,
                DropSwitch = sm.DropSwitch,
                Area = sm.Area,
                Code = sm.Code,
                Name = sm.Name,
                Icon = sm.Icon,
                Action = sm.Action,
                Controller = sm.Controller,
                AppendWarehouse = sm.AppendWarehouse,
                FullScreen = sm.FullScreen
            };
        }
    }
}