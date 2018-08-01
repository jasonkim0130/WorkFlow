namespace Omnibackend.Api.Workflow.Data
{
    public class RefreshNotificationModel
    {
        public int maxNotificationId { get; set; }
        public int[] notDismissedIds { get; set; }
    }
}