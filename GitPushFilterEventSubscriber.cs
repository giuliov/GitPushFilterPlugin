using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Integration.Server;
using Microsoft.TeamFoundation.Server.Core;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using System.Collections;

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

                            var validationResult = ApplyPolicy(policy, requestContext, pushNotification, repository);
                            if (validationResult.Fails)
                            {
                                Logger.Log("Request DENIED!");
                                statusCode = 1;
                                statusMessage = "Force Push is restricted to TFS Admin";
                                return EventNotificationStatus.ActionDenied;
                            }
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
                if (collectionName.SameAs(policy.CollectionName)
                    && projectName.SameAs(policy.ProjectName)
                    && repositoryName.SameAs(policy.RepositoryName))
                    return policy;
            }//for

            return null;
        }

        private Validation ApplyPolicy(Policy policy, TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository)
        {
            var result = new Validation();

            bool force = RequiresForcePush(pushNotification, requestContext, repository);
            if (force)
            {
                // HACK
                string collectionUrl = PluginConfiguration.Instance.TfsBaseUrl + requestContext.ServiceHost.VirtualDirectory;
                // client API
                TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(collectionUrl));
                var identityManagement = tfs.GetService<IIdentityManagementService>();
                var requestIdentity = identityManagement.ReadIdentity(IdentitySearchFactor.AccountName, pushNotification.AuthenticatedUserName, MembershipQuery.Direct, ReadIdentityOptions.None);

                bool allowed = false;
                foreach (var groupName in policy.Groups)
                {
                    var groupIdentity = identityManagement.ReadIdentity(IdentitySearchFactor.AccountName, groupName, MembershipQuery.Direct, ReadIdentityOptions.None);
                    allowed |= identityManagement.IsMember(groupIdentity.Descriptor, requestIdentity.Descriptor);
                }//for

                if (allowed)
                {
                    Logger.Log(string.Format(
                        "User {0} has explicit permission for {1}"
                        , pushNotification.AuthenticatedUserName
                        , "Forced Push"));
                }
                else
                {
                    result.Fails = true;
                    result.ReasonCode = 1;
                    result.ReasonMessage = "Force Push policy fails for user " + pushNotification.AuthenticatedUserName;
                }//if
            }//if

            return result;
        }

        public bool RequiresForcePush(PushNotification pushNotification, TeamFoundationRequestContext requestContext, TfsGitRepository repository)
        {
            foreach (var refUpdateResult in pushNotification.RefUpdateResults)
            {
                // new or deleted refs have id==0
                if (IsNullHash(refUpdateResult.OldObjectId)
                    || IsNullHash(refUpdateResult.NewObjectId))
                    continue;

                TfsGitCommit gitCommit = repository.LookupObject(requestContext, refUpdateResult.NewObjectId) as TfsGitCommit;
                if (gitCommit == null)
                    continue;

                if (!DescendsFrom(gitCommit, refUpdateResult.OldObjectId, requestContext))
                    // aha! no common node
                    return true;
            }
            return false;
        }

        class CommitHashEqualityComparer : IEqualityComparer<TfsGitCommit>
        {
            public bool Equals(TfsGitCommit a, TfsGitCommit b)
            {
                return a.ObjectId.SequenceEqual(b.ObjectId);
            }

            public int GetHashCode(TfsGitCommit x)
            {
                return BitConverter.ToString(x.ObjectId).GetHashCode();
            }
        }

        public bool DescendsFrom(TfsGitCommit commit, byte[] ancestorCommitId, TeamFoundationRequestContext requestContext)
        {
            var comparer = new CommitHashEqualityComparer();

            // classic breadth first search in graph
            var queue = new List<TfsGitCommit> { commit };
            var visited = new HashSet<TfsGitCommit>(comparer);
            visited.Add(commit);
            while (queue.Any())
            {
                // Get current node, and remove it from the to-do-list
                var currentCommit = queue.First();
                queue.RemoveAt(0);

                // ancestor found?
                if (currentCommit.ObjectId.SequenceEqual(ancestorCommitId))
                    return true;

                // Now see, if the node has any parents to handle
                foreach (var parent in currentCommit.GetParents(requestContext))
                {
                    if (!visited.Contains(parent))
                    {
                        queue.Add(parent);
                        visited.Add(parent);
                    }//if
                }//for
            }//while
            return false;
        }

        public bool IsNullHash(byte[] buffer)
        {
            // all bytes to 0, used by TFS to mark dummy
            return buffer.All(b => b == 0);
        }
    }
}
