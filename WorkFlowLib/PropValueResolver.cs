using System.Linq;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;

namespace WorkFlowLib
{
    public class PropValueResolver
    {
        private readonly PropertiesValue _caseValues;

        public PropValueResolver(PropertiesValue caseValues)
        {
            _caseValues = caseValues;
        }

        public string GetValueByName(string propertyName)
        {
            WF_FlowPropertys prop = _caseValues.PropertyInfo.FirstOrDefault(p => p.StatusId < 0 && p.PropertyName.Equals(propertyName));
            return GetPropertyValue(prop);
        }

        public string GetPropertyValue(WF_FlowPropertys prop)
        {
            if (prop == null)
            {
                return null;
            }
            WF_CasePropertyValues value = _caseValues.Values.FirstOrDefault(p => p.PropertyId == prop.FlowPropertyId);
            return value?.StringValue
                   ?? value?.IntValue?.ToString()
                   ?? value?.DateTimeValue?.ToString("yyyy-MM-ddTHH:mm")
                   ?? value?.NumericValue?.ToString("#.##")
                   ?? value?.TextValue
                   ?? value?.DateValue?.ToString("yyyy-MM-dd")
                   ?? value?.UserNoValue;
        }

    }
}
