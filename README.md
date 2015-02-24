A basic TFS Server Plugin that filters Git Push operations.
Its purpose is to implement enterprise policies on Git repositories.

# Design
A **policy** controls behavior for one or more repositories (you can use the `*` wildcard); a policy
checks one or more **rules** that must be satisfied for the requested Git push operation to be accepted.
The rules can be:
 - *Force Push* to restrict Force Push (i.e. history rewriting) to a group of users, no matters the permission on the Repository
 - *email* to constraint the accepted email addresses for Authors and Contributors

# Configuration

The plugin is driven by the `GitPushFilter.config` configuration file, which must be present
in the same folder, e.g. `%Program Files%\Microsoft Team Foundation Server 12.0\Application Tier\Web Services\bin\Plugins`

```
<GitPushFilterConfiguration
    TfsBaseUrl="http://localhost:8080"
    LogFile="optional-path-to-diagnostic-log-file">
  <Policy
      Collection="name-of-tfs-collection-or-wildcard"
      Project="name-of-tfs-project-or-wildcard"
      Repository="name-of-tfs-git-repository-or-wildcard">
    <!-- rules -->
    <ForcePush>
      <Allowed Group="windows-or-tfs-group"/>
      <!-- add more allowed groups -->
    </ForcePush>
    <ValidEmails>
      <AuthorEmail matches="regular-expression" />
      <CommitterEmail matches="regular-expression" />
      <!-- add more allowed emails -->
    </ValidEmails>
  </Policy>
  <!-- add more policies for additional projects and repos -->
</GitPushFilterConfiguration>
```

| Parameter          | Mandatory | Repeat | Description                                             |
|--------------------|-----------|--------|---------------------------------------------------------|
| _TfsBaseUrl_       | Yes | no  | Base URL for TFS e.g. `http://localhost:8080`                    |
| _LogFile_          | No  | no  | Full path to diagnostic log file                                 |
| **Policy**         | Yes | no  | Policy to apply to one or more repositories                      |
| _Collection_       | Yes | no  | TeamProjectCollection scope, use `*` for all Collections         |
| _Project_          | Yes | no  | TeamProject scope, use `*` for all Projects in Collection        |
| _Repository_       | Yes | no  | Git Repository scope, use `*` for all Repositories in Project    |
| **ForcePush**      | No  | no  | Rule to control history rewrite, i.e. *force push*               |
| **Allowed**        | Yes | yes | The `Group` attribute specify the Windows or TFS group           |
| **ValidEmails**    | No  | no  | Rule to control commit emails                                    |
| **AuthorEmail**    | Yes | yes | The `matches` attribute specify a regular expression to be satisfied by Author email |
| **CommitterEmail** | Yes | yes | The `matches` attribute specify a regular expression to be satisfied by Committer email  |


# Build
To compile the project, you must copy in the `References` folder, the proper version of the following TFS assemblies:

Microsoft.TeamFoundation.Client
Microsoft.TeamFoundation.Common
Microsoft.TeamFoundation.Framework.Server
Microsoft.TeamFoundation.Git.Server
Microsoft.TeamFoundation.Server.Core
Microsoft.VisualStudio.Services.WebApi

Built and tested with Visual Studio 2013 Update 4 and TFS 2013 Update 4.