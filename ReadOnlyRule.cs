using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitPushFilter
{
    internal class ReadOnlyRule : Rule
    {
        public override Validation CheckRule(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository)
        {
            var result = new Validation();

            // refuse any push
            result.Fails = true;
            result.ReasonCode = 99;
            result.ReasonMessage = string.Format(
                "Repository '{0}' is in read-only mode",
                pushNotification.RepositoryName);

            return result;
        }
    }
}
