using wordslab.manager.config;
using wordslab.manager.storage;

namespace wordslab.manager.vm.googlecloud
{
    public class GoogleCloudVM : VirtualMachine
    {
        public static List<VirtualMachine> ListLocalVMs(ConfigStore configStore, HostStorage storage)
        {
            throw new NotImplementedException();
        }

        public static VirtualMachine FindByName(string vmName, ConfigStore configStore, HostStorage storage)
        {
            throw new NotImplementedException();
        }

        internal GoogleCloudVM(VirtualMachineConfig vmConfig, VirtualDisk clusterDisk, VirtualDisk dataDisk, ConfigStore configStore, HostStorage storage)
            : base(vmConfig, clusterDisk, dataDisk, configStore, storage)
        {
            if(vmConfig.VmProvider != VirtualMachineProvider.GoogleCloud)
            {
                throw new ArgumentException("VmProvider should be GoogleCloud");
            }

            // Initialize the running state
            IsRunning();
        }

        public override bool IsRunning()
        {
            throw new NotImplementedException();
        }

        public override VirtualMachineInstance Start(ComputeSpec computeStartArguments, GPUSpec gpuStartArguments)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
