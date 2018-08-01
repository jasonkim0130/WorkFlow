using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dreamlab.Core;
using WorkFlow.Logic;
using WorkFlowLib.Results;

namespace WorkFlow.Models
{
    public class MenuViewModel
    {
        public WarningConfig WarningConfig { get; set; }
        public QtySummaryResult QtyResult { get; set; }
        public Menu[] Menus { get; set; }
        public Menu[] Reports { get; set; }
        public Menu[] Documents { get; set; }
        public Menu[] Staff { get; set; }
        public List<WarehousesGroupModel> WarehouseGroup { get; set; }

        public Collection<SideMenuInfo> Create(Collection<SideMenu> nodes, string username)
        {
            //if (nodes == null || nodes.Count == 0 || Menus == null || Menus.Length == 0)
            //    return null;

            List<TreeMenu> treeMenus = new List<TreeMenu>();
            foreach (var parent in Menus.Where(p => string.IsNullOrWhiteSpace(p.PARENT_PGM)).OrderBy(p => p.SORT_ORDER))
            {
                TreeMenu menu = new TreeMenu(parent);
                menu.SubMenus = GetSubMenus(menu.PGM_NO, Menus);
                treeMenus.Add(menu);
            }

            Collection<SideMenuInfo> roots = new Collection<SideMenuInfo>();
            foreach (var item in treeMenus)
            {
                var info = CovertToSideMenuInfo(item, nodes, item.PGM_ID, username);
                if (info != null)
                    roots.Add(info);
            }
            if (Reports != null && Reports.Length > 0)
            {
                roots.Add(GetReports(Reports));
            }
            if (Documents != null && Documents.Length > 0)
            {
                roots.Add(GetDocuments(Documents));
            }
            if (Staff != null && Staff.Length > 0)
            {
                roots.Add(GetStaff(Staff));
            }
            return roots;
        }

        private SideMenuInfo GetStaff(Menu[] Staff)
        {
            SideMenuInfo report = new SideMenuInfo
            {
                Name = "Staff",
                Icon = "~/Content/Images/Icons/Documents.png"
            };
            List<TreeMenu> reportMenus = new List<TreeMenu>();
            foreach (Menu parent in Staff.Where(p => string.IsNullOrWhiteSpace(p.PARENT_PGM)).OrderBy(p => p.SORT_ORDER))
            {
                TreeMenu menu = new TreeMenu(parent);
                menu.SubMenus = GetSubMenus(menu.PGM_NO, Staff);
                reportMenus.Add(menu);
            }
            Collection<SideMenuInfo> roots = new Collection<SideMenuInfo>();
            foreach (var item in reportMenus)
            {
                SideMenuInfo info = CovertToDocumentsMenuInfo(item);
                if (info != null)
                    roots.Add(info);
            }
            report.ChildNodes = roots;
            return report;
        }

        private SideMenuInfo GetDocuments(Menu[] Documents)
        {
            SideMenuInfo report = new SideMenuInfo
            {
                Name = "Documents",
                Icon = "~/Content/Images/Icons/Documents.png"
            };
            List<TreeMenu> reportMenus = new List<TreeMenu>();
            foreach (Menu parent in Documents.Where(p => string.IsNullOrWhiteSpace(p.PARENT_PGM)).OrderBy(p => p.SORT_ORDER))
            {   
                TreeMenu menu = new TreeMenu(parent);
                menu.SubMenus = GetSubMenus(menu.PGM_NO, Documents);
                reportMenus.Add(menu);
            }
            Collection<SideMenuInfo> roots = new Collection<SideMenuInfo>();
            foreach (var item in reportMenus)
            {
                SideMenuInfo info = CovertToDocumentsMenuInfo(item);
                if (info != null)
                    roots.Add(info);
            }
            report.ChildNodes = roots;
            return report;
        }

