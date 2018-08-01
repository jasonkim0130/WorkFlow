using System.Collections.Generic;

namespace WorkFlowLib.DTO
{
    public class TemplateConfigurationModel
    {
        public int FlowGroupId { get; set; }
        public bool HasCoverUsers { get; set; }
        public string ApprovedArchivePath { get; set; }
        public List<CountryArchivePath> CountryArchivePaths { get; set; }

    }
    public class CountryArchivePath
    {
        public string CountryCode { get; set; }
        public string ApprovedArchivePath { get; set; }
    }
}