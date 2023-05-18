using Spectre.Console.Cli;
using System.ComponentModel;
using wordslab.manager.apps;
using wordslab.manager.config;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console.app
{
    public class AppDeploymentListCommand : AppCommand<VmNameSettings>
    {
        public AppDeploymentListCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameSettings settings)
        {
            AsyncUtil.RunSync(() => KubernetesAppsManager.DisplayKubernetesAppDeployments(vm, UI, configStore));

            return 0;
        }
    }

    public class AppDeploymentCreateCommand : AppCommand<VmNameAndAppIdSettings>
    {
        public AppDeploymentCreateCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
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
            else
            {
                AsyncUtil.RunSync(() => KubernetesApp.ParseYamlFileContent(app, loadContainersMetadata: false, configStore));
            }

            var result = AsyncUtil.RunSync(() => appManager.DeployKubernetesApp(app, vm, UI));
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
        public AppDeploymentCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            var deploymentNamespace = settings.NamespaceName;
            if (String.IsNullOrEmpty(deploymentNamespace))
            {
                UI.WriteLine("Deployment namespace argument is missing");
                return 1;
            }

            var appDeployment = configStore.TryGetKubernetesAppDeployment(vm.Name, deploymentNamespace);
            if (appDeployment == null)
            {
                UI.WriteLine($"Could not find a deployment namespace named {deploymentNamespace} on virtual machine {vm.Name}");
                return 1;
            }

            return ExecuteDeploymentCommand(appDeployment, vm, context, settings);
        }

        protected abstract int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings);
    }

    public class AppDeploymentStopCommand : AppDeploymentCommand
    {
        public AppDeploymentStopCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage,  configStore, ui)
        { }

        protected override int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            if(appDeployment.State == AppDeploymentState.Stopped) 
            {
                UI.WriteLine($"Kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} is already stopped.");
                UI.WriteLine();
                return 0;
            }

            UI.WriteLine($"Stopping kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} ...");
            UI.WriteLine();
            try
            {
                appManager.StopKubernetesAppDeployment(appDeployment, vm);
                UI.WriteLine($"The memory used by kubernetes app deployment {appDeployment.Namespace} was successfully released.");
                UI.WriteLine("The application entry points are not available anymore but your data is still there.");
                UI.WriteLine("You can restart this application anytime you want with the app deployment start command.");
                UI.WriteLine();
            }
            catch (Exception ex)
            {
                UI.WriteLine($"Failed to stop kubernetes app deployment {appDeployment.Namespace}:");
                UI.WriteLine(ex.Message);
                UI.WriteLine();
                return -1;
            }

            return 0;
        }
    }


    public class AppDeploymentStartCommand : AppDeploymentCommand
    {
        public AppDeploymentStartCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            if (appDeployment.State != AppDeploymentState.Stopped)
            {
                UI.WriteLine($"Kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} is already started.");
                UI.WriteLine();
                return 0;
            }

            UI.WriteLine($"Restarting kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} ...");
            UI.WriteLine();
            try
            {
                appManager.RestartKubernetesAppDeployment(appDeployment, vm);
                UI.WriteLine("OK");
                UI.WriteLine();
                UI.WriteLine("The components of the application are now starting: this may take up to one minute ...");
                var successful = AsyncUtil.RunSync(() => KubernetesAppsManager.WaitForKubernetesAppEntryPoints(appDeployment, vm));
                if (successful)
                {
                    UI.WriteLine("OK");
                }
                else
                {
                    UI.WriteLine("The application wasn't completely ready after one minute: you may have to wait a little bit longer before you can use all user entry points");
                }
                UI.WriteLine();

                KubernetesAppsManager.DisplayKubernetesAppEntryPoints(appDeployment.App, UI, vm.RunningInstance.GetHttpAddressAndPort(), appDeployment.Namespace);
            }
            catch (Exception ex)
            {
                UI.WriteLine($"Failed to restart kubernetes app deployment {appDeployment.Namespace}:");
                UI.WriteLine(ex.Message);
                UI.WriteLine();
                return -1;
            }

            return 0;
        }
    }

    public class AppDeploymentDeleteCommand : AppDeploymentCommand
    {
        public AppDeploymentDeleteCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteDeploymentCommand(KubernetesAppDeployment appDeployment, VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settings)
        {
            AsyncUtil.RunSync(() => appManager.DeleteKubernetesAppDeployment(appDeployment, vm, UI));

            return 0;
        }
    }
}
