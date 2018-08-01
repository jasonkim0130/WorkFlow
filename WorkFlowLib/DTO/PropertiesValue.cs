using System.Collections.Generic;
using WorkFlowLib.Data;

namespace WorkFlowLib.DTO
{
    public class PropertiesValue
    {
        public IEnumerable<WF_FlowPropertys> PropertyInfo { get; set; }
        public IEnumerable<WF_CasePropertyValues> Values { get; set; }
    }
}