using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Omnibackend.Workflow
{
    public static class MailService
    {
        private static string Host { get; set; }
        private static string Sender { get; set; }
        private static string Username { get; set; }
        private static string Password { get; set; }
        private static SmtpClient Client { get; set; }

        static MailService()
        {
            Host = "mail.blsretail.com";
            Sender = "BLS Workflow<app@blsretail.com>";
            Username = "app@blsretail.com";
            Password = "blsrt8888";
            Client = new SmtpClient
            {
                Host = Host,
                Port = 25
            };
        }

        public static void Send(string receiver, string subject, string content)
        {
            try
            {
                MailAddress sendAddress = new MailAddress(Sender);
                MailAddress receiveAddress = new MailAddress(receiver);
                MailMessage mailMessage = new MailMessage(sendAddress, receiveAddress)
                {
                    Subject = subject,
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    Body = content,
                    BodyEncoding = System.Text.Encoding.UTF8
                };
                Client.DeliveryMethod = SmtpDeliveryMethod.Network;
                Client.EnableSsl = false;
                Client.UseDefaultCredentials = false;
                NetworkCredential networkCredential = new NetworkCredential(Username, Password);
                Client.Credentials = networkCredential;
                Client.Send(mailMessage);
            }
            catch (Exception e)
            {
                
            }
        }
    }
}
