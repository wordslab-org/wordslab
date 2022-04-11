using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuVM : VirtualMachine
    {        
        internal QemuVM(string name, int processors, int memoryGB, int osDiskSizeGB, int clusterDiskSizeGB, int dataDiskSizeGB, HostStorage storage) 
            : base(name, processors, memoryGB, osDiskSizeGB, clusterDiskSizeGB, dataDiskSizeGB, storage) 
        { }

        public override bool IsRunning()
        {
            throw new NotImplementedException();
        }

        public override VMEndpoint Start()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
