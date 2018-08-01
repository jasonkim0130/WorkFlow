namespace WorkFlow.Controllers
{
    public class WorkFlowBadgeData
    {
        public static object GetInboxBadgeData(int value)
        {
            return new { Page = new[] { new { expression = ".Inbox", value = value } } };
        }

        public static object GetNotificationBadgeData(int value)
        {
            return new { Page = new[] { new { expression = ".Notification", value = value } } };
        }

        public static object GetPendingBadgeData(int value)
        {
            return new { Page = new[] { new { expression = ".Pending", value = value } } };
        }

        public static object GetArchiveBadgeData(int value)
        {
            return new { Page = new[] { new { expression = ".Archive", value = value } } };
        }

        public static object GetDraftBadgeDat(int value)
        {
            return new { Page = new[] { new { expression = ".Draft", value = value } } };
        }
    }
}