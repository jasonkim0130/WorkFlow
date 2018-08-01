
using System.ComponentModel.DataAnnotations;

namespace WorkFlowLib
{
    public class NewPropertyModel
    {
        [Required]
        public int flowGroupId { get; set; }
        [Required]
        public int fieldTypeId { get; set; }
        public string datasource { get; set; }
        [Required]
        public string fieldName { get; set; }
        [Required]
        public bool compulsory { get; set; }
        public int? code { get; set; }
        public string Tab { get; set; }
        public int? RowIndex { get; set; }
        public int? ColumnIndex { get; set; }
        public string ViewType { get; set; }
        public string Text { get; set; }
        public string Validation { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string HAlign { get; set; }
        public int? FontSize { get; set; }
        public string FontColor { get; set; }
        public bool Multiple { get; set; }
        public string ValidationMsg { get; set; }
        public string BgColor { get; set; }
    }

    public class WFCondition
    {
        [Required]
        public string ComparedData { get; set; }
        [Required]
        public string Operator { get; set; }
        [Required]
        public string Value { get; set; }
        [Required]
        public int FlowId { get; set; }
    }

    public class WFStepCondition
    {
        [Required]
        public int DataKey { get; set; }
        [Required]
        public string Operator { get; set; }
        [Required]
        public string Value { get; set; }
        public string MaxValue { get; set; }
        [Required]
        public int FlowId { get; set; }
        [Required]
        public int StepGroupId { get; set; }
        public int? NextStepGroupId { get; set; }
    }
}