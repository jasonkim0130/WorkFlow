

namespace Omnibackend.Api.Workflow.Data
{
    public class CreateCancelParameterModel
    {
        public int flowcaseid { get; set; }
        public string[] approvers { get; set; }

    }
}