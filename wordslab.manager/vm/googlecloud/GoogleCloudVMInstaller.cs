using wordslab.manager.config;
using wordslab.manager.storage;

namespace wordslab.manager.vm.googlecloud
{
    public static class GoogleCloudVMInstaller
    {
        // Note: before calling this method
        // - you must configure HostStorage directories location
        // - you must ask the user if they want to use a GPU
        public static async Task<bool> CheckAndInstallHostMachineRequirements(HostStorage hostStorage, InstallProcessUI ui)
        {
            throw new NotImplementedException();
        }

        public static async Task<VirtualMachine> CreateVirtualMachine(VirtualMachineConfig vmConfig, ConfigStore configStore, HostStorage hostStorage, InstallProcessUI ui)
        {
            throw new NotImplementedException();
        }

        public static async Task<bool> DeleteVirtualMachine(string vmName, ConfigStore configStore, HostStorage hostStorage, InstallProcessUI ui)
        {
            throw new NotImplementedException();
        }
    }
}
