using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WorkFlow.Ext
{
    public static class CheckboxExtensions
    {
        public static MvcHtmlString CheckboxList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList)
        {
            return CheckboxList(htmlHelper, name, selectList, null);
        }

        public static MvcHtmlString CheckboxList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return CheckboxList(htmlHelper, name, selectList, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString CheckboxList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            return CheckboxListHelper(htmlHelper, name, selectList, htmlAttributes);
        }

        public static MvcHtmlString CheckboxListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
        {
            return CheckboxListFor(htmlHelper, expression, selectList, null);
        }

        public static MvcHtmlString CheckboxListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return CheckboxListFor(htmlHelper, expression, selectList, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString CheckboxListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            string name = ExpressionHelper.GetExpressionText(expression);
            string fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            return CheckboxListHelper(htmlHelper, fullName, selectList, htmlAttributes);
        }

        private static MvcHtmlString CheckboxListHelper(HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, IDictionary<string, object> htmlAttributes)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name");
            }
            bool usedViewData = false; //Read selected values from Viewdata
            if (selectList == null)
            {
                selectList = GetSelectData(htmlHelper, name);
                usedViewData = true;
            }
            object defaultValue = !usedViewData ? htmlHelper.ViewData.Eval(name) : null;
            IEnumerable defaultValues = defaultValue as IEnumerable ?? new[] { defaultValue };
            IEnumerable<string> values = from object value in defaultValues
                                         select Convert.ToString(value, CultureInfo.CurrentCulture);
            HashSet<string> selectedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
            List<SelectListItem> newSelectList = new List<SelectListItem>();
            foreach (SelectListItem item in selectList)
            {
                item.Selected = (item.Value != null)
                                    ? selectedValues.Contains(item.Value)
                                    : selectedValues.Contains(item.Text);
                newSelectList.Add(item);
            }
            selectList = newSelectList;
            StringBuilder listItemBuilder = new StringBuilder();
            int index = 0;
            foreach (SelectListItem item in selectList)
            {
                listItemBuilder.Append(CheckboxItem(htmlHelper,name, item, index++, htmlAttributes));
            }
            return new MvcHtmlString(listItemBuilder.ToString());
        }

        private static string CheckboxItem(HtmlHelper htmlHelper, string fullName, SelectListItem newSelect, int index, IEnumerable<KeyValuePair<string, object>> htmlAttributes)
        {
            string name = string.Format("{0}[{1}]", fullName, index);
            StringBuilder sb = new StringBuilder();
            sb.Append("<label>");
            //warning: the checkbox element must be after the label, so it can be checked when click the label text.
            sb.AppendFormat("<input id=\"{0}\" name=\"{1}\" type=\"checkbox\" value=\"{2}\" style=\"height:20px;width:20px\"", GenerateId(name),
                            name, HttpUtility.HtmlEncode(newSelect.Value));
            if (newSelect.Selected)
            {
                sb.Append(" checked=\"checked\"");
            }
            if (htmlAttributes != null)
            {
                foreach (KeyValuePair<string, object> item in htmlAttributes)
                {
                    sb.AppendFormat(" {0}=\"{1}\"", item.Key.Replace('_', '-'), HttpUtility.HtmlEncode(item.Value));
                }
            }
            ModelState modelState;
            if (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out modelState))
            {
                if (modelState.Errors.Count > 0)
                {
                    sb.AppendFormat(" class=\"{0}\"", HtmlHelper.ValidationInputCssClassName);
                }
            }
            sb.AppendFormat("/><span style=\"margin-right:5px\">{0}</span>", HttpUtility.HtmlEncode(newSelect.Text));
            sb.AppendFormat("<input type=\"hidden\" name=\"{0}.index\" value=\"{1}\"/>", fullName, index);
            sb.AppendLine("</label>");
            return sb.ToString();
        }

        public static string GenerateId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
            var charArray = name.ToCharArray();
            for (int i = 1; i < charArray.Length; i++)
            {
                char item = charArray[i];
                if (!char.IsLetterOrDigit(item))
                {
                    charArray[i] = '_';
                }
            }
            if (char.IsDigit(charArray[0]))
            {
                charArray[0] = '_';
            }
            return new string(charArray);
        }

        public static IEnumerable<SelectListItem> GetSelectData(HtmlHelper htmlHelper, string name)
        {
            object o = null;
            if (htmlHelper.ViewData != null)
            {
                o = htmlHelper.ViewData.Eval(name);
            }
            if (o == null)
            {
                throw new InvalidOperationException("IEnumerable<SelectListItem>");
            }
            IEnumerable<SelectListItem> selectList = o as IEnumerable<SelectListItem>;
            if (selectList == null)
            {
                throw new InvalidOperationException("IEnumerable<SelectListItem>");
            }
            return selectList;
        }
    }
}
