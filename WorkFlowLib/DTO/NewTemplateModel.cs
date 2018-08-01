namespace WorkFlowLib.DTO
{
    public class NewTemplateModel
    {
        public string name { get; set; }
        public string dep { get; set; }
        public int? grade { get; set; }
        public int? templateType { get; set; }
        public string iconUrl { get; set; }
        public string platform { get; set; }
        public string tabs { get; set; }
    }
}