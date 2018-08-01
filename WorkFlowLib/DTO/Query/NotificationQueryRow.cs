namespace WorkFlowLib.DTO.Query
{
    public class NotificationQueryRow : FlowCaseInfo
    {
        public string Type { get; set; }
        public int CaseNotificationReceiverId { get; set; }
        public int NotificationType { get; set; }
        public int IsRead { get; set; }
        public int StatusId { get; set; }
        public string Source { get; set; }
        public int? SourceType { get; set; }
        public string Sender { get; set; }
        public bool HasAttachment { get; set; }
    }
}