using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace GitPushFilter
{
    static class UserAlerter
    {
        internal static void InformUserOfFailure(string email
            , TeamFoundationRequestContext requestContext, PushNotification pushNotification
            , List<Validation> validationResults)
        {
            var buf = new StringBuilder();
            buf.AppendFormat("{0}'s {1} request was refused for the following reasons:"
                , requestContext.GetNameToDisplay()
                , requestContext.ServiceName
                );
            buf.AppendLine();
            buf.AppendLine();
            foreach (var res in validationResults.Where(res => res.Fails))
            {
                buf.Append(" - ");
                buf.AppendLine(res.ReasonMessage);
            }
            buf.AppendLine();

            buf.AppendLine("Additional information:");
            buf.Append(requestContext.GetSummary());
            buf.AppendLine();

            var message = new MailMessage();
            message.From = new MailAddress(PluginConfiguration.Instance.EmailSender);
            message.To.Add(new MailAddress(email));
            message.Subject = "Push refused";
            message.Body = buf.ToString();
            var smtp = new SmtpClient(PluginConfiguration.Instance.EmailSmtpServer);
            smtp.Send(message);
        }
    }
}
