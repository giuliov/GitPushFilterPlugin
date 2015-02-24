using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace GitPushFilter
{
    internal class PluginConfiguration
    {
        private static PluginConfiguration _instance = null;

        static internal PluginConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    string baseName = Assembly.GetExecutingAssembly().GetName().Name;
                    string step = "opening file";

                    try
                    {
                        var instance = new PluginConfiguration();

                        // Load the options from same folder
                        string xmlFileName = Path.ChangeExtension(
                            Path.Combine(
                                Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase),
                                baseName),
                            ".config");
                        XDocument doc = XDocument.Load(xmlFileName);

                        step = "root attributes";

                        // global settings
                        instance.TfsBaseUrl = doc.Root.Attribute("TfsBaseUrl").Value;
                        instance.LogFile = doc.Root.Attribute("LogFile").Value;

                        step = "Policy element";
                        // policies
                        foreach (var policyElem in doc.Root.Elements("Policy"))
                        {
                            step = "Policy attributes";
                            var policy = new Policy()
                            {
                                CollectionName = policyElem.Attribute("Collection").Value,
                                ProjectName = policyElem.Attribute("Project").Value,
                                RepositoryName = policyElem.Attribute("Repository").Value,
                            };

                            step = "ForcePush element";
                            if (policyElem.Elements("ForcePush").Any())
                            {
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

                                    policy.Rules.Add(rule);
                                }//for
                            }//if

                            step = "ValidEmails element";
                            if (policyElem.Elements("ValidEmails").Any())
                            {
                                foreach (var auth in policyElem.Element("ValidEmails").Elements())
                                {
                                    var rule = new ValidEmailsRule();

                                    switch (auth.Name.LocalName)
                                    {
                                        case "AuthorEmail":
                                            rule.AuthorEmail.Add(auth.Attribute("matches").Value);
                                            break;

                                        case "CommitterEmail":
                                            rule.CommitterEmail.Add(auth.Attribute("matches").Value);
                                            break;

                                        default:
                                            break;
                                    }//switch

                                    policy.Rules.Add(rule);
                                }//for
                            }//if

                            instance.Policies.Add(policy);
                        }
                        // publish
                        System.Threading.Interlocked.Exchange(ref _instance, instance);
                    }
                    catch (System.Exception)
                    {
                        System.Diagnostics.EventLog.WriteEntry("TFS Services",
                            string.Format(
                                "{0} configuration file error in {1}",
                                baseName, step),
                            System.Diagnostics.EventLogEntryType.Error);
                        throw;
                    }//try
                }
                return _instance;
            }
        }

        public PluginConfiguration()
        {
            this.Policies = new List<Policy>();
        }

        public string TfsBaseUrl { get; private set; }

        public bool HasLog { get { return !string.IsNullOrWhiteSpace(this.LogFile); } }
        public string LogFile { get; private set; }

        public List<Policy> Policies { get; private set; }
    }
}