        private SideMenuInfo GetReports(Menu[] reports)
        {
            SideMenuInfo report = new SideMenuInfo
            {
                Name = "Reporting",
                Icon = "~/Content/Images/Icons/Reporting.png"
            };
            List<TreeMenu> reportMenus = new List<TreeMenu>();
            foreach (Menu parent in reports.Where(p => string.IsNullOrWhiteSpace(p.PARENT_PGM)).OrderBy(p => p.SORT_ORDER))
            {
                TreeMenu menu = new TreeMenu(parent);
                menu.SubMenus = GetSubMenus(menu.PGM_NO, reports);
                reportMenus.Add(menu);
            }
            Collection<SideMenuInfo> roots = new Collection<SideMenuInfo>();
            foreach (var item in reportMenus)
            {
                SideMenuInfo info = CovertToReportMenuInfo(item);
                if (info != null)
                    roots.Add(info);
            }
            report.ChildNodes = roots;
            return report;
        }

        private SideMenuInfo CovertToReportMenuInfo(TreeMenu treeMenu)
        {
            SideMenuInfo result = new SideMenuInfo
            {
                Name = treeMenu.PGM_NM,
                Type = SideItemType.Page,
                ChildNodes = new Collection<SideMenuInfo>()
            };
            if (treeMenu.SubMenus != null && treeMenu.SubMenus.Length > 0)
            {
                foreach (var info in treeMenu.SubMenus.Select(CovertToReportMenuInfo))
                {
                    result.ChildNodes.Add(info);
                }
            }
            else
            {
                result.Controller = "WebReport";
                result.Action = "Index";
                result.Parameters = new { menuid = treeMenu.PGM_NO };
            }
            return result;
        }

        private SideMenuInfo CovertToDocumentsMenuInfo(TreeMenu treeMenu)
        {
            SideMenuInfo result = new SideMenuInfo
            {
                Name = treeMenu.PGM_NM,
                Type = SideItemType.Page,
                ChildNodes = new Collection<SideMenuInfo>()
            };
            if (treeMenu.SubMenus != null && treeMenu.SubMenus.Length > 0)
            {
                foreach (var info in treeMenu.SubMenus.Select(CovertToDocumentsMenuInfo))
                {
                    result.ChildNodes.Add(info);
                }
            }
            else
            {
                result.Controller = "WebReport";
                result.Action = "Documents";
                result.Parameters = new { menuid = treeMenu.PGM_NO };
            }
            return result;
        }

        private TreeMenu[] GetSubMenus(string parentNo, Menu[] dataMenus)
        {
            var subMenus =
                dataMenus.Where(p => !string.IsNullOrWhiteSpace(p.PARENT_PGM) && p.PARENT_PGM.Equals(parentNo))
                    .Select(p => new TreeMenu(p))
                    .OrderBy(p => p.SORT_ORDER)
                    .ToArray();
            if (subMenus.Length == 0)
                return null;
            foreach (var sub in subMenus)
            {
                sub.SubMenus = GetSubMenus(sub.PGM_NO, dataMenus);
            }
            return subMenus;
        }

        private SideMenuInfo CovertToSideMenuInfo(TreeMenu treeMenu, Collection<SideMenu> nodes, string rootcode, string username)
        {
            if (treeMenu.PGM_ID.StartsWith("Warehouse"))
                return ConvertToWarehouses(treeMenu);
            if (treeMenu.PGM_ID == "TMALL" && treeMenu.SubMenus != null)
            {
                foreach (var channel in treeMenu.SubMenus)
                {
                    string channelName = channel.PGM_ID;
                    foreach (var action in channel.SubMenus)
                    {
                        action.PGM_ID = channelName + action.PGM_ID;
                    }
                }
            }
            SideMenu node = nodes.Count(p => p.Code == treeMenu.PGM_ID) > 1
                ? nodes.FirstOrDefault(p => p.Code == treeMenu.PGM_ID && p.ParentCode == rootcode)
                : nodes.FirstOrDefault(p => p.Code == treeMenu.PGM_ID);
            if (node == null)
                return null;
            SideMenuInfo result = SideMenuInfo.CreateFrom(node);
            result.Name = treeMenu.PGM_NM;
            result.Type = SideItemType.Page;
            result.BadgeId = node.Code.Replace(" ", "");
            result.Badge = ReadBadge(node.Code, username);
            if (treeMenu.SubMenus != null && treeMenu.SubMenus.Length > 0)
            {
                foreach (var info in treeMenu.SubMenus.Select(temp => CovertToSideMenuInfo(temp, nodes, rootcode, username)).Where(info => info != null))
                {
                    result.ChildNodes.Add(info);
                }
            }
            return result;
        }

