using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitPushFilter
{
    /// <summary>
    /// NOT PRODUCTION READY!
    /// </summary>
    static class Logger
    {
        internal static void Log(string message)
        {
            File.AppendAllText(PluginConfiguration.Instance.LogFile, message + Environment.NewLine);
        }

        internal static void LogException(Exception ex)
        {
            var lines = new List<string>();

            lines.Add("Exception: " + ex.Message);
            lines.Add(ex.StackTrace);

            File.AppendAllLines(PluginConfiguration.Instance.LogFile, lines);
        }

        internal static void LogRequest(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository)
        {
            var lines = new List<string>();

            lines.Add(string.Format("Request from {0} for {1}"
                , requestContext.GetNameToDisplay()
                , requestContext.ServiceName
                ));
            lines.Add("Summary for " + requestContext.GetSummary());

            lines.Add(string.Format("{6} #{0} on {1} repo {2} at {3} by {5} ({4})"
                , pushNotification.PushId
                , pushNotification.TeamProjectUri
                , pushNotification.RepositoryName
                , pushNotification.PushTime
                , pushNotification.AuthenticatedUserName
                , pushNotification.Pusher
                , requestContext.Method.Name
                ));

            lines.Add("- Included Commits:");
            foreach (var commitHash in pushNotification.IncludedCommits)
            {
                var commit = repository.TryLookupObject(requestContext, commitHash) as TfsGitCommit;
                lines.Add(string.Format("   Commit {0}: {1} '{2}'"
                    , commit.ObjectId.DisplayHash()
                    , commit.GetCommitterName(requestContext)
                    , commit.GetComment(requestContext)
                    ));

                foreach (var parentCommit in commit.GetParents(requestContext))
                {
                    lines.Add(string.Format("      Parent {0}: {1} '{2}'"
                        , parentCommit.ObjectId.DisplayHash()
                        , parentCommit.GetCommitterName(requestContext)
                        , parentCommit.GetComment(requestContext)
                        ));
                }
            }

            lines.Add("- Ref Update Results:");
            foreach (var refUpdate in pushNotification.RefUpdateResults)
            {
                lines.Add(string.Format("   on {0} {1}..{2} is {3} (succeeded: {4}) rejecter '{5}' message '{6}'"
                    , refUpdate.Name
                    , refUpdate.NewObjectId.DisplayHash()
                    , refUpdate.OldObjectId.DisplayHash()
                    , refUpdate.Status
                    , refUpdate.Succeeded
                    , refUpdate.RejectedBy
                    , refUpdate.CustomMessage
                    ));
            }//for

            File.AppendAllLines(PluginConfiguration.Instance.LogFile, lines);
        }
    }
}
