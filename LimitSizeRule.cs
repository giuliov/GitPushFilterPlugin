using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitPushFilter
{
    internal class LimitSizeRule : ExemptGroupsRuleBase
    {
        public LimitSizeRule()
        {
        }

        public int Megabytes { get; set; }

        public override Validation CheckRule(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository)
        {
            var result = new Validation();

            long totalSize = 0;
            foreach (var commitId in pushNotification.IncludedCommits)
            {
                TfsGitCommit gitCommit = repository.LookupObject(requestContext, commitId) as TfsGitCommit;
                if (gitCommit == null)
                    continue;

                long size = GetCommitSize(gitCommit, requestContext);
                totalSize += size;
            }//for

            // convert to MB
            totalSize = totalSize / (1024 * 1024);

            if (totalSize < this.Megabytes)
            {
                Logger.Log(string.Format(
                    "Push request is {0} MB, below the {1} MB limit."
                    , totalSize, this.Megabytes
                    , "Limit Size"));
            }
            else
            {
                if (IsUserExempted(requestContext, pushNotification))
                {
                    Logger.Log(string.Format(
                        "Push request is {0} MB, above or equal to the {1} MB limit, but user is exempted."
                        , totalSize, this.Megabytes
                        , "Limit Size"));
                }
                else
                {
                    result.Fails = true;
                    result.ReasonCode = 2;
                    result.ReasonMessage = string.Format(
                        "Push request is {0} MB, above or equal to the {1} MB limit: refused."
                        , totalSize, this.Megabytes);
                }
            }//if

            return result;
        }

        long GetCommitSize(TfsGitCommit gitCommit, TeamFoundationRequestContext requestContext)
        {
            var tree = gitCommit.GetTree(requestContext);
            if (tree == null)
                return 0;

            long totalSize = tree.GetBlobs(requestContext).Aggregate(
                0L,
                (size, blob) => size + blob.GetLength(requestContext));
            return totalSize;
        }
    }
}