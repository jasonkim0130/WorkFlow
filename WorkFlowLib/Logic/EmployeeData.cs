using System.Linq;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;

namespace WorkFlowLib.Logic
{
    public class EmployeeData
    {
        public static Employee[] GetAllUsers()
        {
            using (WorkFlowEntities entities = new WorkFlowEntities())
            {
                return
                    entities.GlobalUserView
                    .Select(p => new Employee {Name = p.EmployeeName, UserNo = p.EmployeeID, Country = p.Country})
                    .Distinct()
                    .OrderBy(p => p.Country)
                    .ThenBy(p => p.Name)
                    .ToArray();
            }
        }
    }
}