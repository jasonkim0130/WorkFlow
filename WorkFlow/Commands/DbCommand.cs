using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Dreamlab.Db;

namespace WorkFlow.Commands
{
    public abstract class DbCommand : Command
    {
        public XElement ConvertToXmlParameter(IEnumerable<string> stringArray)
        {
            return new XElement("R", stringArray.Select(p => new XElement("I", p)));
        }
        public IDBRepository Repository { get; set; }
        public virtual string GetSql()
        {
            return string.Empty;
        }

        public static string GetInCondtion<T>(IEnumerable<T> items, string field, bool withAnd = true)
        {
            if (items != null && items.Any())
            {
                if (typeof(T) == typeof(string))
                {
                    return (withAnd ? " AND " : string.Empty) + $" {field} IN  ({ string.Join(",", items.Where(p => p != null).Select(p => $"'{p.ToString().Replace("'", "''")}'")) })";
                }
                return (withAnd ? " AND " : string.Empty) + $" {field} IN  ({ string.Join(",", items.Where(p => p != null).Select(p => p)) })";
            }

            return string.Empty;
        }
    }

    public abstract class DbCommand<TResult> : Command<TResult>
    {
        public XElement ConvertToXmlParameter(IEnumerable<string> stringArray)
        {
            return new XElement("R", stringArray.Select(p => new XElement("I", p)));
        }

        public static string GetInCondtion<T>(IEnumerable<T> items, string field, bool withAnd = true)
        {
            if (items != null && items.Any())
            {
                if (typeof(T) == typeof(string))
                {
                    return (withAnd ? " AND " : string.Empty) + $" {field} IN  ({ string.Join(",", items.Where(p => p != null).Select(p => $"'{p.ToString().Replace("'", "''")}'")) })";
                }
                return (withAnd ? " AND " : string.Empty) + $" {field} IN  ({ string.Join(",", items.Where(p => p != null).Select(p => p)) })";
            }

            return string.Empty;
        }

        public IDBRepository Repository { get; set; }
        public virtual string GetSql()
        {
            return string.Empty;
        }
    }
}