using System.Linq;
using WorkFlowLib.DTO;
using WorkFlowLib.Parameters;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 4/6/2017 9:03:25 PM.
    */
    public class UserResolver
    {
        private readonly IUserManager _userManager;

        public UserResolver(IUserManager userManager)
        {
            _userManager = userManager;
        }

        public int? UserType { get; set; }
        public string Applicant { get; set; }
        public string CurrentUser { get; set; }
        public PropertyInfo[] PropertyValues { get; set; }
        public string SelectedUser { get; set; }
        public string SelectedUsername { get; set; }
        public int? UserRole { get; set; }
        public string Operator { get; set; }
        public int? Grade { get; set; }
        public int? ManagerOption { get; set; }
        public int? ManagerLevel { get; set; }
        public int? ManagerMaxLevel { get; set; }
        public string ManagerLevelOperator { get; set; }
        public int? CountryType { get; set; }
        public int? DeptType { get; set; }
        public int? BrandType { get; set; }
        public string FixedCountry { get; set; }
        public string FixedBrand { get; set; }
        public string FixedDept { get; set; }
        public int? DeptTypeSource { get; set; }
        public string FixedDeptType { get; set; }

        public delegate string UserNameProvider(string userNo);
        public UserNameProvider UserNameByNo { get; set; }
        public Employee[] FindUser()
        {
            if (UserType != null)
            {
                string country = PropertyValues.FirstOrDefault(p => p.Type == 9)?.Value ?? Consts.GetApiCountry();//#TODO
                string deptcode = PropertyValues.FirstOrDefault(p => p.Type == 11)?.Value ?? "%";
                string depttype = PropertyValues.FirstOrDefault(p => p.Type == 12)?.Value ?? "%";
                if (UserType == (int)ApproverType.PredefinedRole)
                {
                    if ((CountryType.HasValue && CountryType.Value == 0) 
                        || (DeptType.HasValue && DeptType.Value == 0)
                        || (DeptTypeSource.HasValue && DeptTypeSource.Value == 0))
                    {
                        UserStaffInfo result = _userManager.SearchStaff(Applicant);
                        if (result != null && CountryType == 0)
                        {
                            country = result.Country;
                        }
                        if (result != null && DeptType == 0)
                        {
                            deptcode = result.Department;
                        }
                        if (result != null && DeptTypeSource == 0)
                        {
                            depttype = result.DepartmentType;
                        }
                    }
                    if (CountryType.HasValue && CountryType.Value == 2)
                    {
                        country = FixedCountry;
                    }
                    if (DeptType.HasValue && DeptType.Value == 2)
                    {
                        deptcode = FixedDept;
                    }
                    if (DeptTypeSource.HasValue && DeptTypeSource.Value == 2)
                    {
                        depttype = FixedDeptType;
                    }
                    string brand = "";
                    switch (BrandType)
                    {
                        case 1:
                            brand = PropertyValues.FirstOrDefault(p => p.Type == 14)?.Value;
                            break;
                        case 2:
                            brand = FixedBrand;
                            break;
                    }
                    return GetUserByRole(country, deptcode, UserRole.ToString(), depttype, brand);
                }
                if (UserType == (int)ApproverType.PredefinedReportingLine)
                {
                    var employees = GetManager();
                    if (UserNameByNo != null)
                    {
                        foreach (var employee in employees)
                        {
                            employee.Name = UserNameByNo(employee.UserNo)??employee.UserNo;
                        }
                    }
                    return employees;
                }
                if (UserType == (int)ApproverType.RoleCriteria)
                {
                    if ((CountryType.HasValue && CountryType.Value == 0) || (DeptType.HasValue && DeptType.Value == 0))
                    {
                        UserStaffInfo result = _userManager.SearchStaff(Applicant);
                        if (result != null && CountryType == 0)
                        {
                            country = result.Country;
                        }
                        if (result != null && DeptType == 0)
                        {
                            deptcode = result.Department;
                        }
                    }
                    if (CountryType.HasValue && CountryType.Value == 2)
                    {
                        country = FixedCountry;
                    }
                    if (DeptType.HasValue && DeptType.Value == 2)
                    {
                        deptcode = FixedDept;
                    }
                    return GetUserByGrade(country, deptcode, depttype);
                }
            }
            return new[] { new Employee(SelectedUser, SelectedUsername) };
        }

        private Employee[] GetUserByGrade(string country, string deptcode, string depttype)
        {
            return _userManager.GetUserByGrade(country, deptcode, depttype, Operator, Grade);
        }

        private Employee[] GetManager()
        {
            string userId = ManagerOption != null && ManagerOption == 2 ? Applicant : CurrentUser;
            return _userManager.GetManager(userId, ManagerLevel, ManagerMaxLevel, ManagerLevelOperator);
        }

        private Employee[] GetUserByRole(string country, string deptcode, string role, string depttype, string brand)
        {
            return _userManager.GetUserByRole(country, deptcode, role, depttype, brand);
        }
    }
}