using Dreamlab.Core;
using System.Collections.Generic;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 2/13/2017 5:13:20 PM.
    */
    public class Consts
    {
        public const int Approved = 1;
        public const int Rejected = 2;
        public const int Aborted = 3;
        public const int ConditionAll = 1;
        public const int ConditionAny = 2;
        public const int ApplicationCancelled = -1;
        public const int ApplicationObsoleted = -2;
        public const int ApplicationDeleted = -3;

        //闭店申请里要根据shopcode来获取flowcaseinfo
        public const string ShopCodePropertyName = "ShopCode";
        public static readonly string[] Brands = { "HTN", "LEO", "ROS", "HCT", "HTJ", "APM" };
        public static readonly string[] Countries = { "CHN", "TWN", "HKG", "SGP", "KOR", "MYS" };
        public static readonly Dictionary<string, string[]> BrandsOfContries = new Dictionary<string, string[]>
        {
            { "TWN", new string[] { "HCT", "HTN", "ROS", "APM" } },
            { "CHN", new string[] { "HCT", "HTN", "ROS" } },
            { "KOR", new string[] { "HCT", "HTN" } },
            { "HKG", new string[] { "HTN", "LEO"} },
            { "SGP", new string[] { "HCT", "HTN", "ROS" } },
            { "MYS", new string[] { "HCT", "HTN", "ROS" } }
        };
        public static string GetBrandFullName(string brand)
        {
            if (brand.EqualsIgnoreCaseAndBlank("ROS"))
                return "ROOTS";
            if (brand.EqualsIgnoreCaseAndBlank("HTN"))
                return "HangTen";
            if (brand.EqualsIgnoreCaseAndBlank("HCT"))
                return "H:Connect";
            if (brand.EqualsIgnoreCaseAndBlank("APM"))
                return "Arnold Palmer";
            if (brand.EqualsIgnoreCaseAndBlank("LEO"))
                return "LEO";
            return brand;
        }
        public static string GetApiCountry()
        {
            return "TWN";
        }

        public const string WorkFlowApIToken = "EFalke3l2=-zefzef";

        #region CAPEX
        /// <summary>
        /// CHN HCT CAPEX
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, double>> CHN_HCT_CAPEX = new Dictionary<string, Dictionary<string, double>>
        {
            #region Dept.Store Counter
            {
                "Dept.Store Counter",
                new Dictionary<string, double>
                {
                    { "<100", 2794.8 },
                    { "100-150", 2022.4 },
                    { "150-200", 2022.4}
                }
            },
            #endregion
            #region Shopping Mall
            {
                "Shopping Mall",
                new Dictionary<string, double>
                {
                    { "<100", 3649.2 },
                    { "100-150", 3178.1 },
                    { "150-200", 3178.1},
                    { ">200", 4343.5}
                }
            },
            #endregion
            #region Outlet
            {
                "Outlet",
                new Dictionary<string, double>
                {
                    { "<100", 3273.2 },
                    { "100-150", 3280.8 },
                    { "150-200", 2677.3}
                }
            }
            #endregion
        };

        /// <summary>
        /// CHN ROS CAPEX
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, double>> CHN_ROS_CAPEX = new Dictionary<string, Dictionary<string, double>>
        {
            #region Dept.Store Counter
            {
                "Dept.Store Counter",
                new Dictionary<string, double>
                {
                    { "<100", 3074.3 },
                    { "100-150", 2224.6 },
                    { "150-200", 2224.6}
                }
            },
            #endregion
            #region Shopping Mall
            {
                "Shopping Mall",
                new Dictionary<string, double>
                {
                    { "<100", 4014.1 },
                    { "100-150", 3495.9 },
                    { "150-200", 3495.9},
                    { ">200", 4777.9}
                }
            },
            #endregion
            #region Outlet
            {
                "Outlet",
                new Dictionary<string, double>
                {
                    { "<100", 3600.5 },
                    { "100-150", 3608.9 },
                    { "150-200", 2945.0}
                }
            }
            #endregion
        };

        /// <summary>
        /// CHN HTN CAPEX
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, double>> CHN_HTN_CAPEX = new Dictionary<string, Dictionary<string, double>>
        {
            #region Dept.Store Counter
            {
                "Dept.Store Counter",
                new Dictionary<string, double>
                {
                    { "<100", 2235.8 },
                    { "100-150", 1617.9 },
                    { "150-200", 1617.9}
                }
            },
            #endregion
            #region Shopping Mall
            {
                "Shopping Mall",
                new Dictionary<string, double>
                {
                    { "<100", 2919.4 },
                    { "100-150", 2542.5 },
                    { "150-200", 2542.5},
                    { ">200", 3474.8}
                }
            },
            #endregion
            #region Outlet
            {
                "Outlet",
                new Dictionary<string, double>
                {
                    { "<100", 2618.6 },
                    { "100-150", 2624.6 },
                    { "150-200", 2141.8}
                }
            }
            #endregion
        };

        /// <summary>
        /// TWN HCT CAPEX
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, double>> TWN_HCT_CAPEX = new Dictionary<string, Dictionary<string, double>>
        {
            #region Dept.Store Counter
            {
                "Dept.Store Counter",
                new Dictionary<string, double>
                {
                    { "<1000", 20493.6 },
                    { "1000-1500", 20493.6 },
                    { "1500-2000", 20493.6},
                    { ">2000", 20493.6}
                }
            },
            #endregion
            #region Shopping Mall
            {
                "Shopping Mall",
                new Dictionary<string, double>
                {
                    { "<1000", 20493.6 },
                    { "1000-1500", 20493.6 },
                    { "1500-2000", 20493.6},
                    { ">2000", 20493.6}
                }
            },
            #endregion
            #region Street Store
            {
                "Street Store",
                new Dictionary<string, double>
                {
                    { "<1000", 20493.6 },
                    { "1000-1500", 20493.6 },
                    { "1500-2000", 20493.6},
                    { ">2000", 20493.6}
                }
            },
            #endregion
            #region Outlet
            {
                "Outlet",
                new Dictionary<string, double>
                {
                    { "<1000", 20493.6 },
                    { "1000-1500", 20493.6 },
                    { "1500-2000", 20493.6},
                    { ">2000", 20493.6}
                }
            }
            #endregion
        };

        /// <summary>
        /// TWN ROS CAPEX
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, double>> TWN_ROS_CAPEX = new Dictionary<string, Dictionary<string, double>>
        {
            #region Dept.Store Counter
            {
                "Dept.Store Counter",
                new Dictionary<string, double>
                {
                    { "<1000", 25617 },
                    { "1000-1500", 25617 },
                    { "1500-2000", 25617},
                    { ">2000", 25617}
                }
            },
            #endregion
            #region Shopping Mall
            {
                "Shopping Mall",
                new Dictionary<string, double>
                {
                    { "<1000", 25617 },
                    { "1000-1500", 25617 },
                    { "1500-2000", 25617},
                    { ">2000", 25617}
                }
            },
            #endregion
            #region Street Store
            {
                "Street Store",
                new Dictionary<string, double>
                {
                    { "<1000", 25617 },
                    { "1000-1500", 25617 },
                    { "1500-2000", 25617},
                    { ">2000", 25617}
                }
            },
            #endregion
            #region Outlet
            {
                "Outlet",
                new Dictionary<string, double>
                {
                    { "<1000", 25617 },
                    { "1000-1500", 25617 },
                    { "1500-2000", 25617},
                    { ">2000", 25617}
                }
            }
            #endregion
        };

        /// <summary>
        /// TWN HTN CAPEX
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, double>> TWN_HTN_CAPEX = new Dictionary<string, Dictionary<string, double>>
        {
            #region Dept.Store Counter
            {
                "Dept.Store Counter",
                new Dictionary<string, double>
                {
                    { "<1000", 18444.2 },
                    { "1000-1500", 18444.2 },
                    { "1500-2000", 18444.2},
                    { ">2000", 18444.2}
                }
            },
            #endregion
            #region Shopping Mall
            {
                "Shopping Mall",
                new Dictionary<string, double>
                {
                    { "<1000", 18444.2 },
                    { "1000-1500", 18444.2 },
                    { "1500-2000", 18444.2},
                    { ">2000", 18444.2}
                }
            },
            #endregion
            #region Street Store
            {
                "Street Store",
                new Dictionary<string, double>
                {
                    { "<1000", 18444.2 },
                    { "1000-1500", 18444.2 },
                    { "1500-2000", 18444.2},
                    { ">2000", 18444.2}
                }
            },
            #endregion
            #region Outlet
            {
                "Outlet",
                new Dictionary<string, double>
                {
                    { "<1000", 18444.2 },
                    { "1000-1500", 18444.2 },
                    { "1500-2000", 18444.2},
                    { ">2000", 18444.2}
                }
            }
            #endregion
        };

        /// <summary>
        /// TWN APM CAPEX
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, double>> TWN_APM_CAPEX = new Dictionary<string, Dictionary<string, double>>
        {
            #region Dept.Store Counter
            {
                "Dept.Store Counter",
                new Dictionary<string, double>
                {
                    { "<1000", 23055.3 },
                    { "1000-1500", 23055.3 },
                    { "1500-2000", 23055.3},
                    { ">2000", 23055.3}
                }
            },
            #endregion
            #region Shopping Mall
            {
                "Shopping Mall",
                new Dictionary<string, double>
                {
                    { "<1000", 23055.3 },
                    { "1000-1500", 23055.3 },
                    { "1500-2000", 23055.3},
                    { ">2000", 23055.3}
                }
            },
            #endregion
            #region Street Store
            {
                "Street Store",
                new Dictionary<string, double>
                {
                    { "<1000", 23055.3 },
                    { "1000-1500", 23055.3 },
                    { "1500-2000", 23055.3},
                    { ">2000", 23055.3}
                }
            },
            #endregion
            #region Outlet
            {
                "Outlet",
                new Dictionary<string, double>
                {
                    { "<1000", 23055.3 },
                    { "1000-1500", 23055.3 },
                    { "1500-2000", 23055.3},
                    { ">2000", 23055.3}
                }
            }
            #endregion
        };

        public static readonly Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>> CAPEX = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>
        {
            {
                "CHN",
                new Dictionary<string, Dictionary<string, Dictionary<string, double>>>
                {
                    { "HCT", CHN_HCT_CAPEX},
                    { "ROS", CHN_ROS_CAPEX},
                    { "HTN", CHN_HTN_CAPEX}
                }
            },
            {
                "TWN",
                new Dictionary<string, Dictionary<string, Dictionary<string, double>>>
                {
                    { "HCT", TWN_HCT_CAPEX},
                    { "ROS", TWN_ROS_CAPEX},
                    { "HTN", TWN_HTN_CAPEX},
                    { "APM", TWN_APM_CAPEX}
                }
            },
        };
        #endregion
    }
}