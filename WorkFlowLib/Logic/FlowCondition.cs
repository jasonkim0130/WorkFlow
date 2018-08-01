using System;
using System.Linq;
using System.Xml.Linq;
using Dreamlab.Core;
using Unity;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.Parameters;

namespace WorkFlowLib.Logic
{
    /**
    * Created by jeremy on 3/2/2017 4:19:01 PM.
    */
    public class FlowCondition
    {
        private readonly WF_FlowPropertys _property;
        private readonly string _otherProp;
        private readonly string _operator;
        private readonly string _value;
        private readonly string _userid;
        private readonly string _maxValue;
        public static readonly string[] Operators = { "=", ">", ">=", "<", "<=", "!=" };
        public static readonly string[] SimpleOperators = { "=", ">", "<" };
        public static readonly string[] SimpleOperatorsWithIn = { "=", ">", "<", "in" };

        public FlowCondition(string otherProp, string @operator, string value, string maxValue, string userid)
        {
            _property = null;
            _otherProp = otherProp;
            _operator = @operator;
            _value = value;
            _maxValue = maxValue;
            _userid = userid;
        }

        public FlowCondition(string otherProp, string @operator, string value, string userid)
        {
            _property = null;
            _otherProp = otherProp;
            _operator = @operator;
            _value = value;
            _userid = userid;
        }

        public FlowCondition(WF_FlowPropertys property, string @operator, string value)
        {
            _property = property;
            _otherProp = null;
            _operator = @operator;
            _value = value;
            _userid = null;
        }

        public bool IsValid()
        {
            if (_property == null)
                return false;
            if (_property.PropertyType == 1)
            {
                if (_property.StatusId == -1)
                    return true;
                return XElement.Parse(_property.DataSource)
                    .Elements("item").Any(p => p.Value.EqualsIgnoreCaseAndBlank(_value));
            }
            if (_property.PropertyType == 2)
            {
                int s;
                return int.TryParse(_value, out s);
            }
            if (_property.PropertyType == 3)
            {
                DateTime s;
                return DateTime.TryParse(_value, out s);
            }
            if (_property.PropertyType == 4)
            {
                float s;
                return float.TryParse(_value, out s);
            }
            if (_property.PropertyType == 5)
            {
                DateTime s;
                return DateTime.TryParse(_value, out s);
            }
            if (_property.PropertyType == 8)
            {
                TimeSpan s;
                return TimeSpan.TryParse(_value, out s);
            }
            if (_property.PropertyType == 9)
            {
                return new[] { "chn", "twn", "hkg", "kor", "sgp", "mys" }.Any(p => p.EqualsIgnoreCaseAndBlank(_value));
            }
            return !string.IsNullOrEmpty(_value);
        }

        public bool Check(PropertyInfo[] values)
        {
            if (_otherProp.EqualsIgnoreCaseAndBlank("id"))
            {
                return Compare(_userid, _value);
            }
            if (_otherProp.EqualsIgnoreCaseAndBlank("grade"))
            {
                using (IUserManager um = Singleton<IUnityContainer>.Instance.Resolve<IUserManager>())
                {
                    UserStaffInfo staff = um.SearchStaff(_userid);
                    if (staff == null)
                        return false;
                    bool isNumberA = int.TryParse(staff.Grade.Substring(1), out var a);
                    bool isNumberB = int.TryParse(_value, out var b);
                    if (isNumberA && isNumberB)
                    {
                        if (_operator.ToLower().Equals("in") && !string.IsNullOrWhiteSpace(_maxValue))
                        {
                            bool isNumberC = int.TryParse(_maxValue, out var c);
                            if (isNumberC)
                            {
                                return a <= b && a >= c;
                            }
                            return false;
                        }
                        return _operator.Equals("=") ? Compare(a, b) : Compare(b, a);
                    }
                    return false;
                }
            }
            if (_otherProp.EqualsIgnoreCaseAndBlank("approverno"))
            {
                using (IUserManager um = Singleton<IUnityContainer>.Instance.Resolve<IUserManager>())
                {
                    UserStaffInfo staff = um.SearchStaff(_userid);
                    if (staff == null)
                        return false;
                    return Compare(staff.StaffId, _value);
                }
            }
            PropertyInfo item = values.FirstOrDefault(p => p.Id == _property.FlowPropertyId);
            if (item == null)
                return false;
            if (_property.PropertyType == 2)
            {
                return int.TryParse(item.Value, out var a) && int.TryParse(_value, out var b) && Compare<int>(a, b);
            }
            if (_property.PropertyType == 3)
            {
                return DateTime.TryParse(item.Value, out var a) && DateTime.TryParse(_value, out var b) && Compare<DateTime>(a, b);
            }
            if (_property.PropertyType == 4)
            {
                return float.TryParse(item.Value, out var a) && float.TryParse(_value, out var b) && Compare<float>(a, b);
            }
            if (_property.PropertyType == 5)
            {
                return DateTime.TryParse(item.Value, out var a) && DateTime.TryParse(_value, out var b) && Compare<DateTime>(a.Date, b.Date);
            }
            if (_property.PropertyType == 8)
            {
                return TimeSpan.TryParse(item.Value, out var a) && TimeSpan.TryParse(_value, out var b) && Compare<TimeSpan>(a, b);
            }
            return Compare(item.Value.Trim(), _value.Trim());
        }

        private bool Compare<T>(T a, T b) where T : IComparable<T>
        {
            switch (_operator)//"=", ">", ">=", "<", "<=", "!="
            {
                case "=":
                    {
                        return a.CompareTo(b) == 0;
                    }
                case ">":
                    {
                        return a.CompareTo(b) > 0;
                    }
                case ">=":
                    {
                        return a.CompareTo(b) >= 0;
                    }
                case "<":
                    {
                        return a.CompareTo(b) < 0;
                    }
                case "<=":
                    {
                        return a.CompareTo(b) <= 0;
                    }
                case "!=":
                    {
                        return a.CompareTo(b) != 0;
                    }
            }
            return false;
        }
    }
}