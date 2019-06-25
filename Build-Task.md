Using Azure DevOps WikiPDFExport as build task is straightforward.

1. Create a new build definition
1. Git Source is "Other Git"
1. 1. Add the clone url to the wiki to the details and username / password if required
1. Ensure that the agent is a windows agent
1. Add a powershell task with the following code:

```
#Download url to the export tool
$url = "https://dev.azure.com/mmelcher/8036eca1-fd9e-4c0f-8bef-646b32fbda0b/_apis/git/repositories/e08d1ada-7794-4b89-a3ea-cb64a26683c3/Items?path=%2Fazuredevops-export-wiki.exe&versionDescriptor%5BversionOptions%5D=0&versionDescriptor%5BversionType%5D=0&versionDescriptor%5Bversion%5D=master&download=true&resolveLfs=true&%24format=octetStream&api-version=5.0-preview.1"

#filename of the tool
$output = "azuredevops-export-wiki.exe"

#download the file
Invoke-WebRequest -Uri $url -OutFile $output

#launch the tool - adjust the parameters if required
./azuredevops-export-wiki.exe
```

5. Add a second task to publish the PDF as build artifact.

Once the build succeeds, you can download the PDF file from the build page or use it in a release.

## Pictures

### Windows Agent
![Windows Agent](../images/WindowsAgent.png) 

### PowerShell Task
![PowerShell Task](../images/PowershellTask.png)

### Publish Task
![Publish Task](../images/PublishTask.png)