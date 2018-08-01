using System.Linq;
using Dreamlab.Core;
using WorkFlowLib.DTO;
using WorkFlowLib.Parameters;
using WorkFlowLib.Results;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 4/25/2017 8:58:49 AM.
    */
    public class UserManager : IUserManager
    {
        private ApiClient _client;

        public ApiClient Client
        {
            get { return _client ?? CreateApiClient(); }
            set { _client = value; }
        }

        public virtual ApiClient CreateApiClient()
        {
            return new ApiClient();
        }

        public UserStaffInfo SearchStaff(string userid)
        {
            RequestResult<UserStaffSearchResult[]> result = Client.User_Staff_Search(userid).Result;
            if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
            {
                UserStaffSearchResult staff = result.ReturnValue.FirstOrDefault();
                if (staff != null)
                {
                    return new UserStaffInfo
                    {
                        Position = staff.POSITION,
                        PositionName = staff.POSITION_NAME,
                        CellPhone = staff.CELLPHONE,
                        Email = staff.EMAIL,
                        Ipt = staff.IPT,
                        Country = staff.COUNTRY,
                        Department = staff.DEPARTMENT,
                        StaffId = staff.STAFF_ID,
                        StaffName = staff.STAFF_NAME,
                        Company = staff.COMPANY,
                        Grade = staff.GRADE,
                        RoleName = staff.ROLE_NAME,
                        DepartmentName = staff.DEPARTMENT_NAME,
                        DepartmentType = staff.DEPARTMENT_TYPE,
                        DepartmentTypeName = staff.DEPARTMENT_TYPENAME,
                        LeaveBalance = staff.LEAVEBALANCE
                    };
                }
            }
            return null;
        }
        public Employee[] GetUserByGrade(string country, string deptcode, string depttype, string @operator, int? grade)
        {
            RequestResult<UserGradeSearchResult[]> result = Client.User_Grade_Search(new UserGradeSearchParams()
            {
                country = country,
                depttype = depttype,
                deptcode = deptcode,
                standard = GetOperatorValue(@operator).ToString(),
                grade = "G" + (grade ?? 0)
            }).Result;
            if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                return result.ReturnValue.Select(p => new Employee(p.STAFF_ID, p.STAFF_NAME)).ToArray();
            return null;
        }
        private int GetOperatorValue(string @operator)
        {
            if (@operator == "<")
                return 2;
            if (@operator == ">")
                return 1;
            return 3;
        }
        public Employee[] GetManager(string userId, int? level, int? maxlevel, string @operator)
        {
            RequestResult<UserManagerSearchResult[]> result =
                Client.User_Manager_Search(userId).Result;
            //level max 1  min 8
            if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                return
                    result.ReturnValue
                        .Where(p => !string.IsNullOrWhiteSpace(p.GRADE))
                        .Where(
                            p =>
                                !level.HasValue ||
                                (@operator == ">" && ((p.GRADE.EqualsIgnoreCase("s") || p.GRADE.EqualsIgnoreCase("gs")) ? 0 : int.Parse(p.GRADE.Substring(1))) < level.Value) ||
                                (@operator == "<" && ((p.GRADE.EqualsIgnoreCase("s") || p.GRADE.EqualsIgnoreCase("gs")) ? 0 : int.Parse(p.GRADE.Substring(1))) > level.Value) ||
                                (@operator == "=" && ((p.GRADE.EqualsIgnoreCase("s") || p.GRADE.EqualsIgnoreCase("gs")) ? 0 : int.Parse(p.GRADE.Substring(1))) == level.Value) ||
                                (@operator == "in" && maxlevel.HasValue &&
                                 (((p.GRADE.EqualsIgnoreCase("s") || p.GRADE.EqualsIgnoreCase("gs")) ? 0 : int.Parse(p.GRADE.Substring(1))) <= level.Value &&
                                  ((p.GRADE.EqualsIgnoreCase("s") || p.GRADE.EqualsIgnoreCase("gs")) ? 0 : int.Parse(p.GRADE.Substring(1))) >= maxlevel.Value)))
                        .Select(p => new Employee(p.MANAGER, p.MANAGER))
                        .ToArray();
            return null;
        }
        public Employee[] GetUserByRole(string country, string deptcode, string role, string depttype, string brand)
        {
            RequestResult<UserRoleSearchResult[]> result = Client.User_Role_Search(new UserRoleSearchParams
            {
                country = country,
                deptcode = deptcode,
                userrole = role,
                depttype = depttype,
                brand = brand
            }).Result;
            if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                return result.ReturnValue.Select(p => new Employee(p.STAFF_ID, p.STAFF_NAME)).ToArray();
            return null;
        }

        public bool UpdateLeaveBalance(string country, string staffId, float days)
        {
            var result = Client.User_AL_Update(country, staffId, days);
            return string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue.IsSuccess();
        }
        public UserHolidayInfo[] GetHolidays(string country, string from, string to)
        {
            var result = Client.Get_Holiday(new HolidayParameter
            {
                AS_CUTY = country,
                AS_FRDT = from,
                AS_TODT = to
            });
            if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
            {
                return result.ReturnValue.Select(p => new UserHolidayInfo
                {
                    Date = p.DATE,
                    Time = p.TIME,
                    Remark = p.REMARK
                }).ToArray();
            }
            return null;
        }
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}