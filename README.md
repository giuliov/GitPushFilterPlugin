A basic TFS Server Plugin that filters Git Push operations.

# Configuration

```
<GitPushFilterConfiguration
    TfsBaseUrl="http://localhost:8080"
    LogFile="path-to-log-file">
  <Policy
      Collection="name-of-tfs-collection"
      Project="name-of-tfs-project"
      Repository="name-of-tfs-git-repository">
    <ForcePush>
      <Allowed Group="windows-or-tfs-group"/>
      <!-- add more allowed groups -->
    </ForcePush>
  </Policy>
  <!-- add more policies for additional projects and repos -->
</GitPushFilterConfiguration>
```

# Build
To compile the project, you must copy in the `References` folder, the proper version of the following TFS assemblies:

Microsoft.TeamFoundation.Common
Microsoft.TeamFoundation.Framework.Server
Microsoft.TeamFoundation.Git.Server
Microsoft.TeamFoundation.Server.Core
Microsoft.VisualStudio.Services.WebApi

Build with Visual Studio 2013 Update 4 for TFS 2013 Update 4.