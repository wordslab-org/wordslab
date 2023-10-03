using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.apps;
using wordslab.manager.config;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console
{
    public class InstallCommand : CommandWithUI<InstallCommand.Settings>
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;
        protected readonly VirtualMachinesManager vmManager;

        public InstallCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(ui)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
            this.vmManager = new VirtualMachinesManager(hostStorage, configStore);
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var appName = settings.AppName;
            if (String.IsNullOrEmpty(appName))
            {
                UI.WriteLine("Built-in wordslab app name argument is missing");
                return 1;
            }
            string appURL = null;
            if(appName == "notebooks")
            {
                appURL = KubernetesApp.WORDSLAB_NOTEBOOKS_GPU_APP_URL;
            }
            else
            {
                UI.WriteLine($"{appName}: unknown built-in wordslab app name");
                return 1;
            }

            UI.WriteLine($"wordslab manager version: {ConsoleApp.Version}");
            UI.WriteLine();
            UI.WriteLine($"Installing {appName} built-in application with all the default parameters");
            UI.WriteLine();

            if (configStore.HostMachineConfig == null)
            {
                var config = AsyncUtil.RunSync(() => vmManager.ConfigureHostMachine(UI, useDefaultConfig:true));
                if (config == null)
                {

                    return 1;
                }
            }

            var vmName = "dev";
            var vm = vmManager.TryFindLocalVM(vmName);
            if (vm == null)
            {
                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                var vmPresetSpec = vmSpecs.MaximumVMSpecOnThisMachine;
                var vmConfig = AsyncUtil.RunSync(() => vmManager.CreateLocalVMConfig(vmName, vmPresetSpec, configStore.HostMachineConfig, UI));
                if (vmConfig == null)
                {
                    return -1;
                }

                vm = AsyncUtil.RunSync(() => vmManager.CreateLocalVM(vmConfig, UI, useDefaultConfig: true));
                if (vm == null)
                {
                    return -1;
                }

                vm = vmManager.TryFindLocalVM(vmName);
                if (vm == null)
                {
                    UI.WriteLine($"Could not find a local virtual machine named: {vmName}");
                    return 1;
                }
            }

            if (!vm.IsRunning())
            {
                UI.WriteLine($"Starting virtual machine {vmName} ...");
                UI.WriteLine();

                // Then start the VM with the merged config properties
                VirtualMachineInstance vmi = null;
                try
                {
                    var computeSpec = vm.Config.Spec.Compute;
                    var gpuSpec = vm.Config.Spec.GPU;

                    UI.WriteLine($"- {computeSpec.Processors} processors");
                    UI.WriteLine($"- {computeSpec.MemoryGB} GB memory");
                    if (gpuSpec.GPUCount > 0)
                    {
                        UI.WriteLine($"- {gpuSpec.ToString()}");
                    }
                    UI.WriteLine();

                    vmi = vm.Start(computeSpec, gpuSpec);
                }
                catch (Exception ex)
                {
                    UI.WriteLine($"Failed to start virtual machine {vmName}:");
                    UI.WriteLine(ex.Message);
                    UI.WriteLine();
                    return -1;
                }
            }

            UI.WriteLine($"Virtual machine {vmName} is running");
            UI.WriteLine();

            var appDeployment = configStore.TryGetKubernetesAppDeployment(vmName, "notebooks");
            if (appDeployment == null)
            {
                KubernetesAppsManager appsManager = new KubernetesAppsManager(configStore);
                var appInstall = AsyncUtil.RunSync(() => appsManager.DownloadKubernetesApp(vm, appURL, UI, useDefaultConfig: true));
                if (appInstall == null)
                {
                    return -1;
                }

                appDeployment = AsyncUtil.RunSync(() => appsManager.DeployKubernetesApp(appInstall, vm, UI, useDefaultConfig: true));
                if (appDeployment == null)
                {
                    return -1;
                }
            }
            else
            {
                if (appDeployment.State != AppDeploymentState.Stopped)
                {
                    UI.WriteLine($"Kubernetes app {appDeployment.App.Name} deployment in namespace {appDeployment.Namespace} is running");
                    UI.WriteLine();
                }
                else
                {
                    UI.WriteLine($"Starting application {appDeployment.App.Name} in namespace {appDeployment.Namespace}: this may take several minutes ...");
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
                }

                KubernetesAppsManager.DisplayKubernetesAppEntryPoints(appDeployment.App, UI, vm.RunningInstance.GetHttpAddressAndPort(), appDeployment.Namespace);
            }

            return 0;
        }

        public class Settings : CommandSettings
        {
            [Description("Built-in wordslab app name")]
            [CommandArgument(0, "[appName]")]
            public string AppName { get; init; }
        }
    }
}