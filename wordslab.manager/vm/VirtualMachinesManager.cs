using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.vm.qemu;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.vm
{
    public class VirtualMachinesManager
    {
        private HostStorage hostStorage;
        private ConfigStore configStore;
        private HostMachineConfig machineConfig;

        public VirtualMachinesManager(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
            this.machineConfig = configStore.HostMachineConfig;
        }

        public async Task<HostMachineConfig> ConfigureHostMachine(bool userWantsVMWithGPU, InstallProcessUI installUI)
        {
            var cmd1 = installUI.DisplayCommandLaunch($"Checking if host machine {OS.GetMachineName()} is already configured");
            if (machineConfig != null)
            {
                installUI.DisplayCommandResult(cmd1, true);
                return machineConfig;
            }
            else
            {
                installUI.DisplayCommandResult(cmd1, false);
            }
            if (OS.IsWindows)
            {
                machineConfig = await WslVMInstaller.ConfigureHostMachine(hostStorage, installUI);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                machineConfig = await QemuVMInstaller.ConfigureHostMachine(hostStorage, installUI);
            }
            return machineConfig;
        }

        public async Task<VirtualMachine> CreateLocalVM(VirtualMachineConfig vmConfig, InstallProcessUI installUI)
        {
            VirtualMachine vm = null;
            if (OS.IsWindows)
            {
                vm = await WslVMInstaller.CreateVirtualMachine(vmConfig, configStore, hostStorage, installUI);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                vm = await QemuVMInstaller.CreateVirtualMachine(vmConfig, configStore, hostStorage, installUI);
            }
            return vm;
        }

        public List<VirtualMachine> ListLocalVMs()
        {
            // List vms found on disk
            List<VirtualMachine> vms = null;
            if (OS.IsWindows)
            {
                vms = WslVM.ListLocalVMs(configStore, hostStorage);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                vms = QemuVM.ListLocalVMs(configStore, hostStorage);
            }

            /*            
            // List configs found in database
            var localVmType = OS.IsWindows ? VirtualMachineType.Wsl : VirtualMachineType.Qemu;
            var vmConfigs = configStore.VirtualMachines.Where(vm => vm.Type == localVmType);            
            var vmNamesInDatabase = new HashSet<string>(vmConfigs.Select(config => config.Name));
 
            var vmNamesOnDisk = new HashSet<string>(vms.Select(vm => vm.Name));

            // The TRUTH is on disk => align the database

            // Remove configs found in the database but not found on disk
            foreach (var vmNameNotFoundOnDisk in vmNamesInDatabase.Except(vmNamesOnDisk))
            {
                configStore.RemoveVirtualMachineConfig(vmNameNotFoundOnDisk);
            }

            // Add configs found on disk but not found in the databse
            foreach (var vmNotFoundInDatabase in vmNamesOnDisk.Except(vmNamesInDatabase))
            {
                var vmToAdd = vms.First(vm => vm.Name == vmNotFoundInDatabase);
                var vmConfigToAdd = new VirtualMachineConfig(vmToAdd);
                configStore.AddVirtualMachineConfig(vmConfigToAdd);
            }

            // Merge configs and vms properties to sync disk and database
            foreach (var vmConfig in vmConfigs)
            {
                var vmOnDisk = vms.First(vm => vm.Name == vmConfig.Name);
                vmConfig.UpdateFromVM(vmOnDisk);
            }

            // Update configs in database
            configStore.SaveChanges();
            */

            return vms;
        }

        public VirtualMachine TryFindLocalVM(string vmName)
        {
            VirtualMachine vm = null;
            try
            {
                if (OS.IsWindows)
                {
                    vm = WslVM.FindByName(vmName, configStore, hostStorage);
                }
                else if (OS.IsLinux || OS.IsMacOS)
                {
                    vm = QemuVM.FindByName(vmName, configStore, hostStorage);
                }
            } 
            catch(Exception) { }
            return vm;
        }

        public async Task<bool> DeleteLocalVM(string vmName, InstallProcessUI installUI)
        {
            bool uninstallSuccess = false;
            if (OS.IsWindows)
            {
                uninstallSuccess = await WslVMInstaller.DeleteVirtualMachine(vmName, configStore, hostStorage, installUI);               
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                uninstallSuccess = await QemuVMInstaller.DeleteVirtualMachine(vmName, configStore, hostStorage, installUI);
            }
            if (uninstallSuccess)
            {                
                configStore.RemoveVirtualMachineConfig(vmName);
            }
            return uninstallSuccess;
        }
    }
}
