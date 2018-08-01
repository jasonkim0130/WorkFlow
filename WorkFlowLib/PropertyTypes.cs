using System.Collections.Generic;

namespace WorkFlowLib
{
    public enum PropertyTypes
    {
        None = 0,
        List = 1,
        Int,
        DateTime,
        Decimal,
        Date,
        Userno,
        Text,
        Time,
        Country,
        Role,
        Department,
        DeptType,
        RadioGroup,
        Brand
    }

    public class PropertyTypesList
    {
        public static KeyValuePair<string, int>[] GetPropertyTypesList()
        {
            return new[]
            {
                new KeyValuePair<string, int>("NA", (int)PropertyTypes.None),
                new KeyValuePair<string, int>("�б�", (int)PropertyTypes.List),
                new KeyValuePair<string, int>("����", (int)PropertyTypes.Int),
                new KeyValuePair<string, int>("����(yyyy-MM-dd HH:mm)", (int)PropertyTypes.DateTime),
                new KeyValuePair<string, int>("С��", (int)PropertyTypes.Decimal),
                new KeyValuePair<string, int>("����(yyyy-MM-dd)", (int)PropertyTypes.Date),
                new KeyValuePair<string, int>("Ա�����", (int)PropertyTypes.Userno),
                new KeyValuePair<string, int>("�ı�", (int)PropertyTypes.Text),
                new KeyValuePair<string, int>("ʱ��(HH:mm)", (int)PropertyTypes.Time),
                //new KeyValuePair<string, int>("Country", (int)PropertyTypes.Country),
                //new KeyValuePair<string, int>("Role", (int)PropertyTypes.Role),
                //new KeyValuePair<string, int>("Department", (int)PropertyTypes.Department),
                //new KeyValuePair<string, int>("Dept Type", (int)PropertyTypes.DeptType),
                //new KeyValuePair<string, int>("Radio Group", (int)PropertyTypes.RadioGroup),
                //new KeyValuePair<string, int>("Brand", (int)PropertyTypes.Brand),
            };
        }
    }
}