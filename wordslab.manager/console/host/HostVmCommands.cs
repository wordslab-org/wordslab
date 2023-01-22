using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

    public abstract class VmCommand<TSettings> : Command<TSettings> where TSettings : CommandSettings
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;
        protected readonly VirtualMachinesManager vmManager;

        public VmCommand(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
            this.vmManager = new VirtualMachinesManager(hostStorage, configStore);
        }
    }

    public class VmListCommand : VmCommand<NoParamsSettings>
    {
        public VmListCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] NoParamsSettings settings)
        {
            AnsiConsole.WriteLine("Searching local virtual machines ...");
            AnsiConsole.WriteLine();

            var vms = vmManager.ListLocalVMs();            
            if (vms.Count == 0)
            {
                AnsiConsole.WriteLine("No virtual machine found on this host: you can create one with the command \"wordslab host vm create\".");
                return 0;
            }

            var runningVMs = vms.Where(vm => vm.IsRunning()).ToList();
            var stoppedVMs = vms.Where(vm => !vm.IsRunning()).ToList();

            // 1. Running VMs
            if (runningVMs.Count > 0)
            {
                AnsiConsole.WriteLine("Running virtual machines:");
                AnsiConsole.WriteLine();

                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Address");
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
                        instance.GetHttpURL(),
                        displayStatus.StartedOn,
                        displayStatus.RunningTime,
                        displayStatus.Processors,
                        displayStatus.Memory,
                        displayStatus.GPU,
                        vm.ClusterDisk.CurrentSizeGB + "GB",
                        vm.DataDisk.CurrentSizeGB + "GB");
                }
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }

            // 2. Stopped VMs
            if (stoppedVMs.Count > 0)
            {
                AnsiConsole.WriteLine("Stopped virtual machines:");
                AnsiConsole.WriteLine();

                var table = new Table();
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
                    var firstInstance = configStore.TryGetFirstVirtualMachineInstance(vm.Name);
                    var lastInstance = configStore.TryGetLastVirtualMachineInstance(vm.Name);
                    if(lastInstance.State == VirtualMachineState.Running)
                    {
                        lastInstance.Killed($"Virtual machine process not found on {(DateTime.Now.ToString("MM/dd/yy HH:mm:ss"))}");
                    }
                    var totalRunningTime = configStore.GetVirtualMachineInstanceTotalRunningTime(vm.Name);
                    table.AddRow(
                        vm.Name,
                        firstInstance==null?"":firstInstance.StartTimestamp.ToString("MM/dd/yy HH:mm:ss"),
                        lastInstance == null || !lastInstance.StopTimestamp.HasValue ? "" : lastInstance.StopTimestamp.Value.ToString("MM/dd/yy HH:mm:ss"),
                        lastInstance == null ? "" : lastInstance.State.ToString().ToLowerInvariant(),
                        totalRunningTime.ToString(@"d\.hh\:mm\:ss"),
                        vm.Config.Spec.Compute.Processors.ToString(),
                        $"{vm.Config.Spec.Compute.MemoryGB}GB",
                        vm.Config.Spec.GPU.ToString(),
                        vm.ClusterDisk.CurrentSizeGB + "GB",
                        vm.DataDisk.CurrentSizeGB + "GB");
                }
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }

            return 0;
        }
    }

    public class VmStartCommand : VmCommand<VmComputeSettings>
    {
        public VmStartCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmComputeSettings vmSettings)
        {
            var vmName = vmSettings.Name;
            if (String.IsNullOrEmpty(vmName))
            {
                AnsiConsole.WriteLine("Virtual machine name argument is missing");
                return 1;
            }

            var vm = vmManager.TryFindLocalVM(vmName);
            if(vm == null)
            {
                AnsiConsole.WriteLine($"Could not find a local virtual machine named: {vmName}");
                return 1;
            }

            if(vm.IsRunning())
            {
                AnsiConsole.WriteLine($"Virtual machine {vmName} is already running");
                return 0;
            }

            AnsiConsole.WriteLine($"Starting virtual machine {vmName} ...");
            AnsiConsole.WriteLine();

            // Then start the VM with the merged config properties
            VirtualMachineInstance vmi = null;
            try
            {
                var computeSpec = vmSettings.GetComputeStartArguments(vm.Config);
                var gpuSpec = vmSettings.GetGPUStartArguments(vm.Config);

                AnsiConsole.WriteLine($"- {computeSpec.Processors} processors");
                AnsiConsole.WriteLine($"- {computeSpec.MemoryGB} GB memory");
                if(gpuSpec.GPUCount > 0)
                {
                    AnsiConsole.WriteLine($"- {gpuSpec.ToString()}");
                }
                AnsiConsole.WriteLine();

                vmi = vm.Start(computeSpec, gpuSpec);
            }
            catch(Exception ex)
            {
                AnsiConsole.WriteLine($"Failed to start virtual machine {vmName}:");
                AnsiConsole.WriteLine(ex.Message);
                AnsiConsole.WriteLine();
                return -1;
            }

            AnsiConsole.WriteLine($"Virtual machine {vmName} started:");
            AnsiConsole.WriteLine($"- {vmi.GetHttpURL()}");
            AnsiConsole.WriteLine($"- {vmi.GetHttpsURL()}");
            AnsiConsole.WriteLine();
            return 0;
        }
    }

    public class VmStopCommand : VmCommand<VmNameSettings>
    {
        public VmStopCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.Name;
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
                AnsiConsole.WriteLine($"Virtual machine {vmName} is already stopped");
                return 0;
            }

            AnsiConsole.WriteLine($"Stopping virtual machine {vmName} ...");
            AnsiConsole.WriteLine();

            var vmInstance = vm.RunningInstance;

            try 
            { 
                vm.Stop();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine($"Failed to stop virtual machine {vmName}:");
                AnsiConsole.WriteLine(ex.Message);
                AnsiConsole.WriteLine();
                return -1;
            }

            AnsiConsole.WriteLine($"Virtual machine {vmName} stopped.");
            AnsiConsole.WriteLine($"Running time: {(vmInstance.StopTimestamp.Value.Subtract(vmInstance.StartTimestamp).ToString(@"d\.hh\:mm\:ss"))}");
            AnsiConsole.WriteLine();
            return 0;
        }
    }

    public class VmStatusCommand : VmCommand<VmNameSettings>
    {
        public VmStatusCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.Name;
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

            AnsiConsole.WriteLine($"Virtual machine {vmName} status:");
            AnsiConsole.WriteLine();
            if (vm.IsRunning())
            {
                var instance = vm.RunningInstance;
                var displayStatus = instance.GetDisplayStatus();
                AnsiConsole.WriteLine("- state: running");
                AnsiConsole.WriteLine($"- started on: {displayStatus.StartedOn}");
                AnsiConsole.WriteLine($"- running since: {displayStatus.RunningTime}");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine($"- processors: {displayStatus.Processors}");
                AnsiConsole.WriteLine($"- memory: {displayStatus.Memory}");
                AnsiConsole.WriteLine($"- GPU: {displayStatus.GPU}");
                AnsiConsole.WriteLine($"- cluster disk: {vm.ClusterDisk.CurrentSizeGB} GB");
                AnsiConsole.WriteLine($"- data disk: {vm.DataDisk.CurrentSizeGB} GB");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine($"-> {instance.GetHttpURL()}");
                AnsiConsole.WriteLine($"-> {instance.GetHttpsURL()}");
            }
            else
            {
                var vmInstance = configStore.TryGetLastVirtualMachineInstance(vmName);
                if (vmInstance == null)
                {
                    AnsiConsole.WriteLine("- state: never started");
                }
                else
                {
                    var displayStatus = vmInstance.GetDisplayStatus();
                    AnsiConsole.WriteLine($"- state: {vmInstance.State.ToString().ToLowerInvariant()}");
                    AnsiConsole.WriteLine($"- last stopped on: {displayStatus.StoppedOn}");
                    AnsiConsole.WriteLine($"- last running time: {displayStatus.RunningTime}");
                    AnsiConsole.WriteLine();
                    AnsiConsole.WriteLine($"- processors: {vm.Config.Spec.Compute.Processors}");
                    AnsiConsole.WriteLine($"- memory: {vm.Config.Spec.Compute.MemoryGB} GB");
                    if (vm.Config.Spec.GPU.GPUCount > 0)
                    {
                        AnsiConsole.WriteLine($"- GPU: {vm.Config.Spec.GPU.ToString()}");
                    }
                    AnsiConsole.WriteLine($"- cluster disk: {vm.ClusterDisk.CurrentSizeGB} GB");
                    AnsiConsole.WriteLine($"- data disk: {vm.DataDisk.CurrentSizeGB} GB");
                }
            }
            AnsiConsole.WriteLine();

            return 0;
        }
    }

    public class VmAdviseCommand : VmCommand<VmNameSettings>
    {
        public VmAdviseCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            AnsiConsole.WriteLine("Analysing host machine hardware ...");
            AnsiConsole.WriteLine();

            var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
            
            DisplayRecommendedSpec("Minimum", vmSpecs.MinimumVMSpec, vmSpecs.MinimunVMSpecErrorMessage);
            if (vmSpecs.MinimumVMSpecIsSupportedOnThisMachine)
            {
                DisplayRecommendedSpec("Maximum", vmSpecs.MaximumVMSpecOnThisMachine, null);
            }
            DisplayRecommendedSpec("Recommended", vmSpecs.RecommendedVMSpec, vmSpecs.RecommendedVMSpecErrorMessage);

            var tcpPortsInUse = Network.GetAllTcpPortsInUse();
            var sshPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_SSH_PORT);
            var kubernetesPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_Kubernetes_PORT);
            var httpPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_HttpIngress_PORT);
            var httpsPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_HttpsIngress_PORT);

            string portAvailable = "available";
            string portInUse = "already in use";
            AnsiConsole.WriteLine("Checking if the default Host ports (VM port forward on localhost) are already used:");
            AnsiConsole.WriteLine($"- connect to the virtual machine through SSH: {VMRequirements.DEFAULT_HOST_SSH_PORT} -> {(sshPortAvailable ? portAvailable : portInUse)}");
            AnsiConsole.WriteLine($"- connect to the Kubernetes cluster inside the VM: {VMRequirements.DEFAULT_HOST_Kubernetes_PORT} -> {(kubernetesPortAvailable ? portAvailable : portInUse)}");
            AnsiConsole.WriteLine($"- connect to the HTTP services inside the VM: {VMRequirements.DEFAULT_HOST_HttpIngress_PORT} -> {(httpPortAvailable ? portAvailable : portInUse)}");
            AnsiConsole.WriteLine($"- connect to the HTTPS services inside the VM: {VMRequirements.DEFAULT_HOST_HttpsIngress_PORT} -> {(httpPortAvailable ? portAvailable : portInUse)}");
            AnsiConsole.WriteLine("");

            return 0;
        }

        private static void DisplayRecommendedSpec(string specName, VirtualMachineSpec spec, string errMsg)
        {
            AnsiConsole.WriteLine($"{specName} virtual machine configuration :");
            if (!String.IsNullOrEmpty(errMsg))
            {
                AnsiConsole.WriteLine($"=> {errMsg}");
            }
            else
            {
                AnsiConsole.WriteLine($"- number of processors: {spec.Compute.Processors}");
                AnsiConsole.WriteLine($"- memory size in GB: {spec.Compute.MemoryGB}");
                if (!String.IsNullOrEmpty(spec.GPU.ModelName))
                {
                    AnsiConsole.WriteLine($"- GPU model: {spec.GPU.ModelName} {spec.GPU.MemoryGB}GB");
                }
                AnsiConsole.WriteLine($"- cluster disk size in GB: {spec.Storage.ClusterDiskSizeGB}" + (spec.Storage.ClusterDiskIsSSD ? " (SSD)" : ""));
                AnsiConsole.WriteLine($"- data disk size in GB: {spec.Storage.DataDiskSizeGB}" + (spec.Storage.DataDiskIsSSD ? " (SSD)" : ""));
            }
            AnsiConsole.WriteLine();
        }
    }

    public class VmCreateCommand : VmCommand<VmPresetSettings>
    {
        public VmCreateCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmPresetSettings vmSettings)
        {
            var vmName = vmSettings.Name;
            if (String.IsNullOrEmpty(vmName))
            {
                AnsiConsole.WriteLine("Virtual machine name argument is missing");
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

            var installUI = new ConsoleProcessUI();
            var vmConfig = AsyncUtil.RunSync(() => vmManager.CreateLocalVMConfig(vmName, vmPresetSpec, configStore.HostMachineConfig, installUI));
            if(vmConfig == null)
            {
                return -1;
            }

            var vm = AsyncUtil.RunSync(() => vmManager.CreateLocalVM(vmConfig, installUI));
            if (vm == null)
            {
                return -1;
            }

            return 0;
        }        
    }

    public class VmResizeCommand : VmCommand<VmNameSettings>
    {
        public VmResizeCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            AnsiConsole.WriteLine("ERROR: host vm resize command not yet implemented");
            return -1;
        }
    }
        
    public class VmDeleteCommand : VmCommand<VmNameSettings>
    {
        public VmDeleteCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.Name;
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

            var installUI = new ConsoleProcessUI();
            var success = AsyncUtil.RunSync(() => vmManager.DeleteLocalVM(vmName, installUI));
            if (!success)
            {
                return -1;
            }

            return 0;
        }
    }
}
