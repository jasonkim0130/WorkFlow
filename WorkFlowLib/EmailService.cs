
using Omnibackend.Workflow;
using System.Configuration;
using System.Linq;
using WorkFlowLib;

namespace WorkFlowLib
{
    public static class EmailService
    {
        public static void SendWorkFlowEmail(string sender, string[] receivers, string subject, NotificationTypes? notificationType)
        {
            LoginApiClient login = new LoginApiClient();
            using (login.Wrapper)
            {
                string country = ConfigurationManager.AppSettings["Country"];
                string[] emails = receivers.Select(p => login.UserEmail(p, country).ReturnValue).Where(p => p != null).ToArray();
                string content = string.Empty;
                if (notificationType == null)
                {
                    content = $"New inbox from {sender}, Subject - {subject}";
                }
                else
                {
                    switch (notificationType)
                    {
                        case NotificationTypes.AppFinishedApproved:
                            content =
                                $"Application {subject} is Fully Approved";
                            break;
                        case NotificationTypes.AppFinishedRejected:
                            content =
                                $"Application {subject} is rejected";
                            break;
                        case NotificationTypes.AppFinishedAbort:
                            content =
                                $"Application {subject} is sent back to revise.";
                            break;
                        case NotificationTypes.CancelApp:
                            content =
                                $"{sender} cancelled the application {subject}";
                            break;
                        case NotificationTypes.ApproveApp:
                            content =
                                $"{sender} approved the application {subject}";
                            break;
                        case NotificationTypes.Comments:
                            content =
                                $"{sender} commentted the application {subject}";
                            break;
                        case NotificationTypes.RejectApp:
                            content =
                                $"{sender} reject the application {subject}";
                            break;
                        case NotificationTypes.CoverUserMessage:
                            content =
                                $"Application {subject} is finished. You are the person to cover his(her) duties.";
                            break;
                        case NotificationTypes.FinalMessage:
                            content =
                                $"Application {subject} is finished. you are the final notify person.";
                            break;
                        case NotificationTypes.SecretaryNotification:
                            content =
                                $"{sender} approved the application {subject}. You are the secretary of approver.";
                            break;
                    }
                }
                foreach (var e in emails)
                {
                    MailService.Send(e, subject, content);
                }
            }
        }
    }
}
