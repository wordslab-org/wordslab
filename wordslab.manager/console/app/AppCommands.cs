using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.apps;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console.app
{
    public class AppInfoCommand : Command<AppInfoCommand.Settings>
    {

        private readonly ConfigStore configStore;

        public AppInfoCommand(ConfigStore configStore) 
        { 
            this.configStore = configStore;
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var appUrl = settings.AppURL;
            try
            {
                var parsedUrl = new Uri(appUrl);  
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine($"Kubernetes app URL is not in a valid format: {ex.Message}");
                AnsiConsole.WriteLine();
                return 1;
            }

            AnsiConsole.WriteLine($"Downloading kubernetes app metadata from from {appUrl} ...");
            AnsiConsole.WriteLine();
            var appSpec = AsyncUtil.RunSync(() => KubernetesApp.GetMetadataFromYamlFileAsync(appUrl, configStore));

            var ui = new ConsoleProcessUI();
            KubernetesAppsManager.DisplayKubernetesAppSpec(appSpec, ui);  

            return 0;
        }

        public class Settings : CommandSettings
        {
            [Description("Kubernetes app URL")]
            [CommandArgument(0, "[url]")]
            public string AppURL { get; init; }
        }
    }

    public class VmNameSettings : CommandSettings
    {
        [Description("Virtual machine name")]
        [CommandArgument(0, "[vm]")]
        public string VmName { get; init; }
    }

    public class VmNameAndAppIdSettings : VmNameSettings
    {
        [Description("Kubernetes app ID")]
        [CommandArgument(1, "[ID]")]
        public string AppID { get; init; }
    }

    public abstract class AppCommand<TSettings> : Command<TSettings> where TSettings : VmNameSettings
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;
        protected readonly VirtualMachinesManager vmManager;
        protected readonly KubernetesAppsManager appManager;

        public AppCommand(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
            this.vmManager = new VirtualMachinesManager(hostStorage, configStore);
            this.appManager = new KubernetesAppsManager(configStore);
        }

        public override int Execute(CommandContext context, TSettings settings)
        {
            var vmName = settings.VmName;
            if (String.IsNullOrEmpty(vmName))
            {
                AnsiConsole.WriteLine("Virtual machine name argument is missing");
                return 1;
            }

            var vm = vmManager.TryFindLocalVM(vmName);
            if (vm == null)
            {
                AnsiConsole.WriteLine($"Could not find a local virtual machine named: {vmName}");
                return 1;
            }

            if (!vm.IsRunning())
            {
                AnsiConsole.WriteLine($"Virtual machine {vmName} is not running: please start it first");
                return 1;
            }
             
            return ExecuteCommand(vm, context, settings);
        }

        protected abstract int ExecuteCommand(VirtualMachine vm, CommandContext context, TSettings settings);
    }

    public class AppListCommand : AppCommand<VmNameSettings>
    {
        public AppListCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameSettings settings)
        {
            var apps = appManager.ListKubernetesApps(vm);
            if(apps.Count == 0)
            {
                AnsiConsole.WriteLine($"No kubernetes apps installed on virtual machine {vm.Name}.");
                AnsiConsole.WriteLine();
                return 0; 
            }
            else
            {
                AnsiConsole.WriteLine($"Kubernetes apps installed on virtual machine {vm.Name}:");
                AnsiConsole.WriteLine();

                var ui = new ConsoleProcessUI();
                foreach (var app in apps )
                {
                    AnsiConsole.WriteLine("----------");
                    KubernetesAppsManager.DisplayKubernetesAppInstall(app, ui);                    
                }
            }

            return 0;
        }
    }

    public class AppDownloadCommand : AppCommand<AppDownloadCommand.Settings>
    {
        public AppDownloadCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, Settings settings)
        {
            var yamlFileUrl = settings.AppURL;
            try
            {
                var parsedUrl = new Uri(yamlFileUrl);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine($"Kubernetes app URL is not in a valid format: {ex.Message}");
                AnsiConsole.WriteLine();
                return 1;
            }

            var ui = new ConsoleProcessUI();
            var result = AsyncUtil.RunSync(() => appManager.DownloadKubernetesApp(vm, yamlFileUrl, ui));
            if (result == null)
            {
                return -1;
            }
            
            return 0;
        }

        public class Settings : VmNameSettings
        {
            [Description("Kubernetes app URL")]
            [CommandArgument(1, "[url]")]
            public string AppURL { get; init; }
        }
    }

    public class AppRemoveCommand : AppCommand<VmNameAndAppIdSettings>
    {
        public AppRemoveCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameAndAppIdSettings settings)
        {
            var yamlFileHash = settings.AppID;
            if (String.IsNullOrEmpty(yamlFileHash))
            {
                AnsiConsole.WriteLine("Kubernetes app ID argument is missing");
                return 1;
            }

            var app = configStore.TryGetKubernetesApp(vm.Name, yamlFileHash);
            if (app == null)
            {
                AnsiConsole.WriteLine($"Could not find a kubernetes app with ID {yamlFileHash} on virtual machine {vm.Name}");
                return 1;
            }

            var ui = new ConsoleProcessUI();
            var result = AsyncUtil.RunSync(() => appManager.RemoveKubernetesApp(app, vm, ui));
            if (!result)
            {
                return -1;
            }

            return 0;
        }
    }
}
