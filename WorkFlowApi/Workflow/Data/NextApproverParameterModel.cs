

using WorkFlowLib.Parameters;

namespace Omnibackend.Api.Workflow.Data
{
    public class NextApproverParameterModel
    {
        public int flowTypeId { get; set; }
        public CreateFlowCaseInfo caseInfo { get; set; }

    }

    public class SaveApplicationParameterModel : NextApproverParameterModel
    {
        public string [] nextApprover { get; set; }
    }

    public class EditApplicationParameterModel : SaveApplicationParameterModel
    {
        public int flowCaseId { get; set; }
    }
}