using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PushSharp.Google;
using WorkFlowLib;
using WorkFlowLib.Data;
using WorkFlowLib.Logic;

namespace WorkFlow.Service
{
    public enum PushType
    {
        Inbox = 0,
        Notification
    }

    public class PushTargetData
    {
        private string[] _users;
        public string[] Users {
            get
            {
                for (int i = 0; i < _users.Length; i++)
                {
                    _users[i] = _users[i].ToLower();
                }
                return _users;
            }
            set { _users = value; }
        }
        public int Id { get; set; }
        public PushType Type { get; set; }
        public Func<int> ReadCaseNotificationReceiverId { get; set; }
    }

    public class PushService : INotificationSender
    {
        private const string AppKey =
            "AAAAwGPBfcA:APA91bGeFK7xVAmo4pQqKA5WODBl1rSIbjq9GQaXexn8yVCBJkY4qxtY8ObQmvQ99_hCyRAGpDwNsFPveBWsBZHyruwxRNURNAR3j-bxxX0AXu1mroeFvAKt8p8_nYK7u7m8GXW8G5DG";

        private const string AppUatKey =
            "AAAAiFY2rsw:APA91bHe42_S26dVzssH2q0vlHDrkmmpr2LLXJGWk3LoIp4DI0G6nDNUMVBfICm2p-v3-ncQ-4OtRO_qfdRovD2tuVqiqvkMbSE7gcokaule03ccIKofhvx10gle_ES67UyOl-c08U48";

        private GcmServiceBroker Broker { get; set; }
        private readonly ConcurrentBag<PushTargetData> _pushdata = new ConcurrentBag<PushTargetData>();

        public PushService()
        {
            Broker = new GcmServiceBroker(new GcmConfiguration(Codehelper.IsUat ? AppUatKey : AppKey));
        }

        public void PushInboxMessages(string[] users, int flowCaseId)
        {
            if (users != null && users.Length > 0)
            {
                _pushdata.Add(new PushTargetData {Id = flowCaseId, Users = users, Type = PushType.Inbox});
            }
        }

        public void PushNotificationMessages(string[] users, Func<int> read)
        {
            _pushdata.Add(new PushTargetData
            {
                ReadCaseNotificationReceiverId = read,
                Users = users,
                Type = PushType.Notification
            });
        }

