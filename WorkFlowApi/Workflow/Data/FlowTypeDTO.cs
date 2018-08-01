namespace Omnibackend.Api.Workflow.Data
{
    /**
    * Created by jeremy on 2/17/2017 3:06:56 PM.
    */
    public class FlowTypeDTO
    {
        public int templateId { get; set; }
        public string templateCode { get; set; }
        public string templateName { get; set; }
        public int? templateTypeId { get; set; }
        public string IconUrl { get; set; }
        public string Tabs { get; set; }
    }
}