using System;

namespace WorkFlowLib.Logic
{
    /**
    * Created by jeremy on 3/29/2017 6:10:58 PM.
    */
    public interface INotificationSender
    {
        void PushInboxMessages(string[] users, int flowCaseId);
        void PushNotificationMessages(string[] users, Func<int> notificationIdResolver);
        void Send();
    }
}