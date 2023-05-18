using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.apps;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console.host
{
    public class NoParamsSettings : CommandSettings
    { }

    public class VmNameSettings : CommandSettings
    {
        [Description("Virtual machine name")]
        [CommandArgument(0, "[name]")]
        public string Name { get; init; }
    }

    public class VmPresetSettings : VmNameSettings
    {
        [Description("Use minimum compute and storage config")]
        [CommandOption("--minimum")]
        public bool? UseMinimumSpecs { get; init; }

        [Description("Use recommended compute and storage config")]
        [CommandOption("--recommended")]
        public bool? UseRecommendedSpecs { get; init; }

        [Description("Use maximum compute and storage config")]
        [CommandOption("--maximum")]
        public bool? UseMaximumSpecs { get; init; }
    }

    public class VmComputeSettings : VmPresetSettings
    {
        [Description("Max number of processors")]
        [CommandOption("--proc")]
        public int? Processors { get; init; }

        [Description("Max memory size in GB")]
        [CommandOption("--mem")]
        public int? MemoryGB { get; init; }

        [Description("Allow acces to GPU ?")]
        [CommandOption("--gpu")]
        public bool? UseGPU { get; init; }

        public ComputeSpec GetComputeStartArguments(VirtualMachineConfig vmConfig)
        {
            // Step 1 : start with vm config 
            var computeSpec = (ComputeSpec)vmConfig.Spec.Compute.Clone();

            // Step 2 : override compute values with min, rec or max specs 
            if (UseMinimumSpecs.HasValue || UseRecommendedSpecs.HasValue || UseMaximumSpecs.HasValue)
            {
                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();                               
                if (UseMinimumSpecs.HasValue && UseMinimumSpecs.Value)
                {
                    computeSpec = (ComputeSpec)vmSpecs.MinimumVMSpec.Compute.Clone();
                }
                if (UseRecommendedSpecs.HasValue && UseRecommendedSpecs.Value && vmSpecs.RecommendedVMSpecIsSupportedOnThisMachine)
                {
                    computeSpec = (ComputeSpec)vmSpecs.RecommendedVMSpec.Compute.Clone();
                }
                if (UseMaximumSpecs.HasValue && UseMaximumSpecs.Value)
                {
                    computeSpec = (ComputeSpec)vmSpecs.MaximumVMSpecOnThisMachine.Compute.Clone();
                }
            }

            // Step 3 : apply all the values explicitly set on the command line
            if (Processors.HasValue)
            {
                computeSpec.Processors = Processors.Value;
            }
            if (MemoryGB.HasValue)
            {
                computeSpec.MemoryGB = MemoryGB.Value;
            }

            return computeSpec;
        }

        public GPUSpec GetGPUStartArguments(VirtualMachineConfig vmConfig)
        {
            // Step 1 : start with vm config 
            var gpuSpec = (GPUSpec)vmConfig.Spec.GPU.Clone();

            // Step 2 : override compute values with min, rec or max specs 
            RecommendedVMSpecs vmSpecs = null;
            if (UseMinimumSpecs.HasValue || UseRecommendedSpecs.HasValue || UseMaximumSpecs.HasValue)
            {
                vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                if (UseMinimumSpecs.HasValue && UseMinimumSpecs.Value)
                {
                    gpuSpec = (GPUSpec)vmSpecs.MinimumVMSpec.GPU.Clone();
                }
                if (UseRecommendedSpecs.HasValue && UseRecommendedSpecs.Value && vmSpecs.RecommendedVMSpecIsSupportedOnThisMachine)
                {
                    gpuSpec = (GPUSpec)vmSpecs.RecommendedVMSpec.GPU.Clone();
                }
                if (UseMaximumSpecs.HasValue && UseMaximumSpecs.Value)
                {
                    gpuSpec = (GPUSpec)vmSpecs.MaximumVMSpecOnThisMachine.GPU.Clone();
                }
            }

            // Step 3 : apply all the values explicitly set on the command line
            if (UseGPU.HasValue)
            {
                if (UseGPU.Value)
                {
                    if (vmSpecs == null) vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                    gpuSpec = (GPUSpec)vmSpecs.MaximumVMSpecOnThisMachine.GPU.Clone();
                }
                else
                {
                    gpuSpec.GPUCount = 0;
                }
            }

            return gpuSpec;
        }
    }

    public abstract class VmCommand<TSettings> : CommandWithUI<TSettings> where TSettings : CommandSettings
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;
        protected readonly VirtualMachinesManager vmManager;

        public VmCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(ui)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
            this.vmManager = new VirtualMachinesManager(hostStorage, configStore);
        }

        public override int Execute(CommandContext context, TSettings settings)
        {
            if (configStore.HostMachineConfig == null)
            {
                UI.WriteLine("Host machine config not yet initialized: please execute 'wordslab host init' first");
                UI.WriteLine();
                return 0;
            }

            UI.WriteLine("Checking local virtual machines state ...");
            UI.WriteLine();

            var vms = vmManager.ListLocalVMs();
            return ExecuteCommand(vms, context, settings);
        }

        protected abstract int ExecuteCommand(List<VirtualMachine> vms, CommandContext context, TSettings settings);
    }

    public class VmListCommand : VmCommand<NoParamsSettings>
    {
        public VmListCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] NoParamsSettings settings)
        {           
            if (vms.Count == 0)
            {
                UI.WriteLine("No virtual machine found on this host: you can create one with the command \"wordslab host vm create\".");
                return 0;
            }

            var runningVMs = vms.Where(vm => vm.IsRunning()).ToList();
            var stoppedVMs = vms.Where(vm => !vm.IsRunning()).ToList();

            // 1. Running VMs
            if (runningVMs.Count > 0)
            {
                UI.WriteLine("Running virtual machines:");
                UI.WriteLine();

                var table = new TableInfo();
                table.AddColumn("Name");
                table.AddColumn("Http address");
                table.AddColumn("Started on");
                table.AddColumn("Running since");
                table.AddColumn("Processors");
                table.AddColumn("Memory");
                table.AddColumn("GPU");
                table.AddColumn("Cluster disk");
                table.AddColumn("Data disk");
                foreach (var vm in runningVMs)
                {
                    var instance = vm.RunningInstance;
                    var displayStatus = instance.GetDisplayStatus();
                    table.AddRow(
                        vm.Name,
                        instance.GetHttpAddressAndPort(),
                        displayStatus.StartedOn,
                        displayStatus.RunningTime,
                        displayStatus.Processors,
                        displayStatus.Memory,
                        displayStatus.GPU,
                        vm.ClusterDisk.CurrentSizeGB + "GB",
                        vm.DataDisk.CurrentSizeGB + "GB");
                }
                UI.DisplayTable(table);
                UI.WriteLine();
            }

            // 2. Stopped VMs
            if (stoppedVMs.Count > 0)
            {
                UI.WriteLine("Stopped virtual machines:");
                UI.WriteLine();

                var table = new TableInfo();
                table.AddColumn("Name");
                table.AddColumn("First start");
                table.AddColumn("Last stop");
                table.AddColumn("Last state");
                table.AddColumn("Total time");
                table.AddColumn("Processors");
                table.AddColumn("Memory");
                table.AddColumn("GPU");
                table.AddColumn("Cluster disk");
                table.AddColumn("Data disk");
                foreach (var vm in stoppedVMs)
                {
                    var displayStatus = vm.GetDisplayStatus();
                    table.AddRow(
                        vm.Name,
                        displayStatus.FirstStart,
                        displayStatus.LastStop,
                        displayStatus.LastState,
                        displayStatus.TotalTime,
                        displayStatus.Processors,
                        displayStatus.Memory,
                        displayStatus.GPU,
                        vm.ClusterDisk.CurrentSizeGB + "GB",
                        vm.DataDisk.CurrentSizeGB + "GB");
                }
                UI.DisplayTable(table);
                UI.WriteLine();
            }

            return 0;
        }
    }

    public class VmStartCommand : VmCommand<VmComputeSettings>
    {
        public VmStartCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] VmComputeSettings vmSettings)
        {
            var vmName = vmSettings.Name;
            if (String.IsNullOrEmpty(vmName))
            {
                UI.WriteLine("Virtual machine name argument is missing");
                return 1;
            }

            var vm = vmManager.TryFindLocalVM(vmName);
            if(vm == null)
            {
                UI.WriteLine($"Could not find a local virtual machine named: {vmName}");
                return 1;
            }

            if(vm.IsRunning())
            {
                UI.WriteLine($"Virtual machine {vmName} is already running");
                return 0;
            }

            UI.WriteLine($"Starting virtual machine {vmName} ...");
            UI.WriteLine();

            // Then start the VM with the merged config properties
            VirtualMachineInstance vmi = null;
            try
            {
                var computeSpec = vmSettings.GetComputeStartArguments(vm.Config);
                var gpuSpec = vmSettings.GetGPUStartArguments(vm.Config);

                UI.WriteLine($"- {computeSpec.Processors} processors");
                UI.WriteLine($"- {computeSpec.MemoryGB} GB memory");
                if(gpuSpec.GPUCount > 0)
                {
                    UI.WriteLine($"- {gpuSpec.ToString()}");
                }
                UI.WriteLine();

                vmi = vm.Start(computeSpec, gpuSpec);
            }
            catch(Exception ex)
            {
                UI.WriteLine($"Failed to start virtual machine {vmName}:");
                UI.WriteLine(ex.Message);
                UI.WriteLine();
                return -1;
            }

            UI.WriteLine($"Virtual machine {vmName} started:");
            UI.WriteLine($"- http  address: {vmi.GetHttpAddressAndPort()}");
            // AnsiConsole.WriteLine($"- https address: {vmi.GetHttpsAddress()}");
            UI.WriteLine();

            var appDeployments = configStore.ListKubernetesAppsDeployedOn(vmName);
            foreach(var appDeployment in appDeployments)
            {
                UI.WriteLine($"Starting application {appDeployment.App.Name} in namespace {appDeployment.Namespace}: this may take up to one minute ...");
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

            AsyncUtil.RunSync(() => KubernetesAppsManager.DisplayKubernetesAppDeployments(vm, UI, configStore));

            return 0;
        }
    }

    public class VmStopCommand : VmCommand<VmNameSettings>
    {
        public VmStopCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.Name;
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
                UI.WriteLine($"Virtual machine {vmName} is already stopped");
                return 0;
            }

            UI.WriteLine($"Stopping virtual machine {vmName} ...");
            UI.WriteLine();

            var vmInstance = vm.RunningInstance;

            try 
            { 
                vm.Stop();
            }
            catch (Exception ex)
            {
                UI.WriteLine($"Failed to stop virtual machine {vmName}:");
                UI.WriteLine(ex.Message);
                UI.WriteLine();
                return -1;
            }

            UI.WriteLine($"Virtual machine {vmName} stopped.");
            UI.WriteLine($"Running time: {(vmInstance.StopTimestamp.Value.Subtract(vmInstance.StartTimestamp).ToString(@"d\.hh\:mm\:ss"))}");
            UI.WriteLine();
            return 0;
        }
    }

    public class VmStatusCommand : VmCommand<VmNameSettings>
    {
        public VmStatusCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.Name;
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

            UI.WriteLine($"Virtual machine {vmName} status:");
            UI.WriteLine();
            if (vm.IsRunning())
            {
                var instance = vm.RunningInstance;
                var displayStatus = instance.GetDisplayStatus();
                UI.WriteLine("- state: running");
                UI.WriteLine($"- started on: {displayStatus.StartedOn}");
                UI.WriteLine($"- running since: {displayStatus.RunningTime}");
                UI.WriteLine();
                UI.WriteLine($"- processors: {displayStatus.Processors}");
                UI.WriteLine($"- memory: {displayStatus.Memory}");
                UI.WriteLine($"- GPU: {displayStatus.GPU}");
                UI.WriteLine($"- cluster disk: {vm.ClusterDisk.CurrentSizeGB} GB");
                UI.WriteLine($"- data disk: {vm.DataDisk.CurrentSizeGB} GB");
                UI.WriteLine();
                UI.WriteLine($"-> {instance.GetHttpAddressAndPort()}");
                UI.WriteLine($"-> {instance.GetHttpsAddressAndPort()}");
            }
            else
            {
                var vmInstance = configStore.TryGetLastVirtualMachineInstance(vmName);
                if (vmInstance == null)
                {
                    UI.WriteLine("- state: never started");
                }
                else
                {
                    var displayStatus = vmInstance.GetDisplayStatus();
                    UI.WriteLine($"- state: {vmInstance.State.ToString().ToLowerInvariant()}");
                    UI.WriteLine($"- last stopped on: {displayStatus.StoppedOn}");
                    UI.WriteLine($"- last running time: {displayStatus.RunningTime}");
                    UI.WriteLine();
                    UI.WriteLine($"- processors: {vm.Config.Spec.Compute.Processors}");
                    UI.WriteLine($"- memory: {vm.Config.Spec.Compute.MemoryGB} GB");
                    if (vm.Config.Spec.GPU.GPUCount > 0)
                    {
                        UI.WriteLine($"- GPU: {vm.Config.Spec.GPU.ToString()}");
                    }
                    UI.WriteLine($"- cluster disk: {vm.ClusterDisk.CurrentSizeGB} GB");
                    UI.WriteLine($"- data disk: {vm.DataDisk.CurrentSizeGB} GB");
                }
            }
            UI.WriteLine();

            return 0;
        }
    }

    public class VmAdviseCommand : VmCommand<VmNameSettings>
    {
        public VmAdviseCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            UI.WriteLine("Analysing host machine hardware ...");
            UI.WriteLine();

            var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
            
            DisplayRecommendedSpec("Minimum", vmSpecs.MinimumVMSpec, vmSpecs.MinimunVMSpecErrorMessage, UI);
            if (vmSpecs.MinimumVMSpecIsSupportedOnThisMachine)
            {
                DisplayRecommendedSpec("Maximum", vmSpecs.MaximumVMSpecOnThisMachine, null, UI);
            }
            DisplayRecommendedSpec("Recommended", vmSpecs.RecommendedVMSpec, vmSpecs.RecommendedVMSpecErrorMessage, UI);

            var tcpPortsInUse = Network.GetAllTcpPortsInUse();
            var sshPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_SSH_PORT);
            var kubernetesPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_Kubernetes_PORT);
            var httpPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_HttpIngress_PORT);
            var httpsPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_HttpsIngress_PORT);

            string portAvailable = "available";
            string portInUse = "already in use";
            UI.WriteLine("Checking if the default Host ports (VM port forward on localhost) are already used:");
            UI.WriteLine($"- connect to the virtual machine through SSH: {VMRequirements.DEFAULT_HOST_SSH_PORT} -> {(sshPortAvailable ? portAvailable : portInUse)}");
            UI.WriteLine($"- connect to the Kubernetes cluster inside the VM: {VMRequirements.DEFAULT_HOST_Kubernetes_PORT} -> {(kubernetesPortAvailable ? portAvailable : portInUse)}");
            UI.WriteLine($"- connect to the HTTP services inside the VM: {VMRequirements.DEFAULT_HOST_HttpIngress_PORT} -> {(httpPortAvailable ? portAvailable : portInUse)}");
            UI.WriteLine($"- connect to the HTTPS services inside the VM: {VMRequirements.DEFAULT_HOST_HttpsIngress_PORT} -> {(httpPortAvailable ? portAvailable : portInUse)}");
            UI.WriteLine("");

            return 0;
        }

        private static void DisplayRecommendedSpec(string specName, VirtualMachineSpec spec, string errMsg, ICommandsUI ui)
        {
            ui.WriteLine($"{specName} virtual machine configuration :");
            if (!String.IsNullOrEmpty(errMsg))
            {
                ui.WriteLine($"=> {errMsg}");
            }
            else
            {
                ui.WriteLine($"- number of processors: {spec.Compute.Processors}");
                ui.WriteLine($"- memory size in GB: {spec.Compute.MemoryGB}");
                if (!String.IsNullOrEmpty(spec.GPU.ModelName))
                {
                    ui.WriteLine($"- GPU model: {spec.GPU.ModelName} {spec.GPU.MemoryGB}GB");
                }
                ui.WriteLine($"- cluster disk size in GB: {spec.Storage.ClusterDiskSizeGB}" + (spec.Storage.ClusterDiskIsSSD ? " (SSD)" : ""));
                ui.WriteLine($"- data disk size in GB: {spec.Storage.DataDiskSizeGB}" + (spec.Storage.DataDiskIsSSD ? " (SSD)" : ""));
            }
            ui.WriteLine();
        }
    }

    public class VmCreateCommand : VmCommand<VmPresetSettings>
    {
        public VmCreateCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] VmPresetSettings vmSettings)
        {
            var vmName = vmSettings.Name;
            if (String.IsNullOrEmpty(vmName))
            {
                UI.WriteLine("Virtual machine name argument is missing");
                return 1;
            }

            VirtualMachineSpec vmPresetSpec = null;
            if (vmSettings.UseMinimumSpecs.HasValue || vmSettings.UseRecommendedSpecs.HasValue || vmSettings.UseMaximumSpecs.HasValue)
            {
                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                if (vmSpecs.MinimumVMSpecIsSupportedOnThisMachine)
                {
                    if (vmSettings.UseMinimumSpecs.HasValue && vmSettings.UseMinimumSpecs.Value)
                    {
                        vmPresetSpec = vmSpecs.MinimumVMSpec;
                    }
                    if (vmSettings.UseRecommendedSpecs.HasValue && vmSettings.UseRecommendedSpecs.Value && vmSpecs.RecommendedVMSpecIsSupportedOnThisMachine)
                    {
                        vmPresetSpec = vmSpecs.RecommendedVMSpec;
                    }
                    if (vmSettings.UseMaximumSpecs.HasValue && vmSettings.UseMaximumSpecs.Value)
                    {
                        vmPresetSpec = vmSpecs.MaximumVMSpecOnThisMachine;
                    }
                }
            }

            var vmConfig = AsyncUtil.RunSync(() => vmManager.CreateLocalVMConfig(vmName, vmPresetSpec, configStore.HostMachineConfig, UI));
            if(vmConfig == null)
            {
                return -1;
            }

            var vm = AsyncUtil.RunSync(() => vmManager.CreateLocalVM(vmConfig, UI));
            if (vm == null)
            {
                return -1;
            }

            return 0;
        }        
    }

    public class VmResizeCommand : VmCommand<VmNameSettings>
    {
        public VmResizeCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            UI.WriteLine("ERROR: host vm resize command not yet implemented");
            return -1;
        }
    }
        
    public class VmDeleteCommand : VmCommand<VmNameSettings>
    {
        public VmDeleteCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        protected override int ExecuteCommand(List<VirtualMachine> vms, [NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.Name;
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

            var success = AsyncUtil.RunSync(() => vmManager.DeleteLocalVM(vmName, UI));
            if (!success)
            {
                return -1;
            }

            return 0;
        }
    }
}
