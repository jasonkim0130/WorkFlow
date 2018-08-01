using System;
using System.Data.Entity;
using System.Linq;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.Logic;

namespace WorkFlowLib
{
    public class NotificationManager
    {
        private readonly WorkFlowEntities _entities;
        private readonly string _currentUser;
        public INotificationSender NotificationSender { get; set; }

        public NotificationManager(WorkFlowEntities entities, string user)
        {
            _entities = entities;
            _currentUser = user;
        }

        public WF_CaseNotifications CreateNotification(WF_FlowCases flowCase, int? flowStepId, string comments, NotificationTypes type)
        {
            WF_CaseNotifications message = new WF_CaseNotifications
            {
                Created = DateTime.UtcNow,
                WF_FlowCases = flowCase,
                FlowStepId = flowStepId,
                LastUpdated = DateTime.UtcNow,
                NotificationType = (int)type,
                Comments = comments,
                Sender = _currentUser,
                StatusId = 1
            };
            _entities.WF_CaseNotifications.Add(message);
            return message;
        }

        public WF_CaseNotifications CreateNotification(int flowCaseId, int? flowStepId, string comments, NotificationTypes type)
        {
            WF_CaseNotifications message = new WF_CaseNotifications
            {
                Created = DateTime.UtcNow,
                FlowCaseId = flowCaseId,
                FlowStepId = flowStepId,
                LastUpdated = DateTime.UtcNow,
                NotificationType = (int)type,
                Comments = comments,
                Sender = _currentUser,
                StatusId = 1
            };
            _entities.WF_CaseNotifications.Add(message);
            return message;
        }

        public void PushNotification(int flowCaseId, int? flowstepId, string comments, string receiver, NotificationTypes type, NotificationSources source)
        {
            WF_CaseNotificationReceivers not = new WF_CaseNotificationReceivers
            {
                WF_CaseNotifications = CreateNotification(flowCaseId, flowstepId, comments, type),
                SourceType = (int)source,
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsRead = 0,
                IsDissmissed = 0,
                Receiver = receiver,
                StatusId = 1
            };
            _entities.WF_CaseNotificationReceivers.Add(not);
            NotificationSender.PushNotificationMessages(new[] { receiver }, () => not.CaseNotificationReceiverId);
        }

        public void PushNotification(WF_FlowCases flowCase, int? flowstepId, string comments, string receiver, NotificationTypes type, NotificationSources source)
        {
            WF_CaseNotificationReceivers not = new WF_CaseNotificationReceivers
            {
                WF_CaseNotifications = CreateNotification(flowCase, flowstepId, comments, type),
                SourceType = (int)source,
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsRead = 0,
                IsDissmissed = 0,
                Receiver = receiver,
                StatusId = 1
            };
            _entities.WF_CaseNotificationReceivers.Add(not);
            NotificationSender.PushNotificationMessages(new[] { receiver }, () => not.CaseNotificationReceiverId);
        }

        public void PushNotification(WF_CaseNotifications notification, string receiver, NotificationSources source, Action<WF_CaseNotificationReceivers> handler = null)
        {
            EmailService.SendWorkFlowEmail(
                _entities.GlobalUserView.FirstOrDefault(p => p.EmployeeID == _currentUser)?.EmployeeName,
                new[] { receiver },
                notification.WF_FlowCases.Subject,
                (NotificationTypes)notification.NotificationType);
            WF_CaseNotificationReceivers not = new WF_CaseNotificationReceivers
            {
                WF_CaseNotifications = notification,
                SourceType = (int)source,
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsRead = 0,
                IsDissmissed = 0,
                Receiver = receiver,
                StatusId = 1
            };
            handler?.Invoke(not);
            _entities.WF_CaseNotificationReceivers.Add(not);
            if (not.IsDissmissed == 0)
                NotificationSender.PushNotificationMessages(new[] { receiver }, () => not.CaseNotificationReceiverId);
        }

        public WF_CaseNotificationReceivers SetNotificationAsRead(int caseNotificationReceiverId)
        {
            WF_CaseNotificationReceivers target = _entities.WF_CaseNotificationReceivers.Include("WF_CaseNotifications").FirstOrDefault(p => p.CaseNotificationReceiverId == caseNotificationReceiverId);
            if (target == null) return null;
            target.IsRead = 1;
            _entities.SaveChanges();
            return target;
        }

        public bool SetNotificationDismissed(int caseNotificationReceiverId)
        {
            WF_CaseNotificationReceivers target = _entities.WF_CaseNotificationReceivers.FirstOrDefault(p => p.CaseNotificationReceiverId == caseNotificationReceiverId);
            if (target == null) return false;
            target.IsDissmissed = 1;
            return _entities.SaveChanges() > 0;
        }

        public CaseNotification[] GetComments(int flowCaseId)
        {
            int comments = (int)NotificationTypes.Comments;
            return _entities.WF_CaseNotifications.Where(p => p.FlowCaseId == flowCaseId && p.StatusId > 0 && p.NotificationType == comments).Select(p => new CaseNotification
            {
                CaseNotificationId = p.CaseNotificationId,
                Created = p.Created,
                Sender = p.Sender,
                Comments = p.Comments,
                Source = p.Source,
                NotificationType = p.NotificationType,
            }).AsNoTracking().ToArray();
        }

        public CaseNotification GetNotificationInfo(int caseNotificationReceiverId)
        {
            return
                _entities.WF_CaseNotificationReceivers.Where(p => p.CaseNotificationReceiverId == caseNotificationReceiverId && p.StatusId > 0)
                    .Select(p => new CaseNotification
                    {
                        CaseNotificationReceiverId = p.CaseNotificationReceiverId,
                        CaseNotificationId = p.WF_CaseNotifications.CaseNotificationId,
                        Created = p.Created,
                        Receiver = p.Receiver,
                        Sender = p.WF_CaseNotifications.Sender,
                        Comments = p.WF_CaseNotifications.Comments,
                        Source = p.WF_CaseNotifications.Source,
                        NotificationType = p.WF_CaseNotifications.NotificationType,
                    }).FirstOrDefault();
        }
    }
}