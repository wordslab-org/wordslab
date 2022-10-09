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
        [CommandArgument(0, "[Name]")]
        public string MachineName { get; init; }
    }

    public class VmComputeSettings : VmNameSettings
    {
        [Description("Use minimum compute and storage config")]
        [CommandOption("--minmum")]
        public bool? UseMinimumSpecs { get; init; }
        [Description("Use recommended compute and storage config")]
        [CommandOption("--recommended")]
        public bool? UseRecommendedSpecs { get; init; }
        [Description("Use maximum compute and storage config")]
        [CommandOption("--maximum")]
        public bool? UseMaximumSpecs { get; init; }

        [Description("Number of processors")]
        [CommandOption("--proc")]
        [DefaultValue(VMRequirements.MIN_VM_PROCESSORS)]
        public int? Processors { get; init; }
        [Description("Memory size in GB")]
        [CommandOption("--mem")]
        [DefaultValue(VMRequirements.MIN_VM_MEMORY_GB)]
        public int? MemoryGB { get; init; }
        [Description("GPU device index")]
        [CommandOption("--gpu")]
        public int? GPUDevice { get; init; }

        [Description("Host port to connect to the virtual machine through SSH (port forward)")]
        [CommandOption("--sshport")]
        [DefaultValue(VMRequirements.DEFAULT_HOST_SSH_PORT)]
        public int? SSHPort { get; init; }
        [Description("Host port to connect to the Kubernetes cluster inside the virtual machine (port forward)")]
        [CommandOption("--kubeport")]
        [DefaultValue(VMRequirements.DEFAULT_HOST_Kubernetes_PORT)]
        public int? KubernetesPort { get; init; }
        [Description("Host port to connect to the http services inside the virtual machine (port forward)")]
        [CommandOption("--httpport")]
        [DefaultValue(VMRequirements.DEFAULT_HOST_HttpIngress_PORT)]
        public int? HttpIngressPort { get; init; }

        public string ApplyToVMConfig(VirtualMachineConfig vmConfig, HostStorage hostStorage)
        {
            var vmSettings = this;

            // Step 2 : override compute values with min, rec or max specs 
            if (vmSettings.UseMinimumSpecs.HasValue || vmSettings.UseRecommendedSpecs.HasValue || vmSettings.UseMaximumSpecs.HasValue)
            {
                var vmSpecs = VMRequirements.GetRecommendedVMSpecs(hostStorage);

                VirtualMachineSpec targetSpec = null;
                if (vmSettings.UseMinimumSpecs.HasValue && vmSettings.UseMinimumSpecs.Value)
                {
                    if (!vmSpecs.MinimumVMSpecIsSupportedOnThisMachine)
                    {
                        var errorMessage = $"The host machine is not powerful enough to run a virtual machine with the minimum config: {vmSpecs.MinimunVMSpecErrorMessage}";
                        return errorMessage;
                    }
                    targetSpec = vmSpecs.MinimumVMSpec;
                }
                if (vmSettings.UseRecommendedSpecs.HasValue && vmSettings.UseRecommendedSpecs.Value)
                {
                    if (!vmSpecs.RecommendedVMSpecIsSupportedOnThisMachine)
                    {
                        var errorMessage = $"The host machine is not powerful enough to run a virtual machine with the recommended config: {vmSpecs.RecommendedVMSpecErrorMessage}";
                        return errorMessage;
                    }
                    targetSpec = vmSpecs.RecommendedVMSpec;
                }
                if (vmSettings.UseMaximumSpecs.HasValue && vmSettings.UseMaximumSpecs.Value)
                {
                    targetSpec = vmSpecs.MaximumVMSpecOnThisMachine;
                }

                vmConfig.Spec.Compute.Processors = targetSpec.Compute.Processors;
                vmConfig.Spec.Compute.MemoryGB = targetSpec.Compute.MemoryGB;
                if (string.IsNullOrEmpty(targetSpec.GPU.ModelName))
                {
                    vmConfig.Spec.GPU.ModelName = null;
                }
                else if (string.IsNullOrEmpty(vmConfig.Spec.GPU.ModelName))
                {
                    vmConfig.Spec.GPU.ModelName = vmSpecs.MaximumVMSpecOnThisMachine.GPU.ModelName;
                    vmConfig.Spec.GPU.MemoryGB = vmSpecs.MaximumVMSpecOnThisMachine.GPU.MemoryGB;
                }
            }

            // Step 3 : apply all the values explicitly set on the command line
            if (vmSettings.Processors.HasValue)
            {
                vmConfig.Spec.Compute.Processors = vmSettings.Processors.Value;
            }
            if (vmSettings.MemoryGB.HasValue)
            {
                vmConfig.Spec.Compute.MemoryGB = vmSettings.MemoryGB.Value;
            }
            if (vmSettings.GPUDevice.HasValue)
            {
                int gpuDevice = vmSettings.GPUDevice.Value;
                var gpusInfo = Compute.GetNvidiaGPUsInfo();
                if (gpuDevice < 1 || gpuDevice > gpusInfo.Count)
                {
                    var errorMessage = $"Invalid GPU selected: requested GPU device {gpuDevice} while {gpusInfo.Count} GPUs were detected on this machine";
                    return errorMessage;
                }
                var selectedGPU = gpusInfo[gpuDevice - 1];
                vmConfig.Spec.GPU.ModelName = selectedGPU.ModelName;
                vmConfig.Spec.GPU.MemoryGB = selectedGPU.MemoryMB / 1024;
            }
            if (vmSettings.SSHPort.HasValue)
            {
                vmConfig.HostSSHPort = vmSettings.SSHPort.Value;
            }
            if (vmSettings.KubernetesPort.HasValue)
            {
                vmConfig.HostKubernetesPort = vmSettings.KubernetesPort.Value;
            }
            if (vmSettings.HttpIngressPort.HasValue)
            {
                vmConfig.HostHttpIngressPort = vmSettings.HttpIngressPort.Value;
            }

            return null;
        }
    }

    public class VmConfigSettings : VmComputeSettings
    {
        [Description("Cluster disk size in GB")]
        [CommandOption("--clusterdisk")]
        [DefaultValue(VMRequirements.REC_VM_CLUSTERDISK_GB)]
        public int? ClusterDiskSizeGB { get; init; }

        [Description("Data disk size in GB")]
        [CommandOption("--datadisk")]
        [DefaultValue(VMRequirements.REC_VM_DATADISK_GB)]
        public int? DataDiskSizeGB { get; init; }
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
            var vms = vmManager.ListLocalVMs();            

            DisplayVmList(vms);
            return 0;
        }

        internal static void DisplayVmList(List<VirtualMachine> vms)
        {
            if (vms.Count == 0)
            {
                AnsiConsole.WriteLine("No virtual machine found on this host: you can create one with the command \"wordslab host vm create\".");
                return;
            }

            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("State");
            table.AddColumn("Http port");
            table.AddColumn("Processors");
            table.AddColumn("Memory");
            table.AddColumn("Os disk");
            table.AddColumn("Cluster disk");
            table.AddColumn("Data disk");
            foreach (var vm in vms)
            {
                table.AddRow( vm.Name,
                    vm.IsRunning() ? "running" : "stopped",
                    vm.Config.HostHttpIngressPort.ToString(),
                    vm.IsRunning() ? vm.RunningInstance.ComputeStartArguments.Processors.ToString() : vm.Config.Spec.Compute.Processors.ToString(),
                    vm.IsRunning() ? vm.RunningInstance.ComputeStartArguments.MemoryGB.ToString() : vm.Config.Spec.Compute.MemoryGB.ToString(),
                    vm.ClusterDisk.MaxSizeGB + " GB",
                    vm.DataDisk.MaxSizeGB + " GB"
                    );
            }
            AnsiConsole.Write(table);
        }
    }

    public class VmStartCommand : VmCommand<VmComputeSettings>
    {
        public VmStartCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmComputeSettings vmSettings)
        {
            var vmName = vmSettings.MachineName;
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

            // Step 1 : initialize vm config with the values from the last run
            var vmConfig = configStore.TryGetVirtualMachineConfig(vmName);
            if(vmConfig == null)
            {
                AnsiConsole.WriteLine($"Could not find the configuration of the virtual machine: {vmName}");
                return 1;
            }

            // Step 2 & 3 : override with command line parameters
            var errorMessage = vmSettings.ApplyToVMConfig(vmConfig, hostStorage);
            if(errorMessage != null)
            {
                AnsiConsole.WriteLine(errorMessage);
                return 1;
            }

            // Then start the VM with the merged config properties
            vm.Start(vmConfig.Spec.Compute, vmConfig.Spec.GPU);

            // Save the new config to the database
            configStore.SaveChanges();

            return 0;
        }
    }

    public class VmStopCommand : VmCommand<VmNameSettings>
    {
        public VmStopCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.MachineName;
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

            vm.Stop();
            return 0;
        }
    }

    public class VmStatusCommand : VmCommand<VmNameSettings>
    {
        public VmStatusCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmName = settings.MachineName;
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

            var vms = new List<VirtualMachine>();
            vms.Add(vm);

            VmListCommand.DisplayVmList(vms);
            return 0;
        }
    }

    public class VmAdviseCommand : VmCommand<VmNameSettings>
    {
        public VmAdviseCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmNameSettings settings)
        {
            var vmSpecs = VMRequirements.GetRecommendedVMSpecs(hostStorage);
            
            DisplayRecommendedSpec("Minimum", vmSpecs.MinimumVMSpec, vmSpecs.MinimunVMSpecErrorMessage);
            DisplayRecommendedSpec("Recommended", vmSpecs.RecommendedVMSpec, vmSpecs.RecommendedVMSpecErrorMessage);
            if (vmSpecs.MinimumVMSpecIsSupportedOnThisMachine)
            {
                DisplayRecommendedSpec("Maximum", vmSpecs.MaximumVMSpecOnThisMachine, null);
            }

            var tcpPortsInUse = Network.GetAllTcpPortsInUse();
            var sshPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_SSH_PORT);
            var kubernetesPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_Kubernetes_PORT);
            var httpPortAvailable = !tcpPortsInUse.Contains(VMRequirements.DEFAULT_HOST_HttpIngress_PORT);

            string portAvailable = "available";
            string portInUse = "already in use";
            AnsiConsole.WriteLine("Checking if the default Host ports (VM port forward on localhost) are already used:");
            AnsiConsole.WriteLine($"- connect to the virtual machine through SSH: {VMRequirements.DEFAULT_HOST_SSH_PORT} -> {(sshPortAvailable ? portAvailable : portInUse)}");
            AnsiConsole.WriteLine($"- connect to the Kubernetes cluster inside the VM: {VMRequirements.DEFAULT_HOST_Kubernetes_PORT} -> {(kubernetesPortAvailable ? portAvailable : portInUse)}");
            AnsiConsole.WriteLine($"- connect to the HTTP services inside the VM: {VMRequirements.DEFAULT_HOST_HttpIngress_PORT} -> {(httpPortAvailable ? portAvailable : portInUse)}");
            AnsiConsole.WriteLine("");
            if (!sshPortAvailable || !kubernetesPortAvailable || !httpPortAvailable)
            {
                AnsiConsole.WriteLine("You can add the following options to the wordlab host vm create or start commands:");
                var portOptions = "";
                if (!sshPortAvailable)
                {
                    portOptions += $"--sshport {Network.GetNextAvailablePort(VMRequirements.DEFAULT_HOST_SSH_PORT, tcpPortsInUse)} ";
                }
                if (!kubernetesPortAvailable)
                {
                    portOptions += $"--kubeport {Network.GetNextAvailablePort(VMRequirements.DEFAULT_HOST_Kubernetes_PORT, tcpPortsInUse)} ";
                }
                if (!httpPortAvailable)
                {
                    portOptions += $"--httpport {Network.GetNextAvailablePort(VMRequirements.DEFAULT_HOST_HttpIngress_PORT, tcpPortsInUse)}";
                }
                AnsiConsole.WriteLine(portOptions);
            }

            return 0;
        }

        private static void DisplayRecommendedSpec(string specName, VirtualMachineSpec spec, string errMsg)
        {
            AnsiConsole.WriteLine($"{specName} Virtual Machine configuration :");
            if (!String.IsNullOrEmpty(errMsg))
            {
                AnsiConsole.WriteLine($"=> {errMsg}");
            }
            else
            {
                AnsiConsole.WriteLine($"- command: wordslab host vm create --{specName.ToLowerInvariant()}");
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

    public class VmCreateCommand : VmCommand<VmConfigSettings>
    {
        public VmCreateCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmConfigSettings vmSettings)
        {
            /*
            // Step 1 : initialize vm config with default values
            var vmSpec = new VirtualMachineSpec();
            
            // Step 2 & 3 : override with command line parameters
            var errorMessage = vmSettings.ApplyToVMConfig(vmSpec, hostStorage);
            if (errorMessage != null)
            {
                AnsiConsole.WriteLine(errorMessage);
                return 1;
            }*
            if(vmSettings.ClusterDiskSizeGB.HasValue) vmSpec.ClusterDiskSizeGB = vmSettings.ClusterDiskSizeGB.Value;
            if(vmSettings.DataDiskSizeGB.HasValue) vmSpec.DataDiskSizeGB = vmSettings.DataDiskSizeGB.Value;

            var installUI = new ConsoleProcessUI();
            var vm = AsyncUtil.RunSync(() => vmManager.CreateLocalVM(vmSpec, installUI));
            if (vm == null)
            {
                return 1;
            }

            // Save the new virtual machine config in the database
            configStore.AddVirtualMachineConfig(new VirtualMachineConfig(vm));
            */
            throw new NotImplementedException();

            return 0;
        }        
    }

    public class VmResizeCommand : VmCommand<VmConfigSettings>
    {
        public VmResizeCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] VmConfigSettings settings)
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
            var vmName = settings.MachineName;
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
            var success = AsyncUtil.RunSync(() => vmManager.DeleteLocalVM(vm.Name, installUI));
            if (!success)
            {
                return 1;
            }

            // Remove the virtual machine from the database
            configStore.RemoveVirtualMachineConfig(vm.Name);

            return 0;
        }
    }
}
