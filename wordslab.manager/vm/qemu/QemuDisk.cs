using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuDisk : VirtualDisk
    {        
        public static VirtualDisk CreateFromOSImage(string vmName, string osImagePath, string userDataPath, string metaDataPath, int totalSizeGB, HostStorage storage)
        {
            throw new NotImplementedException();
        }

        public static VirtualDisk CreateBlank(string vmName, VirtualDiskFunction function, int totalSizeGB, HostStorage storage)
        {
            throw new NotImplementedException();
        }

        public static VirtualDisk TryFindByName(string vmName, VirtualDiskFunction function, HostStorage storage)
        {
            throw new NotImplementedException();
        }

        private QemuDisk(string vmName, VirtualDiskFunction function, string storagePath, int totalSizeGB, bool isSSD) :
            base(vmName, function, storagePath, totalSizeGB, isSSD)
        { }

        public override void Resize(int totalSizeGB)
        {
            throw new NotImplementedException();
        }

        public override void Delete()
        {
            throw new NotImplementedException();
        }

        public override bool IsServiceRequired()
        {
            throw new NotImplementedException();
        }

        public override void StartService()
        {
            throw new NotImplementedException();
        }

        public override bool IsServiceRunnig()
        {
            throw new NotImplementedException();
        }

        public override void StopService()
        {
            throw new NotImplementedException();
        }
    }
}