        private SideMenuInfo ConvertToWarehouses(TreeMenu treeMenu)
        {
            SideMenuInfo warehouseRoot = new SideMenuInfo
            {
                Name = treeMenu.PGM_NM,
                Type = SideItemType.Page,
                Icon = "~/Content/Images/Icons/warehouse_mg.png",
                ChildNodes = new Collection<SideMenuInfo>()
            };
            if (treeMenu.SubMenus == null) return warehouseRoot;
            foreach (var ws in treeMenu.SubMenus)
            {
                string code = ws.PGM_ID.Substring(3);
                WarehousesGroupModel wgGroup = WarehouseGroup?.FirstOrDefault(p => p.Hdr.WarehouseCode == code);
                if (wgGroup == null)
                    continue;
                string[] codes = wgGroup.Detl != null ? new[] { code }.Concat(wgGroup.Detl.Select(p => p.WarehouseCode)).ToArray() : new[] { code };
                SideMenuInfo warehouse = new SideMenuInfo
                {
                    Name = ws.PGM_NM.Replace($"({code})", ""),
                    Type = SideItemType.Warehouse,
                    BadgeId = "wh-" + code + "-num",
                    Badge = QtyResult?.GetAssignedQty(codes),
                    Parameters = new { warehouse = code },
                    ChildNodes = new Collection<SideMenuInfo>()
                };
                if (ws?.SubMenus != null)
                {
                    foreach (var warehouseAction in ws.SubMenus)
                    {
                        SideMenuInfo action = null;
                        switch (warehouseAction.PGM_ID)
                        {
                            case "View all orders":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "View all orders",
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    BadgeId = "wh-" + code + "-total",
                                    Badge = QtyResult?.GetViewAllQty(codes),
                                    Action = "ViewAllAssignedOrders",
                                    Controller = "WhAll"
                                };
                                break;
                            case "Pick":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "Pick",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Badge = warehouse.Badge,
                                    Parameters = new { warehouse = code },
                                    BadgeId = "wh-" + code + "-picknum",
                                    Action = "Pick",
                                    Controller = "WhPick"
                                };
                                break;
                            case "Pick confirm":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "Pick confirm",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Badge = QtyResult?.GetPickingQty(codes),
                                    Parameters = new { warehouse = code },
                                    BadgeId = "wh-" + code + "-pickednum",
                                    FullScreen = true,
                                    Action = "ScanQRToPicked",
                                    Controller = "Warehouse"
                                };
                                break;
                            case "Pack":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Name = warehouseAction.PGM_NM,
                                    Code = "Pack",
                                    DropSwitch = false,
                                    Badge = QtyResult?.GetPackQty(codes),
                                    Parameters = new { warehouse = code },
                                    BadgeId = "wh-" + code + "-packnum",
                                    FullScreen = true,
                                    Action = "ScanQRToPacking",
                                    Controller = "Warehouse"
                                };
                                break;
                            case "Shipment":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "Shipment",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Badge = QtyResult?.GetPackedQty(codes),
                                    Parameters = new { warehouse = code },
                                    BadgeId = "wh-" + code + "-shipnum",
                                    FullScreen = true,
                                    Action = "Ship",
                                    Controller = "Warehouse"
                                };
                                break;
                            case "Shipped":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "Shipped",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Badge = QtyResult?.GetShippedQty(codes),
                                    Parameters = new { warehouse = code },
                                    BadgeId = "wh-" + code + "-shippednum",
                                    FullScreen = false,
                                    Action = "Shipped",
                                    Controller = "Warehouse"
                                };
                                break;
                            case "Return":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "Return",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    FullScreen = true,
                                    Action = "RefundOrder",
                                    Controller = "Warehouse"
                                };
                                break;
                            case "View All Refund":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "View All Refund",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Badge = QtyResult?.GetRFAssignedQty(codes),
                                    BadgeId = "wh-" + code + "-rfassignednum",
                                    Action = "ViewAllRefund",
                                    Controller = "WhRefund"
                                };
                                break;
                            case "BatchPrint":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "BatchPrint",
                                    DropSwitch = false,
                                    FullScreen = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "BatchPrint",
                                    Controller = "Warehouse"
                                };
                                break;
                            //case "CartGet":
                            //    action = new SideMenuInfo
                            //    {
                            //        Type = SideItemType.Warehouse,
                            //        Code = "CartGet",
                            //        DropSwitch = false,
                            //        FullScreen = true,
                            //        Name = StringResource.CartGet,
                            //        Parameters = new { warehouse = warehouseCode },
                            //        Action = "AssignCart",
                            //        Controller = "PickCart"
                            //    };
                            //    break;
                            case "CartLoad":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "CartLoad",
                                    DropSwitch = false,
                                    FullScreen = true,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "InstallBar",
                                    Controller = "PickCart"
                                };
                                break;
                            case "CartUnload":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "CartUnload",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "UninstallCarts",
                                    Controller = "PickCart"
                                };
                                break;
                            //case "CartCheckOut":
                            //    action = new SideMenuInfo
                            //    {
                            //        Type = SideItemType.Warehouse,
                            //        Code = "CartCheckOut",
                            //        DropSwitch = false,
                            //        Name = StringResource.CartCheckOut,
                            //        Parameters = new { warehouse = warehouseCode },
                            //        Action = "ConfirmBag",
                            //        Controller = "PickCart"
                            //    };
                            //    break;
                            case "RackBox":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "RackBox",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "Index",
                                    Controller = "PickZoneBox"
                                };
                                break;
                            case "OutStockReport":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "OutStockReport",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "SkuLaneInfo",
                                    Controller = "AppStockoutReport"
                                };
                                break;
                            case "RackSetUp":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "RackSetUp",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "Setting",
                                    Controller = "RackBoxPrint"
                                };
                                break;
                            case "RackLabelPrint":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "RackLabelPrint",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "Index",
                                    Controller = "RackBoxPrint"
                                };
                                break;
                            case "SKUWithOutRack":
                                action = new SideMenuInfo
                                {
                                    Type = SideItemType.Warehouse,
                                    Code = "SKUWithOutRack",
                                    DropSwitch = false,
                                    Name = warehouseAction.PGM_NM,
                                    Parameters = new { warehouse = code },
                                    Action = "SKUWithOutRack",
                                    Controller = "SkuWithoutRack"
                                };
                                break;
                        }
                        if (action == null)
                            continue;
                        warehouse.ChildNodes.Add(action);
                    }
                }
                warehouseRoot.ChildNodes.Add(warehouse);
            }
            return warehouseRoot;
        }

        private int? ReadBadge(string code, string username)
        {
            if (code.EqualsIgnoreCaseAndBlank("Pending order"))
            {
                return QtyResult.OrderedNum;
            }
            if (code.EqualsIgnoreCaseAndBlank("Order Edit"))
            {
                return QtyResult.EditableCount;
            }
            if (code.EqualsIgnoreCaseAndBlank("Logistics config"))
            {
                return QtyResult.ApprovedNum;
            }
            //var resolvers = DependencyResolver.Current.GetService<IEnumerable<IBadgeQtyReader>>();
            //foreach (IBadgeQtyReader resolver in resolvers)
            //{
            //    var value = resolver.GetQty(code, username);
            //    if (value != null)
            //        return value;
            //}
            WfBadgeQtyReader reader = new WfBadgeQtyReader();
            var value = reader.GetQty(code, username);
            return value;
        }
    }
}