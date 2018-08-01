

using WorkFlowLib.DTO;

namespace Omnibackend.Api.Workflow.Data
{
    public class GetAllFlowCaseModel
    {
        public FlowCaseIdLastUpdatedPair[] pairs { get; set; }
        public int currentPage { get; set; }
    }
}