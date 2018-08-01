

using WorkFlowLib.Data;

namespace WorkFlowLib.DTO
{
    public class FlowTypeModel
    {
        public WF_FlowTypes Template { get; set; }
        public bool IsEditing { get; set; }
        public int? Version { get; set; }
    }
}