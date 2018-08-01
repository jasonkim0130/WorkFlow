using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Dreamlab.Db;
using LinqToExcel;
using WorkFlowLib;

namespace WorkFlow.Controllers
{
    [Authorize]
    public class AdapterController : Controller
    {
        public string Country { get; private set; }
        public string Username { get; private set; }
        private IDBRepository _dbRepository;
        public IDBRepository DbRepository => _dbRepository ?? (_dbRepository = new DBRepository(Codehelper.ConnectionStr));

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            Country = Codehelper.DefaultCountry;
            Username = User.Identity.Name;
            if (User.Identity is FormsIdentity)
            {
                string userdata = ((FormsIdentity)User.Identity).Ticket.UserData;
            }
        }

        protected T[] ReadFromExcel<T>(Func<Row, T> func, out string filename, out string error)
        {
            return ReadExcel(sheet => sheet.Select(func).ToArray(), out filename, out error);
        }

        protected T[] ReadFromExcel<T>(Func<Row, T> func, out string error)
        {
            return ReadExcel(sheet => sheet.Select(func).ToArray(), out error);
        }

        protected T ReadExcel<T>(Func<IEnumerable<Row>, T> readExcel, out string error)
        {
            string filename;
            return ReadExcel(readExcel, out filename, out error);
        }

        protected T ReadExcel<T>(Func<IEnumerable<Row>, T> readExcel, out string filename, out string error)
        {
            filename = null;
            error = null;
            HttpPostedFileBase file = Request.Files.Count > 0 ? Request.Files[0] : null;
            if (file != null)
            {
                try
                {
                    string dir = Server.MapPath("~/temp/excel");
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string ticks = DateTime.Now.Ticks.ToString();
                    filename = Path.Combine(dir, ticks + Path.GetExtension(file.FileName));
                    file.SaveAs(filename);
                    if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xls"))
                    {
                        using (ExcelQueryFactory excel = new ExcelQueryFactory(filename))
                        {
                            return readExcel(excel.Worksheet(0).ToList()
                                .Where(p => p.Any(q => !string.IsNullOrWhiteSpace(q.ToString()))));
                        }
                    }
                }
                catch (Exception e)
                {
                    error = " 读取Excel文件出现错误 " + e.Message;
                }
            }

            return default(T);
        }

        protected T ReadExcelFile<T>(Func<IEnumerable<Row>, T> readExcel, string filename, out string error)
        {
            error = null;
            {
                try
                {
                    string dir = Server.MapPath("~/temp/excel");
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    filename = Path.Combine(dir, filename);
                    if (filename.EndsWith(".xlsx") || filename.EndsWith(".xls"))
                    {
                        using (ExcelQueryFactory excel = new ExcelQueryFactory(filename))
                        {
                            return readExcel(excel.Worksheet(0).ToList()
                                .Where(p => p.Any(q => !string.IsNullOrWhiteSpace(q.ToString()))));
                        }
                    }
                }
                catch (Exception e)
                {
                    error = " 读取Excel文件出现错误 " + e.Message;
                }
            }
            return default(T);
        }

        protected void IterateFromExcel(Action<Row> action, out string error)
        {
            ReadFromExcel(row =>
            {
                action(row);
                return 0;
            }, out error);
        }
    }
}