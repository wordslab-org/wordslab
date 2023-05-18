using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Xml.Linq;
using wordslab.manager.apps;
using wordslab.manager.config;
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

    public abstract class AppDeploymentCommand : AppCommand<VmAndDeploymentNameSettings>
    {
        public AppDeploymentCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            var deploymentNamespace = settings.NamespaceName;
            if (String.IsNullOrEmpty(deploymentNamespace))
            {
                AnsiConsole.WriteLine("Deployment namespace argument is missing");
                return 1;
            }

            var appDeployment = configStore.TryGetKubernetesAppDeployment(vm.Name, deploymentNamespace);
            if (appDeployment == null)
            {
                AnsiConsole.WriteLine($"Could not find a deployment namespace named {deploymentNamespace} on virtual machine {vm.Name}");
                return 1;
            }

            return ExecuteDeploymentCommand(appDeployment, vm, context, settings);
        }

        protected abstract int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings);
    }

    public class AppDeploymentStopCommand : AppDeploymentCommand
    {
        public AppDeploymentStopCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage,  configStore)
        { }

        protected override int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            if(appDeployment.State == AppDeploymentState.Stopped) 
            {
                AnsiConsole.MarkupLine($"Kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} is already stopped.");
                AnsiConsole.WriteLine();
                return 0;
            }

            AnsiConsole.MarkupLine($"Stopping kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} ...");
            AnsiConsole.WriteLine();
            try
            {
                appManager.StopKubernetesAppDeployment(appDeployment, vm);
                AnsiConsole.WriteLine($"The memory used by kubernetes app deployment {appDeployment.Namespace} was successfully released.");
                AnsiConsole.WriteLine("The application entry points are not available anymore but your data is still there.");
                AnsiConsole.WriteLine("You can restart this application anytime you want with the app deployment start command.");
                AnsiConsole.WriteLine();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine($"Failed to stop kubernetes app deployment {appDeployment.Namespace}:");
                AnsiConsole.WriteLine(ex.Message);
                AnsiConsole.WriteLine();
                return -1;
            }

            return 0;
        }
    }


    public class AppDeploymentStartCommand : AppDeploymentCommand
    {
        public AppDeploymentStartCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            if (appDeployment.State != AppDeploymentState.Stopped)
            {
                AnsiConsole.MarkupLine($"Kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} is already started.");
                AnsiConsole.WriteLine();
                return 0;
            }

            AnsiConsole.MarkupLine($"Restarting kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} ...");
            AnsiConsole.WriteLine();
            try
            {
                appManager.RestartKubernetesAppDeployment(appDeployment, vm);
                AnsiConsole.WriteLine("OK");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine("The components of the application are now starting: this may take up to one minute ...");
                var successful = AsyncUtil.RunSync(() => KubernetesAppsManager.WaitForKubernetesAppEntryPoints(appDeployment, vm));
                if (successful)
                {
                    AnsiConsole.WriteLine("OK");
                }
                else
                {
                    AnsiConsole.WriteLine("The application wasn't completely ready after one minute: you may have to wait a little bit longer before you can use all user entry points");
                }
                AnsiConsole.WriteLine();

                var ui = new ConsoleProcessUI();
                KubernetesAppsManager.DisplayKubernetesAppEntryPoints(appDeployment.App, ui, vm.RunningInstance.GetHttpAddressAndPort(), appDeployment.Namespace);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine($"Failed to restart kubernetes app deployment {appDeployment.Namespace}:");
                AnsiConsole.WriteLine(ex.Message);
                AnsiConsole.WriteLine();
                return -1;
            }

            return 0;
        }
    }

    public class AppDeploymentDeleteCommand : AppDeploymentCommand
    {
        public AppDeploymentDeleteCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            var ui = new ConsoleProcessUI();
            AsyncUtil.RunSync(() => appManager.DeleteKubernetesAppDeployment(appDeployment, vm, ui));

            return 0;
        }
    }
}
