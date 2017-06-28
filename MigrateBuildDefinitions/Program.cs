using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.HostManagement;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using System.Net.Http;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Mono.Options;

namespace MigrateBuildDefinitions
{
    class Program
    {
        static string sourceUrl = string.Empty;
        static string sourceProject = string.Empty;
        static string sourceRepo = string.Empty;

        static string destinationUrl = string.Empty;
        static string destinationProject = string.Empty;
        static string destinationRepo = string.Empty;

        static bool hasErrors = false;

        static bool help = false;

        static string usage = $"{System.AppDomain.CurrentDomain.FriendlyName} [/h] [/su https://x.y.z] [/sp SourceProject] [/sr $/SourceRepo] [/du https://x.y.z] [/dp DestinationProject] [/dr $/DestinationRepo] ";
        static void Main(string[] args)
        {
            
            OptionSet option_set = new OptionSet()
                .Add("?|help|h", "Prints out the options.", option => { help = option != null; })
                .Add("su|source-url=", "Set the source VSTS account URL (https://[sourcerepo].visualstudio.com)", option => sourceUrl = option)
                .Add("sp|source-project=", "Set the source VSTS project", option => sourceProject = option)
                .Add("sr|source-repository=", "Set the source VSTS repository path. (ex: $/SourceRepo)", option => sourceRepo = option)
                .Add("du|destination-url=", "Set the destination VSTS account URL (https://[destrepo].visualstudio.com)", option => destinationUrl = option)
                .Add("dp|destination-project=", "Set the destination VSTS project", option => destinationProject = option)
                .Add("dr|destination-repository=", "Set the destination VSTS repository path. (ex: $/DestinationRepo)", option => destinationRepo = option);
            try
            {
                option_set.Parse(args);
            }
            catch (Exception ex)
            {
                LogGenericException(ex, "Error on commandline parse");
            }
            if (help)
            {
                ShowHelp(usage, option_set);
            }

            if (string.IsNullOrEmpty(sourceUrl))
            {
                Console.Write("Enter the source account url (https://<<account>>.visualstudio.com/): ");
                sourceUrl = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(sourceProject))
            {
                Console.Write("Enter the source project: ");
                sourceProject = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(sourceRepo))
            {
                Console.Write("Enter the source repository ($/MYSOURCEREPO): ");
                sourceRepo = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(destinationUrl))
            {
                Console.Write("Enter the destination account url (https://<<account>>.visualstudio.com/): ");
                destinationUrl = Console.ReadLine(); 
            }
            if (string.IsNullOrEmpty(destinationProject))
            {
                Console.Write("Enter the destination project: ");
                destinationProject = Console.ReadLine(); 
            }
            if (string.IsNullOrEmpty(destinationRepo))
            {
                Console.Write("Enter the source repository ($/MYDESTREPO): ");
                destinationRepo = Console.ReadLine(); 
            }

            string sourceAccount = $"{sourceUrl}DefaultCollection";
            string destinationAccount = $"{destinationUrl}DefaultCollection";

            // Interactively ask the user for credentials

            VssCredentials sourceCredentials = new VssClientCredentials();
            sourceCredentials.Storage = new VssClientCredentialStorage();

            VssCredentials destinationCredentials = new VssClientCredentials();
            destinationCredentials.Storage = new VssClientCredentialStorage();

            BuildHttpClient sourceBuildClient = null;
            BuildHttpClient destinationBuildClient = null;
            ProjectHttpClient destinationProjectClient = null;
            TaskAgentHttpClient destinationTaskAgentClient = null;
            TfvcHttpClient destinationTfvcClient = null;

            try
            {
                Console.WriteLine($"Creating VSTS connections and clients.");
                Console.WriteLine();
                // Connect to VSTS Source and Destination
                VssConnection sourceConnection = new VssConnection(new Uri(sourceAccount), sourceCredentials);
                Console.WriteLine($"Authorized acccount {sourceUrl} as {sourceConnection.AuthorizedIdentity.DisplayName} <{sourceConnection.AuthorizedIdentity.Descriptor.Identifier}>");
                VssConnection destinationConnection = new VssConnection(new Uri(destinationAccount), destinationCredentials);
                Console.WriteLine($"Authorized acccount {destinationUrl} as {destinationConnection.AuthorizedIdentity.DisplayName} <{sourceConnection.AuthorizedIdentity.Descriptor.Identifier}>");
                Console.WriteLine();
                // Get a number of HttpClients to read and write data
                Console.WriteLine("Creating source build client.");
                sourceBuildClient = sourceConnection.GetClient<BuildHttpClient>();
                Console.WriteLine("Creating destination build client.");
                destinationBuildClient = destinationConnection.GetClient<BuildHttpClient>();
                Console.WriteLine("Creating destination project client.");
                destinationProjectClient = destinationConnection.GetClient<ProjectHttpClient>();
                Console.WriteLine("Creating destination task agent client.");
                destinationTaskAgentClient = destinationConnection.GetClient<TaskAgentHttpClient>();
                Console.WriteLine("Creating destination TFVC client.");
                destinationTfvcClient = destinationConnection.GetClient<TfvcHttpClient>();
                Console.WriteLine();
            }
            catch (AggregateException ex)
            {
                string message = "Error creating connections.";
                LogAggregateException(ex, message);
            }
            catch (Exception ex)
            {
                string message = "Error creating connections.";
                LogGenericException(ex, message);
            }

            List<BuildDefinition> sourceBuildDefs = null;
            TeamProject destinationProjectObject = null;
            Dictionary<string, TaskAgentQueue> agentDictionary = null;

            try
            {
                sourceBuildDefs = sourceBuildClient.GetFullDefinitionsAsync(project: sourceProject).Result;
            }
            catch (AggregateException ex)
            {
                string message = "Error retreiving objects from source.";
                LogAggregateException(ex, message);
            }
            catch (Exception ex)
            {
                string message = "Error retreiving objects from source.";
                LogGenericException(ex, message);
            }

            try
            {
                destinationProjectObject = destinationProjectClient.GetProject(destinationProject).Result;
                var projectAgentQueues = destinationTaskAgentClient.GetAgentQueuesAsync(project: destinationProject).Result;
                agentDictionary = projectAgentQueues.ToDictionary(k => k.Name);
            }
            catch (AggregateException ex)
            {
                string message = "Error retreiving objects from destination.";
                LogAggregateException(ex, message);
            }
            catch (Exception ex)
            {
                string message = "Error retreiving objects from destination.";
                LogGenericException(ex, message);
            }

            foreach (var item in sourceBuildDefs)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    item.Name = item.Name + DateTime.Now.ToUnixEpochTime();
                }

