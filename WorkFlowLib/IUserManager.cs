using System;
using WorkFlowLib.DTO;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 4/25/2017 8:56:54 AM.
    */
    public interface IUserManager : IDisposable
    {
        UserStaffInfo SearchStaff(string userid);
        Employee[] GetUserByGrade(string country, string deptcode, string depttype, string @operator, int? grade);
        Employee[] GetManager(string userId, int? level, int? maxlevel, string managerLevelOperator);
        Employee[] GetUserByRole(string country, string deptcode, string role, string depttype, string brand);
        bool UpdateLeaveBalance(string country, string staffId, float days);
        UserHolidayInfo[] GetHolidays(string country, string from, string to);
    }
}