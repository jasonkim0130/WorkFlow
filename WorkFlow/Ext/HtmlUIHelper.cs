using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Web.Mvc.Html;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using Dreamlab.Core;
using WorkFlowLib;
using WorkFlowLib.Data;

namespace WorkFlow.Ext
{
    /**
    * Created by jeremy on 3/14/2017 3:25:53 PM.
    */
    public class HtmlUIHelper
    {
        public static AjaxOptions DefaultAjaxGetOption = new AjaxOptions
        {
            HttpMethod = "get",
            InsertionMode = InsertionMode.Replace,
            LoadingElementId = "div_progress",
            UpdateTargetId = "div_content",
            OnFailure = "ajaxError"
        };

        public static AjaxOptions DefaultTabAjaxGetOption = new AjaxOptions
        {
            HttpMethod = "get",
            InsertionMode = InsertionMode.Replace,
            LoadingElementId = "div_progress",
            UpdateTargetId = "div_tab_content",
            OnFailure = "ajaxError"
        };

        public static AjaxOptions DefaultAjaxPostOption = new AjaxOptions
        {
            HttpMethod = "post",
            InsertionMode = InsertionMode.Replace,
            LoadingElementId = "div_progress",
            UpdateTargetId = "div_content",
            OnFailure = "ajaxError"
        };

        public static string GetRowCss(int? savedResult)
        {
            if (savedResult == 2)
                return "success";
            if (savedResult < 0)
                return "danger";
            return String.Empty;
        }

        public static string GetResultMessage(int? savedResult)
        {
            if (savedResult == -1)
                return "Invalid event name";
            if (savedResult == -2)
                return "Shop not in event";
            if (savedResult == -3)
                return "Sku not in event";
            if (savedResult == -4)
                return "Shop duplicate";
            if (savedResult == -5)
                return "Sku duplicate";
            if (savedResult == -6)
                return "Sku duplicate";
            if (savedResult == -7)
                return "DepotName duplicate";
            if (savedResult == -8)
                return "DepotCode duplicate";
            if (savedResult == -9)
                return "Invalid shop";
            return String.Empty;
        }

        public static string GetFontColorByStatus(string hdrStatus)
        {
            if (hdrStatus.EqualsIgnoreCaseAndBlank("COMPLETED"))
                return "#548235";
            if (hdrStatus.EqualsIgnoreCaseAndBlank("PROCESSING"))
                return "#ed7d31";
            if (hdrStatus.EqualsIgnoreCaseAndBlank("NOT STARTED"))
                return "red";
            return "black";
        }

