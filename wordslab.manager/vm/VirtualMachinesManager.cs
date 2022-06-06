using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.storage.config;
using wordslab.manager.vm.qemu;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.vm
{
    public class VirtualMachinesManager
    {
        private HostStorage hostStorage;
        private ConfigStore configStore;

        public VirtualMachinesManager(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
        }

        public List<VirtualMachine> ListLocalVMs()
        {
            // List configs in database
            var localVmType = OS.IsWindows ? VirtualMachineType.Wsl : VirtualMachineType.Qemu;
            var vmConfigsDict = new Dictionary<string,VirtualMachineConfig>(); 
            foreach(var vmConfig in configStore.VirtualMachines.Where(vm => vm.Type == localVmType))
            {
                vmConfigsDict.Add(vmConfig.Name, vmConfig);
            }
            var vmConfigNames = new HashSet<string>(vmConfigsDict.Values.Select(vm => vm.Name));

            // List vms found on disk
            List<VirtualMachine> vms = null;
            if (OS.IsWindows)
            {
                vms = WslVM.ListLocalVMs(hostStorage);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                vms = QemuVM.ListLocalVMs(hostStorage);
            }

            // Remove configs not found on disk from database 
            foreach (var configNameNotFoundOnDisk in vmConfigNames.Except(vms.Select(vm => vm.Name)))
            {
                configStore.RemoveVirtualMachineConfig(vmConfigsDict[configNameNotFoundOnDisk].Name);
            }

            // Restrict list of vms to configs registered in database
            vms = vms.Where(vm => vmConfigsDict.ContainsKey(vm.Name)).ToList();

            // Merge configs and vms properties to sync disk and database
            foreach(var vm in vms)
            {
                var vmConfig = vmConfigsDict[vm.Name];
                if(vm.Processors > 0)
                {
                    vmConfig.Processors = vm.Processors;
                }
                else
                {
                    vm.Processors = vmConfig.Processors;
                }
                if (vm.MemoryGB > 0)
                {
                    vmConfig.MemoryGB = vm.MemoryGB;
                }
                else
                {
                    vm.MemoryGB = vmConfig.MemoryGB;
                }
                if (!String.IsNullOrEmpty(vm.GPUModel))
                {
                    vmConfig.GPUModel = vm.GPUModel;
                    vmConfig.GPUMemoryGB = vm.GPUMemoryGB;
                }
                else
                {
                    vm.GPUModel = vmConfig.GPUModel;
                    vm.GPUMemoryGB = vmConfig.GPUMemoryGB;
                }
                vmConfig.VmDiskSizeGB = vm.OsDisk.MaxSizeGB;
                vmConfig.VmDiskIsSSD = vm.OsDisk.IsSSD;
                vmConfig.ClusterDiskSizeGB = vm.ClusterDisk.MaxSizeGB;
                vmConfig.ClusterDiskIsSSD = vm.ClusterDisk.IsSSD;
                vmConfig.DataDiskSizeGB = vm.DataDisk.MaxSizeGB;
                vmConfig.DataDiskIsSSD = vm.DataDisk.IsSSD;
                if (vm.Endpoint != null)
                {
                    vmConfig.HostSSHPort = vm.Endpoint.SSHPort;
                    vmConfig.HostKubernetesPort = vm.Endpoint.KubernetesPort;
                    vmConfig.HostHttpIngressPort = vm.Endpoint.HttpIngressPort;
                    vm.RequestedSSHPort = vm.Endpoint.SSHPort;
                    vm.RequestedKubernetesPort = vm.Endpoint.KubernetesPort;
                    vm.RequestedHttpIngressPort = vm.Endpoint.HttpIngressPort;
                }
                else
                {
                    vm.RequestedSSHPort = vmConfig.HostSSHPort;
                    vm.RequestedKubernetesPort = vmConfig.HostKubernetesPort;
                    vm.RequestedHttpIngressPort = vmConfig.HostHttpIngressPort;
                }
            }

            configStore.SaveChanges();

            return vms;
        }

        public VirtualMachine TryFindLocalVM(string vmName)
        {
            VirtualMachine vm = null;
            if (OS.IsWindows)
            {
                vm = WslVM.TryFindByName(vmName, hostStorage);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                vm = QemuVM.TryFindByName(vmName, hostStorage);
            }
            return vm;
        }

        public async Task<VirtualMachine> CreateLocalVM(VirtualMachineSpec vmSpec, InstallProcessUI installUI)
        {
            VirtualMachine vm = null;
            if (OS.IsWindows)
            {
                vm = await WslVMInstaller.Install(vmSpec, hostStorage, installUI);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                vm = await QemuVMInstaller.Install(vmSpec, hostStorage, installUI);
            }
            return vm;
        }

        public async Task<bool> DeleteLocalVM(string vmName, InstallProcessUI installUI)
        {
            bool uninstallSuccess = false;
            if (OS.IsWindows)
            {
                uninstallSuccess = await WslVMInstaller.Uninstall(vmName, hostStorage, installUI);               
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                uninstallSuccess = await QemuVMInstaller.Uninstall(vmName, hostStorage, installUI);
            }
            return uninstallSuccess;
        }
    }
}
