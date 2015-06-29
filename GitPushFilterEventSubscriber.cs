using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using Microsoft.TeamFoundation.Integration.Server;
using System;
using System.Linq;

namespace GitPushFilter
{
    /// <summary>
    /// TFS Event subscriber: entry point of plugin
    /// </summary>
    /// <remarks>
    /// See http://almsports.net/tfs-server-side-check-in-policy-for-git-repositories/1025/ for an introduction.
    /// </remarks>
    public class GitPushFilterEventSubscriber : ISubscriber
    {
        public string Name
        {
            get { return "Git Push Filtering"; }
        }

        public Type[] SubscribedTypes()
        {
            return new Type[] {
                typeof(PushNotification)
            };
        }

        public SubscriberPriority Priority
        {
            get { return SubscriberPriority.Normal; }
        }

        public EventNotificationStatus ProcessEvent(
            TeamFoundationRequestContext requestContext, NotificationType notificationType,
            object notificationEventArgs,
            out int statusCode, out string statusMessage,
            out Microsoft.TeamFoundation.Common.ExceptionPropertyCollection properties)
        {
            statusCode = 0;
            statusMessage = string.Empty;
            properties = null;

            try
            {
                if (notificationType == NotificationType.DecisionPoint
                    && notificationEventArgs is PushNotification)
                {
                    PushNotification pushNotification = notificationEventArgs as PushNotification;

                    // validation applies?
                    var policy = GetPolicy(requestContext, pushNotification);
                    if (policy != null)
                    {
                        Logger.LogStart("Request controlled by policy");

                        var gitRepoService = requestContext.GetService<TeamFoundationGitRepositoryService>();
                        using (var repository = gitRepoService.FindRepositoryById(requestContext, pushNotification.RepositoryId))
                        {
                            Logger.LogRequest(requestContext, pushNotification, repository);

                            var validationResults = policy.CheckRules(requestContext, pushNotification, repository);
                            //TODO accumulate failures
                            var failsAt = validationResults.FirstOrDefault(v => v.Fails);
                            if (failsAt != null)
                            {
                                if (PluginConfiguration.Instance.ShouldSendEmail)
                                {
                                    try
                                    {
                                        string email = GetEmailAddress(requestContext, pushNotification);
                                        if (string.IsNullOrWhiteSpace(email))
                                            // no email for user -> notify admin
                                            email = PluginConfiguration.Instance.AdministratorEmail;
                                        UserAlerter.InformUserOfFailure(email, requestContext, pushNotification, validationResults);
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Log(string.Format("Error: failed to notify user {0}, reason {1}", pushNotification.AuthenticatedUserName, e.Message));
                                    }//try
                                }//if

                                Logger.LogDenied(failsAt.ReasonMessage);
                                statusCode = failsAt.ReasonCode;
                                statusMessage = failsAt.ReasonMessage;
                                return EventNotificationStatus.ActionDenied;
                            }//if
                        }//using
                    }//if
                }//if
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                throw; // TFS will disable plugin
            }//try

            /*
             * from https://msdn.microsoft.com/en-us/library/Gg214903%28v=vs.120%29.aspx
             * ActionDenied	Action denied; do not notify other subscribers.
             * ActionPermitted	Action permitted; continue with subscriber notification.
             * ActionApproved	Like ActionPermitted, but do not notify other subscribers.
             */
            return EventNotificationStatus.ActionPermitted;
        }

        private Policy GetPolicy(TeamFoundationRequestContext requestContext, PushNotification pushNotification)
        {
            // HACK
            string collectionName = requestContext.ServiceHost.VirtualDirectory.Replace("/tfs/", "").Replace("/", "");
            // HACK is this cheap?
            var commonService = requestContext.GetService<CommonStructureService>();
            string projectName = commonService.GetProject(requestContext, pushNotification.TeamProjectUri).Name;
            string repositoryName = pushNotification.RepositoryName;

            foreach (var policy in PluginConfiguration.Instance.Policies)
            {
                if (policy.AppliesTo(collectionName, projectName, repositoryName))
                    return policy;
            }//for

            return null;
        }

        private string GetEmailAddress(TeamFoundationRequestContext requestContext, PushNotification pushNotification)
        {
            var collectionUrl = new Uri(requestContext.GetCollectionUri());
            var collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(collectionUrl);
            var managementService = collection.GetService<IIdentityManagementService>();
            TeamFoundationIdentity teamFoundationIdentity = managementService.ReadIdentity(
                    IdentitySearchFactor.AccountName,
                    pushNotification.AuthenticatedUserName,
                    MembershipQuery.None,
                    ReadIdentityOptions.None);

            return teamFoundationIdentity.GetAttribute("Mail", null);
        }
    }
}