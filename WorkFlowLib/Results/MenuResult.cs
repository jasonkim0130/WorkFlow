namespace WorkFlowLib.Results
{
    public class MenuResult
    {
        public Menu[] data { get; set; }
    }

    public class Menu
    {
        public string PGM_NO { get; set; }
        public string PGM_ID { get; set; }
        public string PGM_NM { get; set; }
        public string PGM_KC { get; set; }
        public int SORT_ORDER { get; set; }
        public string PARENT_PGM { get; set; }
        public int PGM_LV { get; set; }
    }

    public class TreeMenu : Menu
    {
        public TreeMenu[] SubMenus { get; set; }

        public TreeMenu(Menu menu)
        {
            PGM_NO = menu.PGM_NO;
            PGM_ID = menu.PGM_ID;
            PGM_NM = menu.PGM_NM;
            PGM_KC = menu.PGM_KC;
            SORT_ORDER = menu.SORT_ORDER;
            PARENT_PGM = menu.PARENT_PGM;
            PGM_LV = menu.PGM_LV;
        }
    }

    public class WebReportAuthResult
    {
        public CompanyList[] data { get; set; }
    }

    public class CompanyList
    {
        public string CODE { get; set; }
        public string NAME { get; set; }
    }
}
