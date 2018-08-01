using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dreamlab.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Resources;
using WorkFlow.Logic;
using WorkFlow.Ext;
using LinqToExcel;
using LinqToExcel.Query;
using WorkFlowLib.Data;
using WorkFlowLib;
using WorkFlowLib.DTO;

namespace WorkFlow.Controllers
{
    public abstract class WFController : AdapterController
    {
        private WorkFlowEntities _wfEntities;
        public WorkFlowEntities WFEntities => _wfEntities ?? (_wfEntities = new WorkFlowEntities());
        public ActionResult MarkFlag(int flowCaseId, int flag)
        {
            WF_FlowCases flow = WFEntities.WF_FlowCases.FirstOrDefault(p => p.StatusId > 0 && p.FlowCaseId == flowCaseId);
            if (flow != null)
                flow.IsFlagged = flag == 1 ? 0 : 1;
            WFEntities.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult ViewAttachment(int attachementId, bool? download)
        {
            using (WorkFlowApiClient client = new WorkFlowApiClient())
            {
                WF_FlowCases_Attachments attach = WFEntities.WF_FlowCases_Attachments.FirstOrDefault(p => p.StatusId > 0 && p.AttachementId == attachementId);
                if (attach != null)
                {
                    try
                    {
                        string contenttype = HtmlUIHelper.GetContentType(attach.FileName);
                        byte[] bytes = client.GetAttachmentFile(attach.AttachementId);
                        if (download == true || string.IsNullOrWhiteSpace(contenttype))
                        {
                            return File(bytes, attach.OriFileName);
                        }
                        Response.AppendHeader("Content-Disposition", "filename=" + attach.OriFileName);
                        return File(bytes, contenttype);
                    }
                    catch (Exception e)
                    {
                        return Content(StringResource.ATTACHMENT_NOT_FOUND);
                    }
                }
            }
            return Content(StringResource.ATTACHMENT_NOT_FOUND);
        }

        public ActionResult ViewAttachmentContent(int flowCaseId)
        {
            ViewBag.Title = StringResource.ATTACHMENTS + " :";
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            ViewBag.DisplayButtons = false;
            return View("ViewAttachmentContent", "~/Views/Shared/_LargeModalLayout.cshtml", manager.GetAttachments(flowCaseId));
        }

        public ActionResult ViewUploaded(string filename, string display, bool? download)
        {
            string dir = Server.MapPath("~/temp/app");
            string contenttype = HtmlUIHelper.GetContentType(filename);
            if (download == true || contenttype == null)
                return File(Path.Combine(dir, filename), "application/octet-stream", display);
            Response.AppendHeader("Content-Disposition", "filename=" + (display ?? filename));
            return File(Path.Combine(dir, filename), contenttype);
        }

        [HttpPost]
        public ActionResult RecieveFile()
        {
            HttpPostedFileBase file = Request.Files.Count > 0 ? Request.Files[0] : null;
            if (file != null)
            {
                try
                {
                    using (WorkFlowApiClient client = new WorkFlowApiClient())
                    {
                        byte[] fileBytes = new byte[file.InputStream.Length];
                        int byteCount = file.InputStream.Read(fileBytes, 0, (int)file.InputStream.Length);
                        string fileContent = Convert.ToBase64String(fileBytes);
                        string ret = client.UploadFile(file.FileName, fileContent);
                        JObject obj = JObject.Parse(ret);
                        string newName = obj["fileName"].ToString();
                        string subDir = newName.Substring(0, newName.IndexOf(@"\"));
                        string fName = newName.Substring(newName.IndexOf(@"\") + 1);
                        string dir = Server.MapPath("~/temp/app/" + subDir);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        string filename = Path.Combine(dir, fName);
                        file.SaveAs(filename);
                        ViewBag.newName = newName;
                        ViewBag.filename = file.FileName;
                        ViewBag.fileSize = file.ContentLength;
                        return PartialView("~/Views/Application/_UploadedFile.cshtml");
                    }
                }
                catch (Exception e)
                {
                    Singleton<ILogWritter>.Instance.WriteExceptionLog("Read excel for event", e, null);
                    return Content(StringResource.UPLOAD_ATTACHMENT_FAILED + ", " + e.Message);
                }
            }
            return Content(StringResource.FILE_IS_MISSING);
        }

        [HttpPost]
        public ActionResult ReceivedPLExcel(int templateTypeId)
        {
            HttpPostedFileBase file = Request.Files.Count > 0 ? Request.Files[0] : null;
            if (file != null)
            {
                try
                {
                    string filename = null;
                    using (WorkFlowApiClient client = new WorkFlowApiClient())
                    {
                        byte[] fileBytes = new byte[file.InputStream.Length];
                        int byteCount = file.InputStream.Read(fileBytes, 0, (int)file.InputStream.Length);
                        string fileContent = Convert.ToBase64String(fileBytes);
                        string ret = client.UploadFile(file.FileName, fileContent);
                        JObject obj = JObject.Parse(ret);
                        string newName = obj["fileName"].ToString();
                        string subDir = newName.Substring(0, newName.IndexOf(@"\"));
                        string fName = newName.Substring(newName.IndexOf(@"\") + 1);
                        string dir = Server.MapPath("~/temp/app/" + subDir);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        filename = Path.Combine(dir, fName);
                        file.SaveAs(filename);
                        ViewBag.newName = newName;
                        ViewBag.filename = file.FileName;
                        ViewBag.fileSize = file.ContentLength;
                    }
                    if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xls"))
                    {
                        using (ExcelQueryFactory excel = new ExcelQueryFactory(filename))
                        {
                            ExcelQueryable<Row> sheet = excel.Worksheet(0);
                            var rows = sheet.Select(p => p).ToArray();
                            Dictionary<string, object> fileData = new Dictionary<string, object>();
                            if (templateTypeId == 1)
                            {
                                var sumProfit = 0.0;
                                #region get file data from excel
                                foreach (var row in rows)
                                {
                                    var key = row[0].Value.ToString();
                                    var keyForConstruction = row[3].Value.ToString();
                                    if (key.Equals("Gross Profit") ||
                                        key.Equals("Net Sales") ||
                                        key.Equals("Occupancy charges") ||
                                        key.Equals("Salary") ||
                                        key.Equals("Depreciation") ||
                                        key.Equals("Royalty") ||
                                        key.Equals("Others") ||
                                        key.Equals("Total operating expenses") ||
                                        key.Equals("Operating Profit"))
                                    {
                                        var formattedKey = key.Replace(" ", "");
                                        if (!fileData.ContainsKey(formattedKey))
                                        {
                                            var year1 = row[3].Value.ToString().Replace(",", "");
                                            var year2 = row[6].Value.ToString().Replace(",", "");
                                            var year3 = row[10].Value.ToString().Replace(",", "");
                                            fileData[formattedKey] = new Dictionary<string, string>()
                                        {
                                            { "Year1", year1},
                                            { "Year2", year2},
                                            { "Year3", year3}
                                        };
                                            if (key.Equals("Operating Profit"))
                                            {
                                                sumProfit = double.Parse(year1) + double.Parse(year2) + double.Parse(year3);
                                            }
                                        }
                                        #region Gross Profit
                                        if (key.Equals("Gross Profit"))
                                        {
                                            formattedKey = "GrossMargin";
                                            if (!fileData.ContainsKey(formattedKey))
                                            {
                                                var year1 = row[4].Value.ToString()
                                                                        .Replace(",", "")
                                                                        .Replace("%", "");
                                                var year2 = row[7].Value.ToString()
                                                                        .Replace(",", "")
                                                                        .Replace("%", "");
                                                var year3 = row[11].Value.ToString()
                                                                        .Replace(",", "")
                                                                        .Replace("%", "");
                                                fileData[formattedKey] = new Dictionary<string, string>()
                                            {
                                                { "Year1", year1},
                                                { "Year2", year2},
                                                { "Year3", year3}
                                            };
                                            }
                                        }
                                        #endregion
                                    }
                                    else if (key.Equals("Store Size (sq.m)"))
                                    {
                                        fileData["StoreSize"] = row[1].Value.ToString().Replace(",", "");
                                    }
                                    else if (key.Equals("# of Staffs:"))
                                    {
                                        fileData["ofStaffs"] = row[1].Value.ToString().Replace(",", "");
                                    }
                                    if (keyForConstruction.Equals("Walls, Ceiling & Floor") ||
                                        keyForConstruction.Equals("Furniture") ||
                                        keyForConstruction.Equals("Labor Cost") ||
                                        keyForConstruction.Equals("IT Equipment") ||
                                        keyForConstruction.Equals("Utilities & Others") ||
                                        keyForConstruction.Equals("Total Costs") ||
                                        keyForConstruction.Equals("Moving, Assembly, Removal"))
                                    {
                                        var formattedKey = keyForConstruction
                                                            .Replace(" ", "")
                                                            .Replace("&", "")
                                                            .Replace(",", "");
                                        if (!fileData.ContainsKey(formattedKey))
                                        {
                                            var value = row[4].Value.ToString().Replace(",", "");
                                            fileData[formattedKey] = value;
                                            if (keyForConstruction.Equals("Total Costs") && value.Length > 0)
                                            {
                                                fileData["NetGain"] = sumProfit - double.Parse(value);
                                            }
                                        }
                                    }
                                    if (row[6].Value.ToString().Equals("Commission as % of Net Sales:"))
                                    {
                                        var year1 = row[7].Value.ToString()
                                                                .Replace(",", "")
                                                                .Replace("%", "");
                                        var year2 = row[8].Value.ToString()
                                                                .Replace(",", "")
                                                                .Replace("%", "");
                                        var year3 = row[9].Value.ToString()
                                                                .Replace(",", "")
                                                                .Replace("%", "");
                                        fileData["CommissionPercent"] = new Dictionary<string, string>()
                                    {
                                        { "Year1", year1},
                                        { "Year2", year2},
                                        { "Year3", year3}
                                    };
                                    }
                                }
                                #endregion
                            }
                            else if (templateTypeId == 7)
                            {
                                #region get file data from excel
                                foreach (var row in rows)
                                {
                                    var key = row[0].Value.ToString();
                                    var keyForConstruction = row[3].Value.ToString();
                                    if (key.Equals("Sales") ||
                                        key.Equals("Gross Profit") ||
                                        key.Equals("Occupancy Charges") ||
                                        key.Equals("Salary") ||
                                        key.Equals("Depreciation") ||
                                        key.Equals("Royalty") ||
                                        key.Equals("Others") ||
                                        key.Equals("Total Operating Expenses") ||
                                        key.Equals("Operating Profit"))
                                    {
                                        var formattedKey = key.Replace(" ", "");
                                        if (!fileData.ContainsKey(formattedKey))
                                        {
                                            var lastYear = row[1].Value.ToString().Replace(",", "");
                                            var year1 = row[3].Value.ToString().Replace(",", "");
                                            fileData[formattedKey] = new Dictionary<string, string>()
                                            {
                                                { "col1", lastYear},
                                                { "col2", year1}
                                            };
                                        }
                                    }
                                }
                                #endregion
                            }

                            ViewBag.fileData = JsonConvert.SerializeObject(fileData);
                        }
                    }
                    //ViewBag.newName = newName;
                    //ViewBag.filename = file.FileName;
                    //ViewBag.fileSize = file.ContentLength;
                    return PartialView("~/Views/Application/_UploadedFile.cshtml");
                }
                catch (Exception e)
                {
                    Singleton<ILogWritter>.Instance.WriteExceptionLog("Read excel for event", e, null);
                    return Content(StringResource.UPLOAD_ATTACHMENT_FAILED + ", " + e.Message);
                }
            }
            return Content(StringResource.FILE_IS_MISSING);
        }

        protected override void Dispose(bool disposing)
        {
            _wfEntities?.Dispose();
            base.Dispose(disposing);
        }

        [HttpGet]
        public ActionResult GetUserByName(string term)
        {
            var users = WebCacheHelper.GetUsernames()
                          .Where(p => !p.Key.EqualsIgnoreCaseAndBlank(this.Username) && p.Value.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                          .Select(p => new
                          {
                              label = p.Value,
                              value = p.Key
                          }).ToList();
            return Json(users, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetImageFile(int attachmentId)
        {
            using (WorkFlowApiClient client = new WorkFlowApiClient())
            {
                string contentType;
                byte[] ret = client.GetImageFile(attachmentId, out contentType);
                return File(ret, contentType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="country"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="fromTime">am/pm</param>
        /// <param name="toTime">am/pm</param>
        /// <returns></returns>
        public double GetTotalHoursWithoutHoliday(string country, string fromDate, string toDate, string fromTime,
    string toTime)
        {
            DateTime from, to;
            bool isDate_Form = DateTime.TryParse(fromDate, out from);
            bool isDate_To = DateTime.TryParse(toDate, out to);
            if (isDate_Form && isDate_To)
            {
                var hoursOfHalfDay = 4;
                UserHolidayInfo[] holidaysInfo = null;
                if (!String.IsNullOrEmpty(country))
                {
                    holidaysInfo = WFUtilities.GetHolidays(country, from.ToString("yyyyMMdd"), to.ToString("yyyyMMdd"));
                }
                var interval = to - from;
                var totalHours = (interval.TotalDays + 1) * hoursOfHalfDay * 2;
                string f = from.ToString("yyyyMMdd");
                string t = to.ToString("yyyyMMdd");
                if (fromTime.EqualsIgnoreCaseAndBlank("pm"))
                {
                    totalHours -= hoursOfHalfDay;
                    f += "2";
                }
                else
                {
                    f += "1";
                }
                if (toTime.EqualsIgnoreCaseAndBlank("am"))
                {
                    totalHours -= hoursOfHalfDay;
                    t += "1";
                }
                else
                {
                    t += "2";
                }
                int nf = int.Parse(f);
                int nt = int.Parse(t);
                if (holidaysInfo != null)
                {
                    foreach (var info in holidaysInfo)
                    {
                        if (String.IsNullOrEmpty(info.Time))
                        {
                            continue;
                        }
                        var date = DateTime.ParseExact(info.Date, "yyyyMMdd",
                            System.Globalization.CultureInfo.CurrentCulture);
                        List<int> holidayList = new List<int>();
                        if (info.Time.EqualsIgnoreCaseAndBlank("all"))
                        {
                            string h1 = info.Date + "1";
                            string h2 = info.Date + "2";
                            holidayList.Add(int.Parse(h1));
                            holidayList.Add(int.Parse(h2));
                        }
                        else if (info.Time.EqualsIgnoreCaseAndBlank("am"))
                        {
                            string h = info.Date + "1";
                            holidayList.Add(int.Parse(h));
                        }
                        else if (info.Time.EqualsIgnoreCaseAndBlank("pm"))
                        {
                            string h = info.Date + "2";
                            holidayList.Add(int.Parse(h));
                        }
                        foreach (var i in holidayList)
                        {
                            if (i >= nf && i <= nt)
                            {
                                totalHours -= 4;
                            }
                        }
                    }
                }
                return totalHours;
            }
            else
            {
                return -1;
            }
        }
    }
}