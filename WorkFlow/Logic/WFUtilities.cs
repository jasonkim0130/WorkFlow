using System.Web.Mvc;
using System.Collections.Generic;
using System.Linq;
using WorkFlowLib.DTO;
using WorkFlowLib;
using WorkFlowLib.Parameters;

namespace WorkFlow.Logic
{
    public class WFUtilities
    {
        public static UserStaffInfo GetUserStaffInfo(string userno)
        {
            IUserManager um = DependencyResolver.Current.GetService<IUserManager>();
            return um.SearchStaff(userno);
        }

        public static UserHolidayInfo[] GetHolidays(string country, string fromdate, string todate)
        {
            IUserManager um = DependencyResolver.Current.GetService<IUserManager>();
            return um.GetHolidays(country, fromdate, todate);
        }

        public static Dictionary<string, string> GetLeaveType(string lang)
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFLVTY0",
                    language = "ENG",
                    code = "%"
                });
                var result1 = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFLVTY0",
                    language = Codehelper.GetCountryByLang(lang),
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null
                                                                   && string.IsNullOrWhiteSpace(result1.ErrorMessage) &&
                                                                   result1.ReturnValue != null)
                {
                    Dictionary<string, string> finalResult = new Dictionary<string, string>();
                    foreach (var item in result.ReturnValue)
                    {
                        var item1 = result1.ReturnValue.FirstOrDefault(p => p.ZZ03_CODE == item.ZZ03_CODE);
                        if (item1 != null)
                            finalResult.Add(item.ZZ03_CDC1, item1.ZZ03_CDC1);
                    }

                    return finalResult;
                }

                return null;
            }
        }

        public static Dictionary<string, string> GetTripDestination()
        {
            return MasterCodeSearchById("WFTRCT0");
        }
        public static Dictionary<string, string> GetCurrencyType()
        {
            return MasterCodeSearchById("WFCURR0");
        }
        public static Dictionary<string, string> GetExpenseType()
        {
            return MasterCodeSearchById("WFEXPG0");
        }
        public static Dictionary<string, string> GetStaffLanguage()
        {
            return MasterCodeSearchById("WFLANG0");
        }
        public static Dictionary<string, string> GetMarriageStatus()
        {
            return MasterCodeSearchById("WFMRST0");
        }
        public static Dictionary<string, string> GetCountryRegion()
        {
            return MasterCodeSearchById("WFCOUN0");
        }

        public static Dictionary<string, string> MasterCodeSearchById(string id)
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = id,
                    language = Consts.GetApiCountry(),
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    return result.ReturnValue.ToDictionary(code => code.ZZ03_CODE, desc => desc.ZZ03_CDC1);
                }

                return null;

            }
        }

    }
}