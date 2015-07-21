A basic TFS Server Plugin that filters Git Push operations.
Its purpose is to implement enterprise policies on Git repositories.

# Design
A **policy** controls behavior for one or more repositories (you can use the `*` wildcard); a policy
checks one or more **rules** that must be satisfied for the requested Git push operation to be accepted.
The rules can be:
 - *read-only* to refuse any change to the repository
 - *Limit size* to avoid accepting big files in the repository
 - *Force Push* to restrict Force Push (i.e. history rewriting) to a group of users, no matters the permission on the Repository
 - *email* to constraint the accepted email addresses for Authors and Contributors

# Configuration

The plugin is driven by the `GitPushFilter.policies` configuration file, which must be present
in the same folder, e.g. `%Program Files%\Microsoft Team Foundation Server 12.0\Application Tier\Web Services\bin\Plugins`

```
<GitPushFilterConfiguration
    LogFile="optional-path-to-diagnostic-log-file">
  <Policy
      Collection="name-of-tfs-collection-or-wildcard"
      Project="name-of-tfs-project-or-wildcard"
      Repository="name-of-tfs-git-repository-or-wildcard">
    <!-- rules -->
    <ReadOnly />
    <LimitSize megabytes="1">
      <!-- optional exempted groups -->
      <Allowed Group="windows-or-tfs-group"/>
      <!-- add more allowed groups -->
    </LimitSize>
    <ForcePush>
      <!-- optional exempted groups -->
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

| Parameter          | Mandatory | Repeatable | Description                                             |
|--------------------|-----------|------------|---------------------------------------------------------|
| _LogFile_          | No  | No  | Full path to diagnostic log file                                     |
| **Policy**         | Yes | No  | Policy to apply to one or more repositories                          |
| _Collection_       | Yes | No  | TeamProjectCollection scope, use `*` for all Collections             |
| _Project_          | Yes | No  | TeamProject scope, use `*` for all Projects in Collection            |
| _Repository_       | Yes | No  | Git Repository scope, use `*` for all Repositories in Project        |
| **LimitSize**      | No  | No  | Rule to control the size of files in a push (not a single commit)    |
| **ForcePush**      | No  | No  | Rule to control history rewrite, i.e. *force push*                   |
| **Allowed**        | Yes | Yes | The `Group` attribute specify the Windows or TFS group               |
| **ValidEmails**    | No  | No  | Rule to control commit emails                                        |
| **AuthorEmail**    | Yes | Yes | The `matches` attribute specify a regular expression to be satisfied by Author email |
| **CommitterEmail** | Yes | Yes | The `matches` attribute specify a regular expression to be satisfied by Committer email  |


# Build
To compile the project, you must copy in the `References` folder, the proper version of the following TFS assemblies:

Microsoft.TeamFoundation.Client
Microsoft.TeamFoundation.Common
Microsoft.TeamFoundation.Framework.Server
Microsoft.TeamFoundation.Git.Server
Microsoft.TeamFoundation.Server.Core
Microsoft.VisualStudio.Services.WebApi

Built and tested with Visual Studio 2013 Update 4 and TFS 2013 Update 4.