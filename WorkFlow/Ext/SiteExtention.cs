using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Security;
using Resources;

namespace WorkFlow.Ext
{
    public static class SiteExtention
    {
        public static List<SelectListItem> GenerateSelectListFromEnum(this Type current, string selected = "")
        {
            return (from object e in Enum.GetValues(current)
                    let display = ((Enum)e).ToDisplayString()
                    select new SelectListItem
                    {
                        Text = display,
                        Value = e.ToString(),
                        Selected = display == selected
                    }).ToList();
        }

        public static string GetValue(this Dictionary<string, string> a, string key)
        {
            return a.ContainsKey(key) ? a[key] : null;
        }

        public static string ToLocal(this string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;
            string value = StringResource.ResourceManager.GetString(key);
            return (string.IsNullOrEmpty(value)) ? null : value;
        }

        public static string ToLocalKey(this string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;
            string value = StringResource.ResourceManager.GetString(key);
            return (string.IsNullOrEmpty(value)) ? key : value;
        }

        public static string ReplaceLocal(this string key, string include)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;
            string value = StringResource.ResourceManager.GetString(include);
            return (string.IsNullOrEmpty(value)) ? key : key.Replace(include, value);
        }

        public static string ToNullIfEmpty(this string str)
        {
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }

        public static string ToDisplayString(this Enum current)
        {
            Type enumType = current.GetType();
            string name = Enum.GetName(enumType, current);
            if (name != null)
            {
                FieldInfo fieldInfo = enumType.GetField(name);
                if (fieldInfo != null)
                {
                    DisplayAttribute attr = Attribute.GetCustomAttribute(fieldInfo,
                        typeof(DisplayAttribute), false) as DisplayAttribute;
                    if (attr != null)
                    {
                        return attr.Name;
                    }
                }
            }
            return current.ToString();
        }

        public static string GetCountry(this WebViewPage page)
        {
            if (page.User.Identity is FormsIdentity)
                return ((FormsIdentity)page.User.Identity).Ticket.UserData.Split(',')[0];
            return "";
        }
    }
}