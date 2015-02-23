using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitPushFilter
{
    abstract class Rule
    {
        public abstract Validation CheckRule(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository);
    }
}
