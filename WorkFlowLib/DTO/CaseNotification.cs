using System;

namespace WorkFlowLib.DTO
{
    public class CaseNotification
    {
        public int CaseNotificationReceiverId { get; set; }
        public DateTime Created { get; set; }
        public string Receiver { get; set; }
        public string Sender { get; set; }
        public string Comments { get; set; }
        public string Source { get; set; }
        public int NotificationType { get; set; }
        public int CaseNotificationId { get; set; }
    }
}