                item.Project = destinationProjectObject;
                item.AuthoredBy = null; // Remove any author to avoid errors in transfer

                FixTriggerRepositories(item);
                UpdateAgentPool(agentDictionary, item);
                ConvertRepositoryPath(item);

                try
                {
                    Console.WriteLine($"Adding build definition: {item.Name}");
                    var itemOut = destinationBuildClient.CreateDefinitionAsync(item).Result;
                }
                catch (AggregateException ex)
                {
                    LogAggregateException(ex, $"Error creating build {item.Name} on {destinationAccount}");
                }
                catch (Exception ex)
                {
                    LogGenericException(ex, $"Error creating build {item.Name} on {destinationAccount}");
                }
            }
            if (hasErrors)
            {
                ExitWithErrors();
            }
            ExitSuccess();
        }


        public static void ShowHelp(string message, OptionSet option_set)
        {

            Console.Error.WriteLine(message);
            option_set.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }

        private static void FixTriggerRepositories(BuildDefinition item)
        {
            foreach (var triggerItem in item.Triggers)
            {
                if (triggerItem is ContinuousIntegrationTrigger)
                {
                    var convertedTrigger = triggerItem as ContinuousIntegrationTrigger;
                    // Update Branch Filters on CI Trigger
                    for (int i = 0; i < convertedTrigger.PathFilters.Count; i++)
                    {
                        convertedTrigger.PathFilters[i] = FixRepositoryPath(convertedTrigger.PathFilters[i]);
                    }
                    for (int i = 0; i < convertedTrigger.BranchFilters.Count; i++)
                    {
                        convertedTrigger.BranchFilters[i] = FixRepositoryPath(convertedTrigger.BranchFilters[i]);
                    }
                }
                else if (triggerItem is GatedCheckInTrigger)
                {
                    var convertedTrigger = triggerItem as GatedCheckInTrigger;
                    for (int i = 0; i < convertedTrigger.PathFilters.Count; i++)
                    {
                        convertedTrigger.PathFilters[i] = FixRepositoryPath(convertedTrigger.PathFilters[i]);
                    }
                }
                else if (triggerItem is ScheduleTrigger)
                {
                    var convertedTrigger = triggerItem as ScheduleTrigger;
                    foreach (var scheduleTrigger in convertedTrigger.Schedules)
                    {
                        for (int i = 0; i < scheduleTrigger.BranchFilters.Count; i++)
                        {
                            scheduleTrigger.BranchFilters[i] = FixRepositoryPath(scheduleTrigger.BranchFilters[i]);
                        }
                    }
                }
            }
        }

        private static void ExitWithErrors()
        {
            Console.WriteLine("");
            Console.WriteLine("Appication encountered errors. Please use console output to troubleshoot.");
            Environment.Exit(-1);
        }

        private static void ExitSuccess()
        {
            Console.WriteLine("");
            Console.WriteLine("Appication completed succesfully.");
            Environment.Exit(0);
        }

        private static void LogGenericException(Exception ex, string message)
        {
            hasErrors = true;
            Console.WriteLine();
            Console.WriteLine("======================================================================");
            Console.WriteLine(message);
            Console.WriteLine($"Exception message: {ex.Message}");
            Console.WriteLine("======================================================================");
            Console.WriteLine();
        }

        private static void LogAggregateException(AggregateException ex, string message)
        {
            hasErrors = true;
            Console.WriteLine();
            Console.WriteLine("======================================================================");
            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine($"Exception message: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Enumerating Inner Exceptions...");
            foreach (var exceptionItems in ex.InnerExceptions)
            {
                Console.WriteLine($"Exception message: {exceptionItems.Message}");
            }
            Console.WriteLine("======================================================================");
            Console.WriteLine();
        }

        private static void UpdateAgentPool(Dictionary<string, TaskAgentQueue> agentDictionary, BuildDefinition item)
        {
            item.Queue.Id = agentDictionary[item.Queue.Name].Id;
            item.Queue.Pool.Id = agentDictionary[item.Queue.Name].Pool.Id;
            item.Queue.Pool.IsHosted = agentDictionary[item.Queue.Name].Pool.IsHosted;
            item.Queue.Pool.Name = agentDictionary[item.Queue.Name].Pool.Name;
            item.Queue.Name = agentDictionary[item.Queue.Name].Name;
        }

        private static void ConvertRepositoryPath(BuildDefinition item)
        {
            item.Repository.DefaultBranch = FixRepositoryPath(item.Repository.DefaultBranch);
            item.Repository.Name = FixRepositoryName(item.Repository.Name);
            item.Repository.RootFolder = FixRepositoryPath(item.Repository.RootFolder);
            System.Uri outUri;
            System.Uri.TryCreate(destinationUrl, UriKind.Absolute, out outUri);
            item.Repository.Url = outUri;
            item.Repository.Properties["tfvcMapping"] = FixRepositoryPath(item.Repository.Properties["tfvcMapping"]);
        }

        private static string FixRepositoryPath(string item)
        {
            return item.Replace(sourceRepo, destinationRepo);
        }
        private static string FixRepositoryName(string item)
        {
            return item.Replace(sourceRepo.Replace("$/",""), destinationRepo.Replace("$/", ""));
        }
    }
}
