# make sure these path are correct for your environment
$TfsFolder = "$env:ProgramFiles\Microsoft Team Foundation Server 12.0"
$PluginsFolder = "$TfsFolder\Application Tier\Web Services\bin\Plugins"

# create EventLog source for Git Filter
New-EventLog -LogName "Application" -Source "GitPushFilter" -ErrorAction SilentlyContinue

# install the plug-in
Copy-Item .\GitPushFilter.* $PluginsFolder
