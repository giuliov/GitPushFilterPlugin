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
                        Logger.Log("Request controlled by policy");

                        var gitRepoService = requestContext.GetService<TeamFoundationGitRepositoryService>();
                        using (var repository = gitRepoService.FindRepositoryById(requestContext, pushNotification.RepositoryId))
                        {
                            Logger.LogRequest(requestContext, pushNotification, repository);

                            var validationResults = policy.CheckRules(requestContext, pushNotification, repository);
                            //TODO accumulate failures
                            var failsAt = validationResults.FirstOrDefault(v => v.Fails);
                            if (failsAt != null)
                            {
                                Logger.Log(string.Format("Request DENIED: {0}", failsAt.ReasonMessage));
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
                throw;
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
    }
}