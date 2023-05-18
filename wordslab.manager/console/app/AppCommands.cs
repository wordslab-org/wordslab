using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.apps;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console.app
{
    public class AppInfoCommand : CommandWithUI<AppInfoCommand.Settings>
    {
        private readonly ConfigStore configStore;

        public AppInfoCommand(ConfigStore configStore, ICommandsUI ui) : base(ui) 
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
                UI.WriteLine($"Kubernetes app URL is not in a valid format: {ex.Message}");
                UI.WriteLine();
                return 1;
            }

            UI.WriteLine($"Downloading kubernetes app metadata from from {appUrl} ...");
            UI.WriteLine();
            var appSpec = AsyncUtil.RunSync(() => KubernetesApp.GetMetadataFromYamlFileAsync(appUrl, configStore));

            KubernetesAppsManager.DisplayKubernetesAppSpec(appSpec, UI);  

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

    public abstract class AppCommand<TSettings> : CommandWithUI<TSettings> where TSettings : VmNameSettings
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;
        protected readonly VirtualMachinesManager vmManager;
        protected readonly KubernetesAppsManager appManager;

        public AppCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(ui)
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
                UI.WriteLine("Virtual machine name argument is missing");
                return 1;
            }

            var vm = vmManager.TryFindLocalVM(vmName);
            if (vm == null)
            {
                UI.WriteLine($"Could not find a local virtual machine named: {vmName}");
                return 1;
            }

            if (!vm.IsRunning())
            {
                UI.WriteLine($"Virtual machine {vmName} is not running: please start it first");
                return 1;
            }
             
            return ExecuteCommand(vm, context, settings);
        }

        protected abstract int ExecuteCommand(VirtualMachine vm, CommandContext context, TSettings settings);
    }

    public class AppListCommand : AppCommand<VmNameSettings>
    {
        public AppListCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameSettings settings)
        {
            var apps = appManager.ListKubernetesApps(vm);
            if(apps.Count == 0)
            {
                UI.WriteLine($"No kubernetes apps installed on virtual machine {vm.Name}.");
                UI.WriteLine();
                return 0; 
            }
            else
            {
                UI.WriteLine($"Kubernetes apps installed on virtual machine {vm.Name}:");
                UI.WriteLine();

                foreach (var app in apps )
                {
                    UI.WriteLine("----------");
                    KubernetesAppsManager.DisplayKubernetesAppInstall(app, UI);                    
                }
            }

            return 0;
        }
    }

    public class AppDownloadCommand : AppCommand<AppDownloadCommand.Settings>
    {
        public AppDownloadCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
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
                UI.WriteLine($"Kubernetes app URL is not in a valid format: {ex.Message}");
                UI.WriteLine();
                return 1;
            }

            var result = AsyncUtil.RunSync(() => appManager.DownloadKubernetesApp(vm, yamlFileUrl, UI));
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
        public AppRemoveCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameAndAppIdSettings settings)
        {
            var yamlFileHash = settings.AppID;
            if (String.IsNullOrEmpty(yamlFileHash))
            {
                UI.WriteLine("Kubernetes app ID argument is missing");
                return 1;
            }

            var app = configStore.TryGetKubernetesApp(vm.Name, yamlFileHash);
            if (app == null)
            {
                UI.WriteLine($"Could not find a kubernetes app with ID {yamlFileHash} on virtual machine {vm.Name}");
                return 1;
            }

            var result = AsyncUtil.RunSync(() => appManager.RemoveKubernetesApp(app, vm, UI));
            if (!result)
            {
                return -1;
            }

            return 0;
        }
    }
}
