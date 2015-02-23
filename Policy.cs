using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Git.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitPushFilter
{
    class Policy
    {
        public Policy()
        {
            this.Rules = new List<Rule>();
        }

        public string CollectionName { get; set; }
        public string ProjectName { get; set; }
        public string RepositoryName { get; set; }

        public List<Rule> Rules { get; private set; }

        public bool AppliesTo(string collectionName, string projectName, string repositoryName)
        {
            // exact match
            return (collectionName.SameAs(this.CollectionName)
                    && projectName.SameAs(this.ProjectName)
                    && repositoryName.SameAs(this.RepositoryName));
        }

        public List<Validation> CheckRules(TeamFoundationRequestContext requestContext, PushNotification pushNotification, TfsGitRepository repository)
        {
            var res = new List<Validation>();
            foreach (var rule in this.Rules)
            {
                res.Add(rule.CheckRule(requestContext, pushNotification, repository));
            }//for
            return res;
        }
    }
}
