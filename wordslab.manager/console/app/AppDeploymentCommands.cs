using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using wordslab.manager.apps;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console.app
{
    public class AppDeploymentListCommand : AppCommand<VmNameSettings>
    {
        public AppDeploymentListCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameSettings settings)
        {
            var ui = new ConsoleProcessUI();
            AsyncUtil.RunSync(() => KubernetesAppsManager.DisplayKubernetesAppDeployments(vm, ui, configStore));

            return 0;
        }
    }

    public class AppDeploymentCreateCommand : AppCommand<VmNameAndAppIdSettings>
    {
        public AppDeploymentCreateCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
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
            else
            {
                AsyncUtil.RunSync(() => KubernetesApp.ParseYamlFileContent(app, loadContainersMetadata: false, configStore));
            }

            var ui = new ConsoleProcessUI();
            var result = AsyncUtil.RunSync(() => appManager.DeployKubernetesApp(app, vm, ui));
            if (result == null)
            {
                return -1;
            }

            return 0;
        }
    }

    public class VmAndDeploymentNameSettings : VmNameSettings
    {
        [Description("Kubernetes app deployment namespace")]
        [CommandArgument(1, "[namespace]")]
        public string NamespaceName { get; init; }
    }

    public class AppDeploymentStopCommand : AppCommand<VmAndDeploymentNameSettings>
    {
        public AppDeploymentStopCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage,  configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            AnsiConsole.MarkupLine("app deployment stop command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }


    public class AppDeploymentStartCommand : AppCommand<VmAndDeploymentNameSettings>
    {
        public AppDeploymentStartCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            AnsiConsole.MarkupLine("app deployment start command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }

    public class AppDeploymentDeleteCommand : AppCommand<VmAndDeploymentNameSettings>
    {
        public AppDeploymentDeleteCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            AnsiConsole.MarkupLine("app deployment delete command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }
}