        public static string GetTrackingURL(string courierCompany, string trackingNumber)
        {
            if (Codehelper.DefaultCountry.EqualsIgnoreCaseAndBlank("KOR"))
            {
                return "http://www.hanjin.co.kr/Delivery_html/inquiry/result_waybill.jsp?wbl_num=" + trackingNumber;
            }
            if (!String.IsNullOrWhiteSpace(courierCompany))
            {
                //if (courierCompany.IndexOf("DEPPON", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //    return "https://www.deppon.com/zhuizong/trackresult.action";
                //if (courierCompany.IndexOf("ZTO Express", StringComparison.InvariantCultureIgnoreCase) >= 0 || courierCompany.EqualsIgnoreCaseAndBlank("ZTO"))
                //    return "http://www.zto.com/GuestService/BillNew?txtbill=" + trackingNumber;
                //if (courierCompany.IndexOf("SF Express", StringComparison.InvariantCultureIgnoreCase) >= 0 || courierCompany.EqualsIgnoreCaseAndBlank("SF"))
                //    return "http://www.sf-express.com/cn/sc/dynamic_functions/waybill/#search/bill-number/" + trackingNumber;
                if (courierCompany.IndexOf("圆通", StringComparison.InvariantCultureIgnoreCase) >= 0 || courierCompany.EqualsIgnoreCaseAndBlank("YTO"))
                    return "http://www.yto.net.cn/gw/service/Shipmenttracking.html?no=" + trackingNumber;
                if (courierCompany.IndexOf("德邦", StringComparison.InvariantCultureIgnoreCase) >= 0 || courierCompany.EqualsIgnoreCaseAndBlank("DBKD"))
                    return "http://m.deppon.com/mow/client/index.html#/goodsTrack/" + trackingNumber;
                if (courierCompany.IndexOf("中通", StringComparison.InvariantCultureIgnoreCase) >= 0 || courierCompany.EqualsIgnoreCaseAndBlank("ZTO"))
                    return "http://www.zto.com/GuestService/BillNew?txtbill=" + trackingNumber;
                if (courierCompany.IndexOf("顺丰", StringComparison.InvariantCultureIgnoreCase) >= 0 || courierCompany.EqualsIgnoreCaseAndBlank("SF"))
                    return "http://www.sf-express.com/cn/sc/dynamic_functions/waybill/#search/bill-number/" + trackingNumber;
                if (courierCompany.IndexOf("POST", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return "http://www.singpost.com/track-items?ti_qt=" + trackingNumber;
                if (courierCompany.IndexOf("GDEX", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return "http://www.gdexpress.com/malaysia/e-tracking?no=" + trackingNumber;
                if (true)
                    return "http://www.t-cat.com.tw/Inquire/Trace.aspx?no=" + trackingNumber;
            }
            return "javascript:void(0)";
        }

        public static string GetCountedQtyBg(int countedQty, int req)
        {
            if (countedQty == req)
                return "background-color:green;color:white;";
            if (countedQty == 0)
                return "background-color:red;color:white;";
            return "background-color:yellow;color:black;";
        }

        public static string GetCountedQtyBg(int countedQty)
        {
            if (countedQty > 0)
                return "color: white; background-color: green;";
            return "color: white; background-color: red;";
        }

        public static string GetSyncBg(int statusId)
        {
            if (statusId == 1)
                return "color: white; background-color: red;";
            return "color: white; background-color: green;";
        }

        public static string GetCourierName(string courierCompany)
        {
            if (courierCompany != null)
            {
                if (courierCompany.IndexOf("顺丰") >= 0)
                {
                    return "顺丰速运";
                }
                if (courierCompany.IndexOf("德邦") >= 0)
                {
                    return "德邦快递";
                }
                if (courierCompany.IndexOf("中通") >= 0)
                {
                    return "中通快递";
                }
            }
            return null;
        }

        public static string GetInvoice(int? invoicetype)
        {
            if (invoicetype == 1)
                return "二聯式";
            if (invoicetype == 2)
                return "三聯式";
            if (invoicetype == 3)
                return "捐贈";
            return String.Empty;
        }

        public static string GetSortDirection(string sort, string @orderby, string orderdir)
        {
            if (@orderby.EqualsIgnoreCaseAndBlank(sort))
            {
                return orderdir.EqualsIgnoreCaseAndBlank("asc") ? "<label style='font-weight:bold;font-size:large;'>↑</label>" : "<label style='font-weight:bold;font-size:large;'>↓</label>";
            }
            return String.Empty;
        }

        public static string GetCompleted(int numerator, int denominator)
        {
            int v = numerator * 100 / denominator;
            if (v == 100)
                return $"<span class=\"completed-100\">{v}%</span>";
            if (v >= 80)
                return $"<span class=\"completed-80\">{v}%</span>";
            if (v >= 50)
                return $"<span class=\"completed-50\">{v}%</span>";
            return $"<span class=\"completed-0\">{v}%</span>";
        }

        public static string GetStatusCompleted(int numerator, int denominator)
        {
            int v = numerator * 100 / denominator;
            if (v == 100)
                return $"<span class=\"job-completed completed-100\">Completed<br/>{v}%</span>";
            if (v >= 80)
                return $"<span class=\"job-completed completed-80\">Progressing<br/>{v}%</span>";
            if (v > 0)
                return $"<span class=\"job-completed completed-50\">Progressing<br/>{v}%</span>";
            return $"<span class=\"job-completed completed-0\">Not Started<br/>{v}%</span>";
        }

        public static string GetStatus(int? status)
        {
            if (status == 1)
                return "approved";
            if (status == 2)
                return "rejected";
            if (status == 3)
                return "abort";
            return null;
        }

        public static HtmlString RenderControl(WF_FlowPropertys prop, HtmlHelper helper, int index, string value = "")
        {
            IDictionary<string, object> attrs = GetHtmlAttributes(prop);
            if (!String.IsNullOrWhiteSpace(prop.DataSource))
            {
                if (prop.PropertyType == (int)PropertyTypes.RadioGroup)
                {
                    return helper.RadioButtonList("Properties[" + index + "].Value",
                        XElement.Parse(prop.DataSource)
                        .Elements("item")
                        .Select(
                            p =>
                                new RadioButtonListItem
                                {
                                    Text = p.Value,
                                    Checked = p.Value.EqualsIgnoreCaseAndBlank(value)
                                }), RepeatDirection.Horizontal, attrs);
                }
                return helper.DropDownList("Properties[" + index + "].Value",
                    XElement.Parse(prop.DataSource)
                        .Elements("item")
                        .Select(
                            p =>
                                new SelectListItem
                                {
                                    Text = p.Value,
                                    Value = p.Value,
                                    Selected = p.Value.EqualsIgnoreCaseAndBlank(value)
                                }), "", attrs);
            }
            if (prop.PropertyType == (int)PropertyTypes.Userno)
            {
                return helper.DropDownList("Properties[" + index + "].Value",
                    WebCacheHelper.GetUsernames()
                        .Select(
                            p =>
                                new SelectListItem
                                {
                                    Text = p.Value,
                                    Value = p.Key,
                                    Selected = p.Key.EqualsIgnoreCaseAndBlank(value)
                                }), "",
                    attrs);
            }
            if (prop.PropertyType == (int)PropertyTypes.Text)
            {
                return helper.TextArea("Properties[" + index + "].Value", value, attrs);
            }
            if (prop.PropertyType == (int)PropertyTypes.Country)
            {
                return helper.DropDownList("Properties[" + index + "].Value",
                    Consts.Countries.Select(
                        p => new SelectListItem { Text = p, Value = p, Selected = p.EqualsIgnoreCaseAndBlank(value) }), "",
                    attrs);
            }
            if (prop.PropertyType == (int)PropertyTypes.Role)
            {
                return helper.DropDownList("Properties[" + index + "].Value",
                      WebCacheHelper.GetRoles().Select(p => new SelectListItem { Text = p.Value, Value = p.Key, Selected = p.Key.EqualsIgnoreCaseAndBlank(value) }), "",
                      attrs);
            }
            if (prop.PropertyType == (int)PropertyTypes.Department)
            {
                return helper.DropDownList("Properties[" + index + "].Value",
                    WebCacheHelper.GetDepartments().Select(p => new SelectListItem { Text = p.Value, Value = p.Key, Selected = p.Key.EqualsIgnoreCaseAndBlank(value) }), "",
                    attrs);
            }
            if (prop.PropertyType == (int)PropertyTypes.DeptType)
            {
                return helper.DropDownList("Properties[" + index + "].Value",
                    WebCacheHelper.GetDeptTypes().Select(p => new SelectListItem { Text = p.Value, Value = p.Key, Selected = p.Key.EqualsIgnoreCaseAndBlank(value) }), "",
                    attrs);
            }
            if (prop.PropertyType == (int)PropertyTypes.Brand)
            {
                var country = Codehelper.DefaultCountry ?? "";
                return helper.DropDownList("Properties[" + index + "].Value",
                    Consts.BrandsOfContries[country]
                          .Select
                          (
                              b => new SelectListItem
                              {
                                  Text = Consts.GetBrandFullName(b),
                                  Value = b,
                                  Selected = b.EqualsIgnoreCaseAndBlank(value)
                              }
                          ), "", attrs);
            }
            {
                return helper.TextBox("Properties[" + index + "].Value", value, attrs);
            }
        }

        public static string GetContentType(string filename)
        {
            string ext = Path.GetExtension(filename);
            var imgs = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            if (imgs.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "image/" + ext.Trim('.');
            }
            var txts = new[] { ".txt", ".xml" };
            if (txts.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "text/" + ext.Trim('.'); ;
            }
            if (ext.EqualsIgnoreCaseAndBlank(".pdf"))
            {
                return "application/pdf";
            }
            var ppt = new[] {".ppt", ".pptx"};
            if (ppt.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "application/ms-powerpoint";
            }
            var excel = new[] {".xls", ".xlsx"};
            if (excel.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "application/ms-excel";
            }
            return "application/octet-stream";
        }

        public static IDictionary<string, object> GetHtmlAttributes(WF_FlowPropertys prop, bool inputSmall = false, bool disabled = false, string style = null)
        {
            var shouldAddValdate = false;
            IDictionary<string, object> attrs = new Dictionary<string, object>();
            attrs.Add("class", "form-control");
            if (inputSmall)
            {
                attrs["class"] = attrs["class"] + " input-sm";
            }
            if (prop != null)
            {
                if (prop.Compulsory)
                {
                    shouldAddValdate = true;
                    attrs.Add("data-val-required", "*");
                }
                if (prop.PropertyType == 2 || prop.PropertyType == 4)
                {
                    shouldAddValdate = true;
                    attrs.Add("data-val-number", "*");
                }
                if (prop.PropertyType == 3)
                {
                    attrs["class"] = attrs["class"] + " datetime";
                }
                if (prop.PropertyType == 5)
                {
                    attrs["class"] = attrs["class"] + " date";
                }
                if (prop.PropertyType == 8)
                {
                    attrs["class"] = attrs["class"] + " time";
                }
                if (shouldAddValdate)
                {
                    attrs.Add("data-val", "true");
                }
            }
            if (disabled)
            {
                attrs.Add("disabled", "disabled");
            }
            if (style != null)
            {
                attrs.Add("style", style);
            }
            return attrs;
        }

        public static MvcHtmlString GenerateRadioButtonList(HtmlHelper helper, string name, string texts, IDictionary<string, object> htmlAttributes = null, string value = "")
        {
            var items = texts
                        .Split(',')
                        .Select(
                            item => new RadioButtonListItem
                            {
                                Text = item,
                                Checked = item.EqualsIgnoreCaseAndBlank(value)
                            });
            return helper.RadioButtonList(name, items, RepeatDirection.Horizontal, htmlAttributes);
        }

        public static MvcHtmlString GenerateDropDownList(HtmlHelper helper, string name, string texts, IDictionary<string, object> htmlAttributes = null, string value = "", string title = null)
        {
            var items = texts
                .Split(',')
                .Select(
                    item => new SelectListItem
                    {
                        Text = item,
                        Value = item,
                        Selected = item.EqualsIgnoreCaseAndBlank(value)
                    });
            return helper.DropDownList(name, items, title, htmlAttributes);
        }

        public static MvcHtmlString GenerateDropDownList(HtmlHelper helper, string name, Dictionary<string,string> items, IDictionary<string, object> htmlAttributes = null, string value = "", string title = null)
        {
            return helper.DropDownList(name,
                items.Select(p =>
                    new SelectListItem
                    {
                        Text = p.Value,
                        Value = p.Key,
                        Selected = p.Key.EqualsIgnoreCaseAndBlank(value)
                    }), title, htmlAttributes);
        }
    }
}