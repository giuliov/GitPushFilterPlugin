using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitPushFilter
{
    internal class ValidEmailsRule : Rule
    {
        public ValidEmailsRule()
        {
            this.AuthorEmail = new List<string>();
            this.CommitterEmail = new List<string>();
        }

        // allowed emails
        public List<string> AuthorEmail { get; private set; }

        public List<string> CommitterEmail { get; private set; }

        public override Validation CheckRule(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository)
        {
            var result = new Validation();

            foreach (var refUpdateResult in pushNotification.RefUpdateResults)
            {
                // new or deleted refs have id==0
                if (IsNullHash(refUpdateResult.OldObjectId)
                    || IsNullHash(refUpdateResult.NewObjectId))
                    continue;

                TfsGitCommit gitCommit = repository.LookupObject(requestContext, refUpdateResult.NewObjectId) as TfsGitCommit;
                if (gitCommit == null)
                    continue;

                string authorEmail = gitCommit.GetAuthorEmail(requestContext);
                if (!AuthorEmail.Any(pattern => Regex.IsMatch(authorEmail, pattern)))
                {
                    result.Fails = true;
                    result.ReasonCode = 2;
                    result.ReasonMessage = string.Format(
                        "Author email '{0}' on commit {1} is not admitted",
                        authorEmail,
                        gitCommit.ObjectId.DisplayHash());
                    break;
                }

                string committerEmail = gitCommit.GetCommitterEmail(requestContext);
                if (!CommitterEmail.Any(pattern => Regex.IsMatch(committerEmail, pattern)))
                {
                    result.Fails = true;
                    result.ReasonCode = 3;
                    result.ReasonMessage = string.Format(
                        "Committer email '{0}' on commit {1} is not admitted",
                        authorEmail,
                        gitCommit.ObjectId.DisplayHash());
                    break;
                }//if
            }//for changes

            return result;
        }

        protected bool IsNullHash(byte[] buffer)
        {
            // all bytes to 0, used by TFS to mark dummy
            return buffer.All(b => b == 0);
        }
    }
}