namespace wordslab.installer.infrastructure
{
    public class WindowsVirtualMachineManager : IVirtualMachineManager
    {
        public string InstallPath => throw new NotImplementedException();

        public string DownloadCachePath => throw new NotImplementedException();

        public string LocalVirtualDisksPath => throw new NotImplementedException();

        public DateTime LastVirtualMachineManagerBackup => throw new NotImplementedException();

        public string VMLauncherName => throw new NotImplementedException();

        public string VMLauncherVersion => throw new NotImplementedException();

        public bool AreOSAndK3sDisksUpToDate(VirtualMachine vmProperties)
        {
            throw new NotImplementedException();
        }

        public bool BackupVirtualMachineManager(BackupStorage backupStorage)
        {
            throw new NotImplementedException();
        }

        public VirtualMachine CreateVirtualMachine(string name = "wordslab-vm", VirtualMachine vmProperties = null)
        {
            throw new NotImplementedException();
        }

        public VirtualMachine CreateVirtualMachineDisks(VirtualMachine vmProperties)
        {
            throw new NotImplementedException();
        }

        public bool DeletePersistentDisk(PersistentDisk disk)
        {
            throw new NotImplementedException();
        }

        public bool DeleteVirtualMachine(string name = "wordslab-vm")
        {
            throw new NotImplementedException();
        }

        public bool DoesVirtualMachineExist(string name = "wordslab-vm")
        {
            throw new NotImplementedException();
        }

        public bool DoVirtualMachineDisksExist(VirtualMachine vmProperties)
        {
            throw new NotImplementedException();
        }

        public bool InstallVirtualMachineManager(out string manualOperationNecessary)
        {
            throw new NotImplementedException();
        }

        public bool IsVirtualMachineManagerInstalled()
        {
            throw new NotImplementedException();
        }

        public bool IsVirtualMachineManagerUpToDate()
        {
            throw new NotImplementedException();
        }

        public bool IsVirtualMachineRunning(string name = "wordslab-vm")
        {
            throw new NotImplementedException();
        }

        public List<PersistentDisk> ListPersistentDisks()
        {
            throw new NotImplementedException();
        }

        public List<VirtualMachine> ListVirtualMachines()
        {
            throw new NotImplementedException();
        }

        public bool ResizePersistentDisk(PersistentDisk disk)
        {
            throw new NotImplementedException();
        }

        public bool StartVirtualMachine(string name = "wordslab-vm")
        {
            throw new NotImplementedException();
        }

        public bool StopVirtualMachine(string name = "wordslab-vm")
        {
            throw new NotImplementedException();
        }

        public bool UninstallVirtualMachineManager(out string manualOperationNecessary)
        {
            throw new NotImplementedException();
        }

        public VirtualMachine UpgradeOSAndK3sDisks(VirtualMachine vmProperties)
        {
            throw new NotImplementedException();
        }

        public bool UpgradeVirtualMachineManager()
        {
            throw new NotImplementedException();
        }
    }
}
