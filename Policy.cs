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
            this.Groups = new List<string>();
        }

        public string CollectionName { get; set; }
        public string ProjectName { get; set; }
        public string RepositoryName { get; set; }
        // allowed users
        public List<string> Groups { get; private set; }
    }

}
