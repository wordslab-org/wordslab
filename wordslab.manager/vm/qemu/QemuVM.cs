using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuVM : VirtualMachine
    {        
        public static QemuVM LocalInstance(HostStorage hostStorage) { return new QemuVM(hostStorage); }

        private HostStorage hostStorage;

        internal QemuVM(HostStorage hostStorage)
        {
            this.hostStorage = hostStorage;
        }

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
