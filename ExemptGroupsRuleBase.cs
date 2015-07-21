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
    abstract class ExemptGroupsRuleBase : Rule
    {
        public ExemptGroupsRuleBase()
        {
            this.Groups = new List<string>();
        }

        // allowed users
        public List<string> Groups { get; private set; }

        protected bool IsUserExempted(TeamFoundationRequestContext requestContext, PushNotification pushNotification)
        {
            string collectionUrl = requestContext.GetCollectionUri();
            // client API
            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(collectionUrl));
            var identityManagement = tfs.GetService<IIdentityManagementService>();
            var requestIdentity = identityManagement.ReadIdentity(IdentitySearchFactor.AccountName, pushNotification.AuthenticatedUserName, MembershipQuery.Direct, ReadIdentityOptions.None);

            bool exempted = false;
            foreach (var groupName in this.Groups)
            {
                var groupIdentity = identityManagement.ReadIdentity(IdentitySearchFactor.AccountName, groupName, MembershipQuery.Direct, ReadIdentityOptions.None);
                exempted |= identityManagement.IsMember(groupIdentity.Descriptor, requestIdentity.Descriptor);
            }//for
            return exempted;
        }
    }
}
