using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitPushFilter
{
    class ForcePushRule : Rule
    {
        public ForcePushRule()
        {
            this.Groups = new List<string>();
        }
        // allowed users
        public List<string> Groups { get; private set; }

        public override Validation CheckRule(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository)
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
                foreach (var groupName in this.Groups)
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

        protected bool RequiresForcePush(PushNotification pushNotification, TeamFoundationRequestContext requestContext, TfsGitRepository repository)
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

        protected class CommitHashEqualityComparer : IEqualityComparer<TfsGitCommit>
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

        protected bool DescendsFrom(TfsGitCommit commit, byte[] ancestorCommitId, TeamFoundationRequestContext requestContext)
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

        protected bool IsNullHash(byte[] buffer)
        {
            // all bytes to 0, used by TFS to mark dummy
            return buffer.All(b => b == 0);
        }
    }
}
