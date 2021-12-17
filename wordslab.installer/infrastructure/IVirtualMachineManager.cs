namespace wordslab.installer.infrastructure
{
    public interface IVirtualMachineManager
    {
        string InstallPath { get; }
        string DownloadCachePath { get; }
        string LocalVirtualDisksPath { get; }

        bool IsVirtualMachineManagerInstalled();
        bool InstallVirtualMachineManager(out string manualOperationNecessary);
        bool IsVirtualMachineManagerUpToDate();
        bool UpgradeVirtualMachineManager();
        public DateTime LastVirtualMachineManagerBackup { get; }
        bool BackupVirtualMachineManager(BackupStorage backupStorage);
        bool UninstallVirtualMachineManager(out string manualOperationNecessary);

        string VMLauncherName { get; }
        string VMLauncherVersion { get; }

        List<PersistentDisk> ListPersistentDisks();
        bool DoVirtualMachineDisksExist(VirtualMachine vmProperties);
        VirtualMachine CreateVirtualMachineDisks(VirtualMachine vmProperties);
        bool AreOSAndK3sDisksUpToDate(VirtualMachine vmProperties);
        VirtualMachine UpgradeOSAndK3sDisks(VirtualMachine vmProperties);
        bool ResizePersistentDisk(PersistentDisk disk);
        bool DeletePersistentDisk(PersistentDisk disk);

        List<VirtualMachine> ListVirtualMachines();
        bool DoesVirtualMachineExist(string name = VirtualMachine.DEFAULT_NAME);
        VirtualMachine CreateVirtualMachine(string name = VirtualMachine.DEFAULT_NAME, VirtualMachine vmProperties = null);
        bool IsVirtualMachineRunning(string name = VirtualMachine.DEFAULT_NAME);
        bool StartVirtualMachine(string name = VirtualMachine.DEFAULT_NAME);
        bool StopVirtualMachine(string name = VirtualMachine.DEFAULT_NAME);
        bool DeleteVirtualMachine(string name = VirtualMachine.DEFAULT_NAME);
    }
}
