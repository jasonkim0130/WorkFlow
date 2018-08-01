using System;

namespace Omnibackend.Api.Workflow.Data
{
    public class LastCheckedModel
    {
        public int flowCaseId { get; set; }
        public DateTime lastChecked { get; set; }
    }
}