using System;
using System.Collections.Generic;

namespace WorkFlowLib.Parameters
{
    public class CreateFlowCaseInfo
    {
        private PropertyInfo[] _Properties;
        public int FlowId { get; set; }
        public int? RelatedCaseId { get; set; }
        public string Subject { get; set; }
        public DateTime? Deadline { get; set; }
        public PropertyInfo[] Properties
        {
            get
            {
                if (_Properties == null && PropertyList != null)
                {
                    return PropertyList.ToArray();
                }
                return _Properties;
            }
            set
            {
                _Properties = value;
            }
        }
        public Attachment[] Attachments { get; set; }
        public string[] NotifyUsers { get; set; }
        public string Dep { get; set; }
        public string[] Approver { get; set; }
        public List<PropertyInfo> PropertyList { get; set; }
        public string[] CoverDuties { get; set; }
    }
}