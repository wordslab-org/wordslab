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
            // List configs found in database
            var localVmType = OS.IsWindows ? VirtualMachineType.Wsl : VirtualMachineType.Qemu;
            var vmConfigs = configStore.VirtualMachines.Where(vm => vm.Type == localVmType);            
            var vmNamesInDatabase = new HashSet<string>(vmConfigs.Select(config => config.Name));

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
            if(vm != null)
            {
                var vmConfig = new VirtualMachineConfig(vm);
                configStore.AddVirtualMachineConfig(vmConfig);
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
