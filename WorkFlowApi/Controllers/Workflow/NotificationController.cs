using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Newtonsoft.Json;
using Omnibackend.Api.Workflow.Data;
using Dreamlab.Core;
using WorkFlowLib;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;

namespace Omnibackend.Api.Controllers.Workflow
{
    [RoutePrefix("workflow/notification")]
    [Authorize]
    public class NotificationController : BaseWFApiController
    {


        [Route("get_notification_info")]
        [HttpGet]
        public object get_notification_info(int notificationId)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-get_notification_info", notificationId.ToString());
            ApplicationUser manager = new ApplicationUser(Entities, User.Identity.Name);
            WF_CaseNotificationReceivers receiver = manager.NotificationManager.SetNotificationAsRead(notificationId);
            manager.UpdateLastChecked(receiver.WF_CaseNotifications.FlowCaseId, User.Identity.Name);
            manager.NotificationManager.SetNotificationDismissed(notificationId);
            return new NotificationResult
            {
                flowCaseId = receiver.WF_CaseNotifications.FlowCaseId,
                notificationId = notificationId,
                created = receiver.Created,
                notificationType = receiver.WF_CaseNotifications.NotificationType,
                comments =
                    receiver.WF_CaseNotifications.WF_FlowCases.WF_CaseNotifications.Where(
                        p => p.StatusId > 0 && p.NotificationType == (int) NotificationTypes.Comments || p.NotificationType == (int)NotificationTypes.AppFinishedAbort || p.NotificationType == (int)NotificationTypes.RejectApp || p.NotificationType == (int)NotificationTypes.AppFinishedRejected)
                        .Select(p => new CaseNotification
                        {
                            CaseNotificationId = p.CaseNotificationId,
                            Created = p.Created,
                            Sender = p.Sender,
                            Comments = p.Comments,
                            Source = p.Source,
                            NotificationType = p.NotificationType,
                        }).ToArray(),
                sender = receiver.WF_CaseNotifications.Sender,
                receiver = receiver.Receiver,
                isDissmissed = receiver.IsDissmissed,
                attachments =
                    receiver.WF_CaseNotifications.WF_FlowCases.WF_FlowCases_Attachments.Where(p => p.StatusId > 0)
                        .Select(p => new
                        {
                            attachmentId = p.AttachementId,
                            fileName = p.OriFileName,
                            fileSize = p.FileSize,
                            lastUpdated = p.LastUpdated
                        })
            };
        }

        [Route("refresh_notifications")]
        [HttpPost]
        public object refresh_notifications(RefreshNotificationModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-refresh_notifications",JsonConvert.SerializeObject(model));
            var result = new List<NotificationResult>();
            result.AddRange(Entities.WF_CaseNotificationReceivers.Include("WF_CaseNotifications")
                .Where(p => p.CaseNotificationReceiverId > model.maxNotificationId).ToArray()
                .Select(p => new NotificationResult
                {
                    flowCaseId = p.WF_CaseNotifications.FlowCaseId,
                    notificationId = p.CaseNotificationReceiverId,
                    created = p.Created,
                    notificationType = p.WF_CaseNotifications.NotificationType,
                    comments =
                        p.WF_CaseNotifications.WF_FlowCases.WF_CaseNotifications.Where(
                            q => q.StatusId > 0 && q.NotificationType == (int) NotificationTypes.Comments)
                            .Select(q => new CaseNotification
                            {
                                CaseNotificationId = p.CaseNotificationId,
                                Created = q.Created,
                                Sender = q.Sender,
                                Comments = q.Comments,
                                Source = q.Source,
                                NotificationType = q.NotificationType,
                            }).ToArray(),
                    sender = p.WF_CaseNotifications.Sender,
                    receiver = p.Receiver,
                    isDissmissed = p.IsDissmissed,
                    attachments =
                        p.WF_CaseNotifications.WF_FlowCases.WF_FlowCases_Attachments.Where(q => q.StatusId > 0)
                            .Select(q => new
                            {
                                attachmentId = q.AttachementId,
                                fileName = q.OriFileName,
                                fileSize = q.FileSize,
                                lastUpdated = q.LastUpdated
                            })
                }).ToArray());
            result.AddRange(Entities.WF_CaseNotificationReceivers.Include("WF_CaseNotifications")
                .Where(p => model.notDismissedIds.Contains(p.CaseNotificationReceiverId) && p.IsDissmissed == 1)
                .ToArray()
                .Select(p => new NotificationResult
                {
                    flowCaseId = p.WF_CaseNotifications.FlowCaseId,
                    notificationId = p.CaseNotificationReceiverId,
                    created = p.Created,
                    notificationType = p.WF_CaseNotifications.NotificationType,
                    comments =
                        p.WF_CaseNotifications.WF_FlowCases.WF_CaseNotifications.Where(
                            q => q.StatusId > 0 && q.NotificationType == (int) NotificationTypes.Comments)
                            .Select(q => new CaseNotification
                            {
                                CaseNotificationId = p.CaseNotificationId,
                                Created = q.Created,
                                Sender = q.Sender,
                                Comments = q.Comments,
                                Source = q.Source,
                                NotificationType = q.NotificationType,
                            }).ToArray(),
                    sender = p.WF_CaseNotifications.Sender,
                    receiver = p.Receiver,
                    isDissmissed = p.IsDissmissed,
                    attachments =
                        p.WF_CaseNotifications.WF_FlowCases.WF_FlowCases_Attachments.Where(q => q.StatusId > 0)
                            .Select(q => new
                            {
                                attachmentId = q.AttachementId,
                                fileName = q.OriFileName,
                                fileSize = q.FileSize,
                                lastUpdated = q.LastUpdated
                            })
                }).ToArray());
            return result;
        }

        [Route("move_to_archive")]
        [HttpPost]
        public object move_to_archive(int[] notificationids)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-refresh_notifications", JsonConvert.SerializeObject(notificationids));
            if (Entities.Database.ExecuteSqlCommand(
                $"UPDATE dbo.WF_CaseNotificationReceivers SET IsRead = 1, IsDissmissed = 1 WHERE CaseNotificationReceiverId IN ({string.Join(",", notificationids)})") > 0)
            {
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            }
            return new { ret_code = RetCode.Failure, ret_msg = string.Empty };
        }

    }
}
