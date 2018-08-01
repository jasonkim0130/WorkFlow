using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace System.Web.Mvc
{
    public class RadioButtonListItem
    {
        public RadioButtonListItem()
        {
        }
        public string Text { get; set; }
        public bool Checked { get; set; }
        public bool Disabled { get; set; }
    }

    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString RadioButtonList(this HtmlHelper helper, string name, IEnumerable<RadioButtonListItem> items, RepeatDirection repeatDirection, object htmlAttributes)
        {
            return RadioButtonList(helper, name, items, repeatDirection, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }
        public static MvcHtmlString RadioButtonList(this HtmlHelper helper, string name, IEnumerable<RadioButtonListItem> items, RepeatDirection repeatDirection, IDictionary<string, object> htmlAttributes = null)
        {
            TagBuilder table = new TagBuilder("table");
            int i = 0;
            if (repeatDirection == RepeatDirection.Horizontal)
            {
                TagBuilder tr = new TagBuilder("tr");
                foreach (var item in items)
                {
                    i++;
                    string id = string.Format("{0}_{1}", name, i);
                    TagBuilder td = new TagBuilder("td");
                    td.MergeAttribute("style", "padding:0 10px 0 10px");
                    td.InnerHtml = GenerateRadioHtml(name, id, item, htmlAttributes);
                    tr.InnerHtml += td.ToString();
                }
                table.InnerHtml = tr.ToString();
            }
            else
            {
                foreach (var item in items)
                {
                    i++;
                    string id = string.Format("{0}_{1}", name, i);
                    TagBuilder tr = new TagBuilder("tr");
                    TagBuilder td = new TagBuilder("td");
                    td.InnerHtml = GenerateRadioHtml(name, id, item, htmlAttributes);
                    tr.InnerHtml = td.ToString();
                    table.InnerHtml += tr.ToString();
                }
            }
            return new MvcHtmlString(table.ToString());
        }

        private static string GenerateRadioHtml(string name, string id, RadioButtonListItem item, IDictionary<string, object> htmlAttributes = null)
        {
            TagBuilder label = new TagBuilder("label");
            label.MergeAttribute("class", "radio-inline");

            TagBuilder radio = new TagBuilder("input");
            radio.GenerateId(id);
            radio.MergeAttribute("name", name);
            radio.MergeAttribute("value", item.Text);
            radio.MergeAttribute("type", "radio");
            radio.MergeAttributes(htmlAttributes);
            if (item.Checked)
            {
                radio.MergeAttribute("checked", "checked");
            }
            if (item.Disabled)
            {
                radio.MergeAttribute("disabled", "disabled");
            }
            label.InnerHtml = radio.ToString() + item.Text;

            return label.ToString();
        }
    }
}