        public void Send()
        {
            foreach (PushTargetData data in _pushdata.Where(p => p.Type == PushType.Notification))
            {
                data.Id = data.ReadCaseNotificationReceiverId();
                data.ReadCaseNotificationReceiverId = null;
            }
            Task.Factory.StartNew(() =>
            {
                Broker.Start();
                using (WorkFlowEntities entities = new WorkFlowEntities())
                {
                    foreach (PushTargetData data in _pushdata.Where(p => p.Type == PushType.Inbox))
                    {
                        var action =
                            entities.WF_CaseUserActions.AsNoTracking().FirstOrDefault(p => p.FlowCaseId == data.Id);
                        var receivers =
                            entities.UserDeviceTokens.AsNoTracking().Where(
                                p => data.Users.Contains(p.UserNo.ToLower()) && p.DeviceToken != null && p.StatusId > 0);
                        if (action != null && receivers.Any())
                        {
                            foreach (var group in receivers.GroupBy(p => p.LastDeviceLang))
                            {
                                
                                GcmNotification notification = new GcmNotification
                                {
                                    RegistrationIds = group.Select(p => p.DeviceToken).ToList(),
                                    Data = JObject.FromObject(new
                                    {
                                        type = "Inbox",
                                        actionId = action.CaseUserActionId,
                                        flowCaseId = action.FlowCaseId,
                                        department = action.WF_FlowCases.Department,
                                        applicant = entities.GlobalUserView.FirstOrDefault(p=>p.EmployeeID.Equals(action.WF_FlowCases.UserNo))?.EmployeeName ?? action.WF_FlowCases.UserNo,
                                        templateName =
                                            entities.WF_Flows.First(p => p.FlowId == action.WF_FlowCases.FlowId)
                                                .WF_FlowGroups.WF_FlowTypes.Name,
                                        subject = action.WF_FlowCases.Subject,
                                        templateType = entities.WF_Flows.First(p => p.FlowId == action.WF_FlowCases.FlowId)
                                                           .WF_FlowGroups.WF_FlowTypes.TemplateType ?? 0,
                                        ver = action.WF_FlowCases.Ver ?? 0,
                                        received = action.Created,
                                        deadline = action.WF_FlowCases.Deadline,
                                        isFlagged = action.WF_FlowCases.IsFlagged,
                                        isReadByApprover = action.StatusId == 2 ? 1 : 0,
                                        attachments =
                                            action.WF_FlowCases.WF_FlowCases_Attachments.Where(q => q.StatusId > 0)
                                                .Select(q => new
                                                {
                                                    attachmentId = q.AttachementId,
                                                    fileName = q.OriFileName
                                                }),
                                        lastUpdated = action.LastUpdated
                                    })
                                };
                                /*
                                switch (group.Key)
                                {
                                    case "en_US":
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "Inbox",
                                            body = "Your inbox has a new message."
                                        });
                                        break;
                                    case "ko_KR":
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "받은 편지함",
                                            body = "받은 편지함에 새 메시지가 있습니다."
                                        });
                                        break;
                                    case "zh_CN":
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "收信箱",
                                            body = "你的收信箱有新信息."
                                        });
                                        break;
                                    default:
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "收信箱",
                                            body = "你的收信箱有新消息."
                                        });
                                        break;
                                }*/
                                Broker.QueueNotification(notification);
                            }
                        }
                    }
                    foreach (PushTargetData data in _pushdata.Where(p => p.Type == PushType.Notification))
                    {
                        var notificationReceiver =
                            entities.WF_CaseNotificationReceivers.AsNoTracking()
                                .FirstOrDefault(p => p.CaseNotificationReceiverId == data.Id);
                        var receivers =
                            entities.UserDeviceTokens.AsNoTracking().Where(
                                p => data.Users.Contains(p.UserNo.ToLower())  && p.DeviceToken != null && p.StatusId > 0);
                        if (notificationReceiver != null && receivers.Any())
                        {
                            foreach (var group in receivers.GroupBy(p => p.LastDeviceLang))
                            {
                                GcmNotification notification = new GcmNotification
                                {
                                    RegistrationIds = group.Select(p => p.DeviceToken).ToList(),
                                    Data = JObject.FromObject(new
                                    {
                                        type = "Notification",
                                        notificationId = notificationReceiver.CaseNotificationReceiverId,
                                        notificationType =
                                            notificationReceiver.WF_CaseNotifications.NotificationType,
                                        flowCaseId = notificationReceiver.WF_CaseNotifications.FlowCaseId,
                                        department =
                                            notificationReceiver.WF_CaseNotifications.WF_FlowCases.Department,
                                        applicant = entities.GlobalUserView.FirstOrDefault(p=>p.EmployeeID.Equals(notificationReceiver.WF_CaseNotifications.WF_FlowCases.UserNo))?.EmployeeName?? notificationReceiver.WF_CaseNotifications.WF_FlowCases.UserNo,
                                        templateName =
                                            entities.WF_Flows.First(
                                                    p =>
                                                        p.FlowId == notificationReceiver.WF_CaseNotifications.WF_FlowCases.FlowId)
                                                .WF_FlowGroups.WF_FlowTypes.Name,
                                        subject = notificationReceiver.WF_CaseNotifications.WF_FlowCases.Subject,
                                        templateType = entities.WF_Flows.First(
                                                               p =>
                                                                   p.FlowId == notificationReceiver.WF_CaseNotifications.WF_FlowCases.FlowId)
                                                           .WF_FlowGroups.WF_FlowTypes.TemplateType ?? 0,
                                        ver = notificationReceiver.WF_CaseNotifications.WF_FlowCases.Ver ?? 0,
                                        received = notificationReceiver.WF_CaseNotifications.WF_FlowCases.Created,
                                        deadline = notificationReceiver.WF_CaseNotifications.WF_FlowCases.Deadline,
                                        sourceType = notificationReceiver.SourceType,
                                        sender = notificationReceiver.WF_CaseNotifications.Sender,
                                        attachments =
                                            entities.WF_FlowCases_Attachments.Where(
                                                    q =>
                                                        q.StatusId > 0 &&
                                                        q.FlowCaseId ==
                                                        notificationReceiver.WF_CaseNotifications.FlowCaseId)
                                                .Select(q => new
                                                {
                                                    attachmentId = q.AttachementId,
                                                    fileName = q.OriFileName
                                                })
                                    })
                                };
                                /*
                                switch (group.Key)
                                {
                                    case "en_US":
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "Notification",
                                            body = "Your Notification has a new message."
                                        });
                                        break;
                                    case "ko_KR":
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "공고",
                                            body = "알림에 새 메시지가 있습니다."
                                        });
                                        break;
                                    case "zh_CN":
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "通知",
                                            body = "你的通知中有新信息."
                                        });
                                        break;
                                    default:
                                        notification.Notification = JObject.FromObject(new
                                        {
                                            title = "通知",
                                            body = "你的通知中有新消息."
                                        });
                                        break;
                                }
                                */
                                Broker.QueueNotification(notification);
                            }
                        }
                    }
                }
                Broker.Stop();
            });
        }
    }
}
