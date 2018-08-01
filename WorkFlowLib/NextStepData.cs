using System.Collections.Generic;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;

namespace WorkFlowLib
{
    public class NextStepData
    {
        public List<Employee[]> EmployeeList = new List<Employee[]>();
        public int NextStepGroupId { get; set; }
        public WF_FlowSteps[] NextSteps { get; set; }
        
        public void AddEmpoyees(Employee[] findUser)
        {
            EmployeeList.Add(findUser);
        }
    }
}