using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;
using Microsoft.Ajax.Utilities;
using Dreamlab.Core;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib;
using WorkFlowLib.Parameters;

namespace WorkFlow.Ext
{
    public class WebCacheHelper
    {
        public static DateTime LastQtyUpdated { get; private set; }
        public static void RemoveQtySummarCache()
        {
            LastQtyUpdated = DateTime.Now.AddDays(-1);
        }

        //#TODO 
        public static string GetWF_UsernameByNo(string userno)
        {
            if (string.IsNullOrWhiteSpace(userno))
                return userno;
            Employee[] data = GetData(() =>
            {
                using (WorkFlowEntities entities = new WorkFlowEntities())
                {
                    return entities.GlobalUserView.Where(p => p.EmpStatus == "A").Select(p => new Employee { UserNo = p.EmployeeID, Name = p.EmployeeName }).ToArray();
                }
            }, "GlobalUserViews");
            var user = data.FirstOrDefault(p => p.UserNo.EqualsIgnoreCaseAndBlank(userno));
            string username = (user != null) ? user.Name : userno;
            return username;
        }

        public static string GetUsernameByNo(string usernno)
        {
            if (string.IsNullOrWhiteSpace(usernno))
                return usernno;
            var data = GetData(() =>
            {
                using (WorkFlowEntities entities = new WorkFlowEntities())
                {
                    return entities.Users.Where(p => p.StatusId > 0).Select(p => new { p.UserNo, p.Username }).ToDictionary(p => p.UserNo, p => p.Username);
                }
            }, "LocalUsernames");
            string username = data.ContainsKey(usernno) ? data[usernno] : usernno;
            return username;
        }

        public static Dictionary<string, string> GetUsernames()
        {
            Employee[] data = GetData(() =>
            {
                using (WorkFlowEntities entities = new WorkFlowEntities())
                {
                    return entities.GlobalUserView.Where(p => p.EmpStatus == "A").Select(p => new Employee { UserNo = p.EmployeeID, Name = p.EmployeeName }).ToArray();
                }
            }, "GlobalUserViews");
            return data.DistinctBy(p => p.UserNo).ToDictionary(p => p.UserNo, p => p.Name);
        }

        public static Dictionary<string, string> GetRegion()
        {
            var data = GetData(() =>
            {
                using (ApiClient client = new ApiClient())
                {
                    var result = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Codehelper.DefaultCountry,
                        id = "ZAGRAR0",
                        language = Codehelper.DefaultCountry,
                        code = "%"
                    });
                    if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                    {
                        return result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);
                    }
                    return null;
                }
            }, "Regions") ?? new Dictionary<string, string>();
            return data;
        }

        public static Dictionary<string, string> GetDepartment()
        {
            var data = GetData(() =>
            {
                using (ApiClient client = new ApiClient())
                {
                    var result = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Codehelper.DefaultCountry,
                        id = "ZADEPT1",
                        language = Codehelper.DefaultCountry,
                        code = "%"
                    });
                    if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                    {
                        return result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);
                    }
                    return null;
                }
            }, "Departments") ?? new Dictionary<string, string>();
            return data;
        }

        public static T GetData<T>(Func<T> dataResolver, string keyname)
        {
            string key = keyname ?? typeof(T).Name;
            var data = WebCache.Get(key);
            if (data == null)
            {
                data = dataResolver();
                if (data != null)
                    WebCache.Set(key, data, 10, false);
            }
            return (T)data;
        }

        public static T GetDataByKey<T>(Func<T> dataResolver, string getKey)
        {
            string key = typeof(Dictionary<string, T>).Name + typeof(string).Name + typeof(T).Name;
            Dictionary<string, T> data = WebCache.Get(key);
            if (data == null)
            {
                data = new Dictionary<string, T> { { getKey, dataResolver() } };
                if (data[getKey] != null)
                    WebCache.Set(key, data, 2, false);
            }
            else
            {
                if (!data.ContainsKey(getKey))
                {
                    data[getKey] = dataResolver();
                    if (data[getKey] != null)
                        WebCache.Set(key, data, 2, false);
                }
            }
            return data[getKey];
        }

        public static void Remove(Type type)
        {
            WebCache.Remove(type.Name);
        }

        public static Dictionary<string, string> GetRoles()
        {
            var data = GetData(() =>
            {
                using (ApiClient client = new ApiClient())
                {
                    var result = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "ZAROLE0",
                        language = "ENG",
                        code = "%"
                    });
                    if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                    {
                        return result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);
                    }
                    return null;
                }
            }, "Roles") ?? new Dictionary<string, string>();
            return data;
        }

        public static Dictionary<string, string> GetDepartments()
        {
            var data = GetData(() =>
            {
                using (ApiClient client = new ApiClient())
                {
                    var result = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "ZADEPT1",
                        language = "ENG",
                        code = "%"
                    });
                    if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                    {
                        return result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);
                    }
                    return null;
                }
            }, "Deparments") ?? new Dictionary<string, string>();
            return data;
        }

        public static Dictionary<string, string> GetDeptTypes()
        {
            var data = GetData(() =>
            {
                using (ApiClient client = new ApiClient())
                {
                    var result = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "ZADEPT0",
                        language = "ENG",
                        code = "%"
                    });
                    if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                    {
                        return result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);
                    }
                    return new Dictionary<string, string>();
                }
            }, "DetpTypes");
            return data;
        }
    }
}