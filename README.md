# VSTSBuildDefinitionMigrator
Tool for moving VSTS build definitions from one repository to another.

## Requirements

### User Permissions
* Ability to **read** build definitions on **source**
* Ability to **read** project on **destination**
* Ability to **read** agent queues on **destination**
* Ability to **write** build definitions on **destination**

### VSTS Accounts
* Must have valid repository on **destination**
* Must have similar queues on **destination**
* Must have all extensions installed on **destination**

## Example
```
MigrateBuildDefinitions.exe /su https://SOURCEACCOUNT.visualstudio.com/ /sp "TFVC Repo 1" /sr "$/TFVC Repo
1" /du https://DESTINATIONACCOUNT.visualstudio.com/ /dp TFVCDestination /dr $/TFVCDestination
```
## Usage Command Line

Enter your source and destination values using the command line **OR** run it without any options and use the interactive mode.

```
MigrateBuildDefinitions.exe [/h] [/su https://x.y.z] [/sp SourceProject] [/sr $/SourceRepo] [/du https://x.y.z] [/dp DestinationProject] [/dr $/DestinationRepo]
  -?, --help, -h             Prints out the options.
      --su, --source-url=VALUE
                             Set the source VSTS account URL (https://[sourcerepo].visualstudio.com)
      --sp, --source-project=VALUE
                             Set the source VSTS project
      --sr, --source-repository=VALUE
                             Set the source VSTS repository path. (ex: $/SourceRepo)
      --du, --destination-url=VALUE
                             Set the destination VSTS account URL (https://[destrepo].visualstudio.com)
      --dp, --destination-project=VALUE
                             Set the destination VSTS project
      --dr, --destination-repository=VALUE
                             Set the destination VSTS repository path. (ex: $/DestinationRepo)
```

## Usage Interactive

Follow the prompts on screen. You will be prompted to enter your user name and password. The prompts aren't labled but will happen in sequential order--ie. SOURCE then DESTINATION.

```
D:\migrate> MigrateBuildDefinitions.exe
Enter the source account url (https://<<account>>.visualstudio.com/): https://SOURCEACCOUNT.visualstudio.com/
Enter the source project: TFVC Repo 1
Enter the source repository ($/MYSOURCEREPO): $/TFVC Repo1
Enter the destination account url (https://<<account>>.visualstudio.com/): https://DESTINATIONACCOUNT.visualstudio.com/
Enter the destination project: TFVCDestination
Enter the source repository ($/MYDESTREPO): $/TFVCDestination
Creating VSTS connections and clients.

Authorized acccount https://SOURCEACCOUNT.visualstudio.com/ as Original Owner <owner1@outlook.com>
Authorized acccount https://DESTINATIONACCOUNT.visualstudio.com/ as Writer Account <writer.account@outlook.com>

Creating source build client.
Creating destination build client.
Creating destination project client.
Creating destination task agent client.
Creating destination TFVC client.

Adding build definition: TFVC Repo 1-Maven-CI

Appication completed succesfully.
```
