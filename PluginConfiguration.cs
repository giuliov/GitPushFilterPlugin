using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GitPushFilter
{
    class PluginConfiguration
    {
        static PluginConfiguration _instance = null;

        static internal PluginConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    var instance = new PluginConfiguration();

                    // Load the options from same folder
                    string xmlFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase), "GitPushFilter.config");
                    XDocument doc = XDocument.Load(xmlFileName);

                    // global settings
                    instance.TfsBaseUrl = doc.Root.Attribute("TfsBaseUrl").Value;
                    instance.LogFile = doc.Root.Attribute("LogFile").Value;

                    // policies
                    foreach (var policyElem in doc.Root.Elements("Policy"))
                    {
                        var policy = new Policy()
                        {
                            CollectionName = policyElem.Attribute("Collection").Value,
                            ProjectName = policyElem.Attribute("Project").Value,
                            RepositoryName = policyElem.Attribute("Repository").Value,
                        };

                        foreach (var auth in policyElem.Element("ForcePush").Elements())
                        {
                            var rule = new ForcePushRule();

                            switch (auth.Name.LocalName)
                            {
                                case "Allowed":
                                    rule.Groups.Add(auth.Attribute("Group").Value.ToLowerInvariant());
                                    break;
                                default:
                                    break;
                            }//switch

                        }//for

                        instance.Policies.Add(policy);
                    }

                    // publish
                    System.Threading.Interlocked.Exchange(ref _instance, instance);
                }
                return _instance;
            }
        }

        public PluginConfiguration()
        {
            this.Policies = new List<Policy>();
        }

        public string TfsBaseUrl { get; private set; }
        public string LogFile { get; private set; }
        public List<Policy> Policies { get; private set; }

    }
}
