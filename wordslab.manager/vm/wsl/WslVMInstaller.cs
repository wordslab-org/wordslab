using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslVMInstaller
    {
        public static VirtualMachine FindLocalVM(HostStorage hostStorage)
        {
            try
            {
                // Wsl.execShell("ls /usr/local/bin/k3s", VM_DISTRIB_NAME);
                return null;
            }
            catch { }
            return null;
        }

        public static async Task<VirtualMachine> Install(VirtualMachineSpec vmSpec, HostStorage hostStorage, InstallProcessUI ui)
        {
            try
            {
                // 1. Check Hardware requirements
                bool cpuSpecOK = true;
                bool memorySpecOK = true;
                bool storageSpecOK = true;
                bool cpuVirtualization = true;
                bool gpuSpecOK = true;

                ui.DisplayInstallStep(1, 6, "Check hardware requirements");

                var c1 = ui.DisplayCommandLaunch($"Host CPU with at least {vmSpec.LogicalProcessors} + 2 logical processors");
                var cpuInfo = Compute.GetCPUInfo();
                cpuSpecOK = cpuInfo.NumberOfLogicalProcessors >= (vmSpec.LogicalProcessors+2);
                ui.DisplayCommandResult(c1, cpuSpecOK, cpuSpecOK ? null : $"Sorry, a CPU with {cpuInfo.NumberOfLogicalProcessors} logical processors is not powerful enough to host the required virtual machine");
                if (!cpuSpecOK)
                {
                    return null;
                }

                var c2 = ui.DisplayCommandLaunch($"Host machine with at least {vmSpec.MemoryGB} + 4 GB physical memory");
                var memInfo = Memory.GetMemoryInfo();
                memorySpecOK = memInfo.TotalPhysicalMB >= (ulong)((vmSpec.MemoryGB+4)*1000);
                ui.DisplayCommandResult(c2, memorySpecOK, memorySpecOK ? null : $"Sorry, a machine with {memInfo.TotalPhysicalMB} MB of memory is not powerful enough to host the required virtual machine");
                if (!memorySpecOK)
                {
                    return null;
                }

                var c3 = ui.DisplayCommandLaunch($"Host machine with at least {vmSpec.VmDiskSizeGB+vmSpec.ClusterDiskSizeGB+vmSpec.DataDiskSizeGB} + 2 GB free storage space");
                var storageReqsGB = new Dictionary<os.DriveInfo, int>();
                var downloadCacheDrive = Storage.GetDriveInfoFromPath(hostStorage.DownloadCacheDirectory);
                storageReqsGB.Add(downloadCacheDrive, 2);
                var vmDrives = new os.DriveInfo[] { 
                    Storage.GetDriveInfoFromPath(hostStorage.VirtualMachineOSDirectory),
                    Storage.GetDriveInfoFromPath(hostStorage.VirtualMachineClusterDirectory),
                    Storage.GetDriveInfoFromPath(hostStorage.VirtualMachineDataDirectory) };
                var vmStorageReqs = new int[] { vmSpec.VmDiskSizeGB, vmSpec.ClusterDiskSizeGB, vmSpec.DataDiskSizeGB };
                var vmSSDReqs = new bool[] { vmSpec.VmDiskIsSSD, vmSpec.ClusterDiskIsSSD, vmSpec.DataDiskIsSSD };
                var storageSpaceMessage = "";
                foreach (var tuple in vmDrives.Zip(vmStorageReqs, vmSSDReqs))
                {
                    var (vmDrive, vmStorageReq, vmSSDReq) = tuple;
                    if (storageReqsGB.ContainsKey(vmDrive))
                    {
                        storageReqsGB[vmDrive] += vmStorageReq;
                    }
                    else
                    {
                        storageReqsGB.Add(vmDrive, vmStorageReq);
                    }
                    if(vmSSDReq && !vmDrive.IsSSD)
                    {
                        storageSpecOK = false;
                        storageSpaceMessage += $"Sorry, the volume {vmDrive.VolumeName} is not an SSD: this was required for one virtual machine disk in the virtual machine spec. ";
                    }
                }
                foreach(var drive in storageReqsGB.Keys)
                {
                    if(drive.FreeSpaceMB < (storageReqsGB[drive]*1000))
                    {
                        storageSpecOK = false;
                        storageSpaceMessage += $"Sorry, not enough free storage space on {drive.VolumeName}: {(int)(drive.FreeSpaceMB/1000)} GB avaible but {storageReqsGB[drive]} GB required. ";
                    }
                }
                ui.DisplayCommandResult(c3, storageSpecOK, storageSpecOK ? null : $"{storageSpaceMessage} You can try to update wordslab storage configuration by moving the VM directories to another disk where more space is available.");
                if (!storageSpecOK)
                {
                    return null;
                }

                var c4 = ui.DisplayCommandLaunch("Host machine with CPU virtualization enabled");
                cpuVirtualization = Compute.IsCPUVirtualizationAvailable(cpuInfo);
                ui.DisplayCommandResult(c4, cpuVirtualization, cpuVirtualization ? null :
                    "Please go to Windows Settings > Update & Security > Recovery (left menu) > Advanced Startup > Restart Now," +
                    " then select: Troubleshoot > Advanced options > UEFI firmware settings, and navigate menus to enable virtualization");
                if (!cpuVirtualization)
                {
                    Windows.OpenWindowsUpdate();
                    return null;
                }

                if (vmSpec.WithGPU)
                {
                    var c5 = ui.DisplayCommandLaunch($"Host machine with a recent Nvidia GPU: {vmSpec.GPUModel} and at least {vmSpec.GPUMemoryGB} GB GPU memory");
                    var gpus = Compute.GetNvidiaGPUsInfo();
                    var availableGPU = gpus.Where(gpu => gpu.ModelName == vmSpec.GPUModel).FirstOrDefault();
                    if(availableGPU != null)
                    {
                        gpuSpecOK = availableGPU.MemoryMB >= (vmSpec.GPUMemoryGB*1000);
                    }
                    else
                    {
                        gpuSpecOK = false;
                    }
                    ui.DisplayCommandResult(c5, gpuSpecOK, gpuSpecOK ? availableGPU.ModelName : "Sorry, could not find the required Nvidia GPU on the host machine. If the card is physically inserted in the machine, you can try to update your Nvidia drivers and to install nvidia-smi");
                    if (!gpuSpecOK)
                    {
                        return null;
                    }
                }

                bool createVMWithGPUSupport = vmSpec.WithGPU && gpuSpecOK;

                // 2. Check OS and drivers requirements
                bool windowsVersionOK = true;
                bool nvidiaDriverVersionOK = true;

                ui.DisplayInstallStep(2, 6, "Check operating system requirements");

                if (createVMWithGPUSupport)
                {
                    var c6 = ui.DisplayCommandLaunch("Checking if operating system version is Windows 10 version 21H2 or higher");
                    windowsVersionOK = Wsl.IsWindowsVersionOKForWSL2WithGPU();
                    ui.DisplayCommandResult(c6, windowsVersionOK);                    
                }
                else
                {
                    var c7 = ui.DisplayCommandLaunch("Checking if operating system version is Windows 10 version 1903 or higher");
                    windowsVersionOK = Wsl.IsWindowsVersionOKForWSL2();
                    ui.DisplayCommandResult(c7, windowsVersionOK);
                }
                if (!windowsVersionOK)
                {
                    var c8 = ui.DisplayCommandLaunch("Launching Windows Update to upgrade operating system version");
                    ui.DisplayCommandResult(c8, true, "Please update Windows, reboot your machine if necessary, then launch this installer again");
                    Windows.OpenWindowsUpdate();
                    return null;
                }

                if (createVMWithGPUSupport)
                {
                    var c9 = ui.DisplayCommandLaunch("Checking Nvidia driver version");
                    nvidiaDriverVersionOK = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
                    ui.DisplayCommandResult(c9, nvidiaDriverVersionOK);

                    if (!nvidiaDriverVersionOK)
                    {
                        var c10 = ui.DisplayCommandLaunch("Please update your Nvidia driver to the latest version. Trying to launch Geforce experience ...");
                        Nvidia.TryOpenNvidiaUpdate();
                        ui.DisplayCommandResult(c10, true, "Go to the Pilots section and check if a newer version is available");

                        var driverUpdateOK = await ui.DisplayQuestionAsync("Did you manage to update the Nvidia driver to the latest version ?");
                        if (!driverUpdateOK)
                        {
                            return null;
                        }

                        var c11 = ui.DisplayCommandLaunch("Checking Nvidia driver version (after update)");
                        nvidiaDriverVersionOK = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
                        ui.DisplayCommandResult(c11, nvidiaDriverVersionOK, nvidiaDriverVersionOK ? null : "Nvidia driver version still not OK: please restart the install process without using the GPU");
                        if (!nvidiaDriverVersionOK)
                        {
                            return null;
                        }
                    }
                }
                                
                // 3. Check Host software dependencies
                bool wsl2Installed = true;
                bool linuxKernelVersionOK = true;

                ui.DisplayInstallStep(3, 6, "Check host software dependencies");

                var c12 = ui.DisplayCommandLaunch("Checking if Windows Subsystem for Linux 2 is already installed");
                wsl2Installed = Wsl.IsWSL2AlreadyInstalled();
                ui.DisplayCommandResult(c12, wsl2Installed);

                if (!wsl2Installed)
                {                    
                    var enableWsl = await ui.DisplayAdminScriptQuestionAsync(
                        "Activating Windows Virtual Machine Platform and Windows Subsystem for Linux requires ADMIN privileges. Are you OK to execute the following script as admin ?",
                        Windows.EnableWindowsSubsystemForLinux_script(hostStorage.ScriptsDirectory));
                    if (!enableWsl)
                    {
                        return null;
                    }

                    var c13 = ui.DisplayCommandLaunch("Activating Windows Virtual Machine Platform and Windows Subsystem for Linux");
                    var restartNeeded = Windows.EnableWindowsSubsystemForLinux(hostStorage.ScriptsDirectory, hostStorage.LogsDirectory);
                    ui.DisplayCommandResult(c13, true);
                    if (restartNeeded)
                    {
                        var restartNow = await ui.DisplayQuestionAsync("You need to restart your computer to finish Windows Subsystem for Linux installation. Restart now ?");
                        if (restartNow)
                        {
                            Windows.ShutdownAndRestart();
                        }
                        return null;
                    }
                }

                if (createVMWithGPUSupport)
                {
                    var c14 = ui.DisplayCommandLaunch("Checking Windows Subsystem for Linux kernel version (GPU support)");
                    linuxKernelVersionOK = Wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
                    ui.DisplayCommandResult(c14, linuxKernelVersionOK);

                    if (!linuxKernelVersionOK)
                    {
                        var c15 = ui.DisplayCommandLaunch("Updating Windows Subsystem for Linux kernel to the latest version");
                        Wsl.UpdateLinuxKernelVersion();
                        ui.DisplayCommandResult(c15, true);

                        var c16 = ui.DisplayCommandLaunch("Checking Windows Subsystem for Linux kernel version (after update)");
                        linuxKernelVersionOK = Wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
                        ui.DisplayCommandResult(c16, linuxKernelVersionOK, linuxKernelVersionOK ? null : "Windows Subsystem for Linux kernel version still not OK: please restart the install process without using the GPU");
                        if (!linuxKernelVersionOK)
                        {
                            return null;
                        }
                    }
                }

                // 4. Download VM software images
                bool alpineImageOK = true;
                bool ubuntuImageOK = true;
                bool k3sExecutableOK = true;
                bool k3sImagesOK = true;
                bool helmExecutableOK = true;

                ui.DisplayInstallStep(4, 6, "Download Virtual Machine OS images and Kubernetes tools");

                var c17 = ui.DisplayCommandLaunchWithProgress($"Downloading Alpine Linux operating system image (v{WslDisk.alpineVersion})", WslDisk.alpineImageSize, "Bytes");
                var download1 = hostStorage.DownloadFileWithCache(WslDisk.alpineImageURL, WslDisk.alpineFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c17, totalBytesDownloaded));

                var c18 = ui.DisplayCommandLaunchWithProgress($"Downloading Ubuntu Linux operating system image ({WslDisk.ubuntuRelease} {WslDisk.ubuntuVersion})", WslDisk.ubuntuImageSize, "Bytes");
                var download2 = hostStorage.DownloadFileWithCache(WslDisk.ubuntuImageURL, WslDisk.ubuntuFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c18, totalBytesDownloaded));

                var c19 = ui.DisplayCommandLaunchWithProgress($"Downloading Rancher K3s executable (v{VirtualMachine.k3sVersion})", VirtualMachine.k3sExecutableSize, "Bytes");
                var download3 = hostStorage.DownloadFileWithCache(VirtualMachine.k3sExecutableURL, VirtualMachine.k3sExecutableFileName, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c19, totalBytesDownloaded));

                var c20 = ui.DisplayCommandLaunchWithProgress($"Downloading Rancher K3s containers images (v{VirtualMachine.k3sVersion})", VirtualMachine.k3sImagesSize, "Bytes");
                var download4 = hostStorage.DownloadFileWithCache(VirtualMachine.k3sImagesURL, VirtualMachine.k3sImagesFileName, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c20, totalBytesDownloaded));

                var c21 = ui.DisplayCommandLaunchWithProgress($"Downloading Helm executable (v{VirtualMachine.helmVersion})", VirtualMachine.helmExecutableSize, "Bytes");
                var download5 = hostStorage.DownloadFileWithCache(VirtualMachine.helmExecutableURL, VirtualMachine.helmFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c21, totalBytesDownloaded));

                Task.WaitAll(download1, download2, download3, download4, download5);

                // Extract helm executable from the downloaded tar file
                var helmExecutablePath = Path.Combine(hostStorage.DownloadCacheDirectory, "helm");
                if (!File.Exists(helmExecutablePath))
                {
                    var helmTarFile = Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.helmFileName);
                    var helmTmpDir = Path.Combine(hostStorage.DownloadCacheDirectory, "helm-temp");
                    Directory.CreateDirectory(helmTmpDir);
                    HostStorage.ExtractTarFile(helmTarFile, helmTmpDir);
                    File.Move(Path.Combine(helmTmpDir, "linux-amd64", "helm"), helmExecutablePath);
                    Directory.Delete(helmTmpDir, true);
                }

                var alpineFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.alpineFileName));
                alpineImageOK = alpineFile.Exists && alpineFile.Length == WslDisk.alpineImageSize;
                ui.DisplayCommandResult(c17, alpineImageOK);

                var ubuntuFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.ubuntuFileName));
                ubuntuImageOK = ubuntuFile.Exists && ubuntuFile.Length == WslDisk.ubuntuImageSize;
                ui.DisplayCommandResult(c18, ubuntuImageOK);

                var k3sExecFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.k3sExecutableFileName));
                k3sExecutableOK = k3sExecFile.Exists && k3sExecFile.Length == VirtualMachine.k3sExecutableSize;
                ui.DisplayCommandResult(c19, k3sExecutableOK);

                var k3sImagesFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.k3sImagesFileName));
                k3sImagesOK = k3sImagesFile.Exists && k3sImagesFile.Length == VirtualMachine.k3sImagesSize;
                ui.DisplayCommandResult(c20, k3sImagesOK);

                var helmExecFile = new FileInfo(helmExecutablePath);
                helmExecutableOK = helmExecFile.Exists && helmExecFile.Length == VirtualMachine.helmExtractedSize;
                ui.DisplayCommandResult(c21, helmExecutableOK);

                if (!alpineImageOK || !ubuntuImageOK || !k3sExecutableOK || !k3sImagesOK || !helmExecutableOK)
                {
                    return null;
                }

                // 5. Create VM disks
                bool virtualDiskVMOK = true;
                bool virtualDiskClusterOK = true;
                bool virtualDiskDataOK = true;

                var wslDistribs = Wsl.list();
                var cachePath = hostStorage.DownloadCacheDirectory;

                var dataDisk = WslDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.Data, hostStorage);
                if (dataDisk == null)
                {
                    var c22 = ui.DisplayCommandLaunch("Initializing wordslab data virtual disk");
                    dataDisk = WslDisk.CreateBlank(vmSpec.Name, VirtualDiskFunction.Data, hostStorage);
                    ui.DisplayCommandResult(c22, dataDisk != null);
                }

                var clusterDisk = WslDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.Cluster, hostStorage);
                if (clusterDisk == null)
                {
                    var c23 = ui.DisplayCommandLaunch("Initializing wordslab cluster virtual disk");
                    clusterDisk = WslDisk.CreateBlank(vmSpec.Name, VirtualDiskFunction.Cluster, hostStorage);
                    ui.DisplayCommandResult(c23, clusterDisk != null);
                }

                var osDisk = WslDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.OS, hostStorage);
                if (osDisk == null)
                {
                    var c24 = ui.DisplayCommandLaunch("Initializing wordslab vm virtual disk");
                    osDisk = WslDisk.CreateFromOSImage(vmSpec.Name, Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.ubuntuFileName), hostStorage);
                    ui.DisplayCommandResult(c24, osDisk != null);
                }

                if (useGpu)
                {
                    var c19 = ui.DisplayCommandLaunch("Installing nvidia GPU software inside virtual machine");
                    Wsl.execShell($"/root/wordslab-cluster-start.sh", "wordslab-cluster");
                    Wsl.execShell("nvidia-smi -L", "wordslab-os");
                    Wsl.execShell($"/root/wordslab-gpu-init.sh '{cachePath}' '{nvidiaContainerRuntimeVersion}'", "wordslab-os", ignoreError: "perl: warning");
                    Wsl.terminate("wordslab-cluster");
                    ui.DisplayCommandResult(c19, true);
                }

                // 6. Initialize and start local Virtual Machine

                var localVM = FindLocalVM(hostStorage);

                var c20 = ui.DisplayCommandLaunch("Launching wordslab virtual machine and k3s cluster");
                int vmIP;
                string kubeconfigPath;
                var vmEndPoint = localVM.Start();
                ui.DisplayCommandResult(c20, true, $"Virtual machine IP = {vmEndPoint.Address}, KUBECONFIG = {vmEndPoint.KubeConfigPath}");

                return localVM;
            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
                return null;
            }
        }

        public static async Task<bool> Uninstall(HostStorage hostStorage, InstallProcessUI ui)
        {
            try
            {
                var localVM = FindLocalVM(hostStorage);

                var c1 = ui.DisplayCommandLaunch("Stopping wordslab virtual machine and local k3s cluster");
                localVM.Stop();
                ui.DisplayCommandResult(c1, true);

                var confirm = await ui.DisplayQuestionAsync("Are you sure you want to delete the virtual machine : ALL DATA WILL BE LOST ?");
                if (confirm)
                {
                    var c2 = ui.DisplayCommandLaunch("Deleting wordslab virtual machine and local k3s cluster");
                    Wsl.unregister("wordslab-os");
                    Wsl.unregister("wordslab-cluster");
                    Wsl.unregister("wordslab-data");
                    ui.DisplayCommandResult(c2, true);

                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
                return false;
            }
        }
    }
}
