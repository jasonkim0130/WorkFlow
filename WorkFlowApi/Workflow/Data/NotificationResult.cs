using System;
using WorkFlowLib.DTO;

namespace Omnibackend.Api.Workflow.Data
{
    public class NotificationResult
    {
        public int flowCaseId { get; set; }
        public int notificationId { get; set; }
        public DateTime created { get; set; }
        public int notificationType { get; set; }
        public CaseNotification[] comments { get; set; }
        public string sender { get; set; }
        public string receiver { get; set; }
        public int isDissmissed { get; set; }
        public object attachments { get; set; }
    }
}