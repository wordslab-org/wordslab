using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslVMInstaller
    {
        public static async Task<VirtualMachine> Install(VirtualMachineSpec vmSpec, HostStorage hostStorage, InstallProcessUI ui)
        {
            VirtualMachine localVM = null;
            try
            {
                // 1. Check Hardware requirements
                bool cpuSpecOK = true;
                bool memorySpecOK = true;
                bool storageSpecOK = true;
                bool cpuVirtualization = true;
                bool gpuSpecOK = true;

                ui.DisplayInstallStep(1, 6, "Check hardware requirements");

                var c1 = ui.DisplayCommandLaunch($"Host CPU with at least {vmSpec.Processors} (VM) + {VirtualMachineSpec.MIN_HOST_PROCESSORS} (host) logical processors");
                var cpuInfo = Compute.GetCPUInfo();
                string cpuErrorMessage;
                cpuSpecOK = vmSpec.CheckCPURequirements(cpuInfo, out cpuErrorMessage);
                ui.DisplayCommandResult(c1, cpuSpecOK, cpuSpecOK ? null : cpuErrorMessage);
                if (!cpuSpecOK)
                {
                    return null;
                }

                var c2 = ui.DisplayCommandLaunch($"Host machine with at least {vmSpec.MemoryGB} (VM) + {VirtualMachineSpec.MIN_HOST_MEMORY_GB} (host) GB physical memory");
                var memInfo = Memory.GetMemoryInfo();
                string memoryErrorMessage;
                memorySpecOK = vmSpec.CheckMemoryRequirements(memInfo, out memoryErrorMessage);
                ui.DisplayCommandResult(c2, memorySpecOK, memorySpecOK ? null : memoryErrorMessage);
                if (!memorySpecOK)
                {
                    return null;
                }

                var c3 = ui.DisplayCommandLaunch($"Host machine with at least {vmSpec.VmDiskSizeGB+vmSpec.ClusterDiskSizeGB+vmSpec.DataDiskSizeGB} (VM) + {VirtualMachineSpec.MIN_HOST_DISK_GB + VirtualMachineSpec.MIN_HOST_DOWNLOADDIR_GB} (host) GB free storage space");
                Dictionary<os.DriveInfo, int> storageReqsGB;
                string storageErrorMessage;
                storageSpecOK = vmSpec.CheckStorageRequirements(hostStorage, out storageReqsGB, out storageErrorMessage);
                ui.DisplayCommandResult(c3, storageSpecOK, storageSpecOK ? null : $"{storageErrorMessage}You can try to update wordslab storage configuration by moving the VM directories to another disk where more space is available.");
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
                    var gpusInfo = Compute.GetNvidiaGPUsInfo();
                    string gpuErrorMessage;
                    gpuSpecOK = vmSpec.CheckGPURequirements(gpusInfo, out gpuErrorMessage);
                    ui.DisplayCommandResult(c5, gpuSpecOK, gpuSpecOK ? null : gpuErrorMessage);
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
                    var c6 = ui.DisplayCommandLaunch("Checking if operating system version is Windows 10 x64 version 21H2 or higher");
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
                        Nvidia.TryOpenNvidiaUpdateOnWindows();
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
                        Wsl.UpdateLinuxKernelVersion(hostStorage.ScriptsDirectory, hostStorage.LogsDirectory);
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

                var c17 = new LongRunningCommand($"Downloading Alpine Linux operating system image (v{WslDisk.alpineVersion})", WslDisk.alpineImageDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(WslDisk.alpineImageURL, WslDisk.alpineFileName, gunzip: true, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var alpineFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.alpineFileName));
                        alpineImageOK = alpineFile.Exists && alpineFile.Length == WslDisk.alpineImageDiskSize;
                        displayResult(alpineImageOK);
                    });

                var c18 = new LongRunningCommand($"Downloading Ubuntu Linux operating system image ({WslDisk.ubuntuRelease} {WslDisk.ubuntuVersion})", WslDisk.ubuntuImageDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(WslDisk.ubuntuImageURL, WslDisk.ubuntuFileName, gunzip: true, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var ubuntuFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.ubuntuFileName));
                        ubuntuImageOK = ubuntuFile.Exists && ubuntuFile.Length == WslDisk.ubuntuImageDiskSize;
                        displayResult(ubuntuImageOK);
                    });

                var c19 = new LongRunningCommand($"Downloading Rancher K3s executable (v{VirtualMachine.k3sVersion})", VirtualMachine.k3sExecutableSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(VirtualMachine.k3sExecutableURL, VirtualMachine.k3sExecutableFileName, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var k3sExecFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.k3sExecutableFileName));
                        k3sExecutableOK = k3sExecFile.Exists && k3sExecFile.Length == VirtualMachine.k3sExecutableSize;
                        displayResult(k3sExecutableOK);
                    });

                var c20 = new LongRunningCommand($"Downloading Rancher K3s containers images (v{VirtualMachine.k3sVersion})", VirtualMachine.k3sImagesDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(VirtualMachine.k3sImagesURL, VirtualMachine.k3sImagesFileName, gunzip: true,
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var k3sImagesFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.k3sImagesFileName));
                        k3sImagesOK = k3sImagesFile.Exists && k3sImagesFile.Length == VirtualMachine.k3sImagesDiskSize;
                        displayResult(k3sImagesOK);
                    });

                var c21 = new LongRunningCommand($"Downloading Helm executable (v{VirtualMachine.helmVersion})", VirtualMachine.helmExecutableDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(VirtualMachine.helmExecutableURL, VirtualMachine.helmFileName, gunzip: true, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
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

                        var helmExecFile = new FileInfo(helmExecutablePath);
                        helmExecutableOK = helmExecFile.Exists && helmExecFile.Length == VirtualMachine.helmExecutableDiskSize;
                        displayResult(helmExecutableOK);
                    });

                ui.RunCommandsAndDisplayProgress(new LongRunningCommand[] { c17, c18, c19, c20, c21 });

                if (!alpineImageOK || !ubuntuImageOK || !k3sExecutableOK || !k3sImagesOK || !helmExecutableOK)
                {
                    return null;
                }

                // 5. Create VM disks
                bool virtualDiskVMOK = true;
                bool virtualDiskClusterOK = true;
                bool virtualDiskDataOK = true;

                ui.DisplayInstallStep(5, 6, "Initialize wordslab VM virtual disks");

                var dataDisk = WslDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.Data, hostStorage);
                if (dataDisk == null)
                {
                    var c22 = ui.DisplayCommandLaunch("Initializing wordslab data virtual disk");
                    dataDisk = WslDisk.CreateBlank(vmSpec.Name, VirtualDiskFunction.Data, hostStorage);
                    virtualDiskDataOK = dataDisk != null;
                    ui.DisplayCommandResult(c22, virtualDiskDataOK);
                }
                if(!virtualDiskDataOK) { return null; }

                var clusterDisk = WslDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.Cluster, hostStorage);
                if (clusterDisk == null)
                {
                    var c23 = ui.DisplayCommandLaunch("Initializing wordslab cluster virtual disk");
                    clusterDisk = WslDisk.CreateBlank(vmSpec.Name, VirtualDiskFunction.Cluster, hostStorage);
                    virtualDiskClusterOK = clusterDisk != null;
                    ui.DisplayCommandResult(c23, virtualDiskClusterOK);
                }
                if (!virtualDiskClusterOK) { return null; }

                var osDisk = WslDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.OS, hostStorage);
                if (osDisk == null)
                {
                    var c24 = ui.DisplayCommandLaunch("Initializing wordslab VM virtual disk");
                    osDisk = WslDisk.CreateFromOSImage(vmSpec.Name, Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.ubuntuFileName), hostStorage);
                    virtualDiskVMOK = osDisk != null;
                    ui.DisplayCommandResult(c24, virtualDiskVMOK);

                    if (createVMWithGPUSupport)
                    {
                        var c25 = ui.DisplayCommandLaunch("Installing nvidia GPU software on VM virtual disk");
                        ((WslDisk)osDisk).InstallNvidiaContainerRuntimeOnOSImage(hostStorage);
                        ui.DisplayCommandResult(c25, true);
                    }
                }
                if (!virtualDiskVMOK) { return null; }

                // 6. Configure and start local Virtual Machine
                bool vmConfigOK = true;
                bool vmInitOK = true;

                ui.DisplayInstallStep(6, 6, "Configure and start wordslab Virtual Machine");
                                
                var wslConfig = Wsl.Read_wslconfig();
                var needToUpdateWslConfig = wslConfig.NeedsToBeUpdatedForVmSpec(vmSpec.Processors, vmSpec.MemoryGB);

                var updateWslConfig = false;
                if (needToUpdateWslConfig)
                {
                    updateWslConfig = await ui.DisplayQuestionAsync($"Do you confirm you want to update the Windows Subsystem for Linux configuration to match with your VM specification: {vmSpec.Processors} processors, {vmSpec.MemoryGB} GB memory ? This will affect all WSL distributions launched from your Windows account, not only wordslab.");
                }
                if (updateWslConfig && Wsl.IsRunning())
                {
                    updateWslConfig = await ui.DisplayQuestionAsync("All WSL distributions currently running will be stopped: please make sure you take all the necessary measures for a graceful shutdown before continuing. Are you ready now ?");
                }               
                if (updateWslConfig)
                {
                    var c26 = ui.DisplayCommandLaunch($"Updating Windows Subsystem for Linux configuration to match your VM specification: {vmSpec.Processors} processors, {vmSpec.MemoryGB} GB memory");
                    wslConfig.UpdateToVMSpec(vmSpec.Processors, vmSpec.MemoryGB, restartIfNeeded: true);
                    ui.DisplayCommandResult(c26, true);
                }

                localVM = WslVM.TryFindByName(vmSpec.Name, hostStorage);

                var c27 = ui.DisplayCommandLaunch("Launching wordslab virtual machine and k3s cluster");
                VirtualMachineEndpoint localVMEndpoint = null;
                if (!localVM.IsRunning())
                {
                    localVMEndpoint = localVM.Start(vmSpec);
                }
                else
                {
                    localVMEndpoint = localVM.Endpoint;
                }

                ui.DisplayCommandResult(c27, true, $"Virtual machine started : IP = {localVMEndpoint.IPAddress}, HTTP port = {localVMEndpoint.HttpIngressPort}");
            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
            }

            return localVM;
        }

        public static async Task<bool> Uninstall(string vmName, HostStorage hostStorage, InstallProcessUI ui)
        {
            try
            {
                var localVM = WslVM.TryFindByName(vmName, hostStorage);
                if (localVM == null) return true;

                var c1 = ui.DisplayCommandLaunch("Stopping wordslab virtual machine and local k3s cluster");
                localVM.Stop();
                ui.DisplayCommandResult(c1, true);

                var confirm = await ui.DisplayQuestionAsync("Are you sure you want to delete the virtual machine: ALL DATA WILL BE LOST !!");
                if (confirm)
                {
                    var c2 = ui.DisplayCommandLaunch("Deleting wordslab virtual machine and local k3s cluster");
                    localVM.OsDisk.Delete();
                    localVM.ClusterDisk.Delete();
                    localVM.DataDisk.Delete();
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
