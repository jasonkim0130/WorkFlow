namespace Omnibackend.Api.Workflow.Data
{
    public class ApproveModel
    {
        public int flowCaseId { get; set; }
        public string[] nextApprovers { get; set; }
    }
}