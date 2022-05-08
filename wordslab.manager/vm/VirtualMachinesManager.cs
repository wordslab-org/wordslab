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

        public VirtualMachinesManager(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
            RefreshState();
        }

        private const string localVMName = "localvm"; 

        private VirtualMachine localVM;

        public VirtualMachine LocalVM { get { return localVM; } }

        public void RefreshState()
        {
            if(OS.IsWindows)
            {
                localVM = WslVM.TryFindByName(localVMName, hostStorage);
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                localVM = QemuVM.TryFindByName(localVMName, hostStorage);
            }
        }

        public bool IsLocalVMInstalled
        {
            get { return localVM != null; }
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

            localVM = vm;
            return vm;
        }

        public async Task<bool> DeleteLocalVM(VirtualMachineSpec vmSpec, InstallProcessUI installUI)
        {
            bool uninstallSuccess = false;
            if (OS.IsWindows)
            {
                uninstallSuccess = await WslVMInstaller.Uninstall(localVMName, hostStorage, installUI);               
            }
            else if (OS.IsLinux || OS.IsMacOS)
            {
                uninstallSuccess = await QemuVMInstaller.Uninstall(localVMName, hostStorage, installUI);
            }

            if (uninstallSuccess)
            {
                localVM = null;
            }
            return uninstallSuccess;
        }
    }
}
