using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;

namespace GitPushFilter
{
    internal abstract class Rule
    {
        public abstract Validation CheckRule(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository);
    }
}