using wordslab.manager.storage;
using wordslab.manager.storage.config;

namespace wordslab.manager.vm.googlecloud
{
    public class GoogleCloudVM : VirtualMachine
    {
        internal GoogleCloudVM(string name, int processors, int memoryGB, VirtualDisk osDisk, VirtualDisk clusterDisk, VirtualDisk dataDisk, HostStorage storage)
            : base(name, processors, memoryGB, osDisk, clusterDisk, dataDisk, storage)
        {
            Type = VirtualMachineType.GoogleCloud;
        }

        public override bool IsRunning()
        {
            throw new NotImplementedException();
        }

        public override VirtualMachineEndpoint Start(VirtualMachineConfig vmConfig)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
