using wordslab.manager.storage;

namespace wordslab.manager.vm.googlecloud
{
    public class GoogleCloudVM : VirtualMachine
    {
        internal GoogleCloudVM(string name, int processors, int memoryGB, VirtualDisk osDisk, VirtualDisk clusterDisk, VirtualDisk dataDisk, HostStorage storage)
            : base(name, processors, memoryGB, osDisk, clusterDisk, dataDisk, storage)
        { }

        public override bool IsRunning()
        {
            throw new NotImplementedException();
        }

        public override VMEndpoint Start(VirtualMachineSpec vmSpec)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
