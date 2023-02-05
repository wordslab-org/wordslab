using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public static class WslVMInstaller
    {
        public static async Task<HostMachineConfig> ConfigureHostMachine(HostStorage hostStorage, InstallProcessUI ui)
        {
            try
            {
                var machineConfig = new HostMachineConfig();
                machineConfig.HostName = OS.GetMachineName();

                // 0. Get minimum VM spec
                var minVmSpec = VMRequirements.GetMinimumVMSpec();

                // 1. Check Hardware requirements
                bool cpuSpecOK = true;
                bool memorySpecOK = true;
                bool cpuVirtualization = true;
                bool gpuSpecOK = true;

                ui.DisplayInstallStep(1, 6, "Check hardware requirements");

                var c1 = ui.DisplayCommandLaunch($"Host CPU with at least {minVmSpec.Compute.Processors} (VM) + {VMRequirements.MIN_HOST_RESERVED_PROCESSORS} (host) logical processors");
                var cpuInfo = Compute.GetCPUInfo();
                string cpuErrorMessage;
                cpuSpecOK = VMRequirements.CheckCPURequirements(minVmSpec, cpuInfo, out cpuErrorMessage);
                ui.DisplayCommandResult(c1, cpuSpecOK, cpuSpecOK ? null : cpuErrorMessage);
                if (!cpuSpecOK)
                {
                    return null;
                }

                var c2 = ui.DisplayCommandLaunch($"Host machine with at least {minVmSpec.Compute.MemoryGB} (VM) + {VMRequirements.MIN_HOST_RESERVED_MEMORY_GB} (host) GB physical memory");
                var memInfo = Memory.GetMemoryInfo();
                string memoryErrorMessage;
                memorySpecOK = VMRequirements.CheckMemoryRequirements(minVmSpec, memInfo, out memoryErrorMessage);
                ui.DisplayCommandResult(c2, memorySpecOK, memorySpecOK ? null : memoryErrorMessage);
                if (!memorySpecOK)
                {
                    return null;
                }

                var c3 = ui.DisplayCommandLaunch("Host machine with CPU virtualization enabled");
                cpuVirtualization = Compute.IsCPUVirtualizationAvailable(cpuInfo);
                ui.DisplayCommandResult(c3, cpuVirtualization, cpuVirtualization ? null :
                    "Please go to Windows Settings > Update & Security > Recovery (left menu) > Advanced Startup > Restart Now," +
                    " then select: Troubleshoot > Advanced options > UEFI firmware settings, and navigate menus to enable virtualization");
                if (!cpuVirtualization)
                {
                    Windows.OpenWindowsUpdate();
                    return null;
                }

                var userWantsVMWithGPU = false;
                var c4 = ui.DisplayCommandLaunch($"[optional] Host machine with at least one Nvidia GPU");
                var gpusInfo = Compute.GetNvidiaGPUsInfo();
                var hasNvidiaGPU = gpusInfo.Count > 0;
                ui.DisplayCommandResult(c4, hasNvidiaGPU, hasNvidiaGPU ? null : "Could not find any GPU on this machine using the nvidia-smi command");
                if (hasNvidiaGPU) 
                {
                    userWantsVMWithGPU = await ui.DisplayQuestionAsync("Do you want to allow the local virtual machines to access your Nvidia GPU(s) ?");
                }
                machineConfig.CanUseGPUs = true;
                if (userWantsVMWithGPU)
                {
                    var c5 = ui.DisplayCommandLaunch($"Host machine with a recent Nvidia GPU: at least {minVmSpec.GPU.ModelName} and {minVmSpec.GPU.MemoryGB} GB GPU memory");
                    string gpuErrorMessage;
                    gpuSpecOK = VMRequirements.CheckGPURequirements(minVmSpec, gpusInfo, out gpuErrorMessage);
                    ui.DisplayCommandResult(c5, gpuSpecOK, gpuSpecOK ? null : gpuErrorMessage);
                    if (!gpuSpecOK)
                    {
                        return null;
                    }
                }

                bool createVMWithGPUSupport = userWantsVMWithGPU && gpuSpecOK;

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
                    var useInstallCommand = Wsl.IsWindowsVersionOKForInstallCommand();
                    var scriptToExecute = useInstallCommand ? Wsl.install_script(hostStorage.ScriptsDirectory) : Windows.EnableWindowsSubsystemForLinux_script(hostStorage.ScriptsDirectory);

                    var enableWsl = await ui.DisplayAdminScriptQuestionAsync(
                            "Activating Windows Virtual Machine Platform and Windows Subsystem for Linux requires ADMIN privileges. Are you OK to execute the following script as admin ?",
                            scriptToExecute);
                    if (!enableWsl)
                    {
                        return null;
                    }

                    var restartNeeded = true;
                    var c13 = ui.DisplayCommandLaunch("Activating Windows Virtual Machine Platform and Windows Subsystem for Linux");
                    if (useInstallCommand)
                    {
                        Wsl.install(hostStorage.ScriptsDirectory, hostStorage.LogsDirectory);
                    }
                    else
                    {                        
                        restartNeeded = Windows.EnableWindowsSubsystemForLinux(hostStorage.ScriptsDirectory, hostStorage.LogsDirectory);
                    }
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
                        Wsl.update(hostStorage.ScriptsDirectory, hostStorage.LogsDirectory);
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

                // 4. Check and configure host machine Storage
                bool storageSpecOK = true;

                ui.DisplayInstallStep(4, 6, "Check and configure host storage");

                var drivesInfo = Storage.GetDrivesInfo();
                foreach(var drivePath in drivesInfo.Keys)
                {
                    var driveInfo = drivesInfo[drivePath];  
                    var c17 = ui.DisplayCommandLaunch($"Checking volume '{drivePath}'");
                    ui.DisplayCommandResult(c17, true, $"Volume '{drivePath}' : total size = {driveInfo.TotalSizeMB/ 1000f:F1} GB, free space = {driveInfo.FreeSpaceMB/ 1000f:F1} GB, is SSD = {driveInfo.IsSSD}");
                }

                var c18 = ui.DisplayCommandLaunch($"Checking minimum disk space requirements for cluster software (min {minVmSpec.Storage.ClusterDiskSizeGB + VMRequirements.MIN_HOST_DOWNLOADDIR_GB} GB{(minVmSpec.Storage.ClusterDiskIsSSD?" on SSD":"")}), user data (min {minVmSpec.Storage.DataDiskSizeGB} GB{(minVmSpec.Storage.DataDiskIsSSD ? " on SSD" : "")}), and backups (min {VMRequirements.MIN_HOST_BACKUPDIR_GB} GB)");
                string storageErrorMessage;
                storageSpecOK = VMRequirements.CheckStorageRequirements(minVmSpec, drivesInfo, out storageErrorMessage);
                ui.DisplayCommandResult(c18, storageSpecOK, storageSpecOK ? null : storageErrorMessage);
                if (!storageSpecOK)
                {
                    return null;
                }
                                
                var storageLocations = new StorageLocation[] { StorageLocation.VirtualMachineCluster, StorageLocation.VirtualMachineData, StorageLocation.Backup };
                var storageDescriptions = new string[] { "cluster software", "user data", "backups" };
                var currentDirectories = new string[] { hostStorage.VirtualMachineClusterDirectory, hostStorage.VirtualMachineDataDirectory, hostStorage.BackupDirectory };
                var candidateVolumesForCluster = drivesInfo.Values.Where(di => di.FreeSpaceMB / 1000f > (minVmSpec.Storage.ClusterDiskSizeGB + VMRequirements.MIN_HOST_DOWNLOADDIR_GB) && (!minVmSpec.Storage.ClusterDiskIsSSD || di.IsSSD)).ToList();
                var candidateVolumesForData = drivesInfo.Values.Where(di => di.FreeSpaceMB / 1000f > minVmSpec.Storage.DataDiskSizeGB && (!minVmSpec.Storage.DataDiskIsSSD || di.IsSSD)).ToList();
                var candidateVolumesForBackup = drivesInfo.Values.Where(di => di.FreeSpaceMB / 1000f > VMRequirements.MIN_HOST_BACKUPDIR_GB).ToList();
                var candidateVolumesArray = new List<os.DriveInfo>[] { candidateVolumesForCluster, candidateVolumesForData, candidateVolumesForBackup };
                foreach (var (storageLocation,currentDirectory,candidateVolumes) in storageLocations.Zip(currentDirectories,candidateVolumesArray))
                {                    
                    var volumeCandidates = String.Join(", ", candidateVolumes.Select(di => $"{di.DrivePath} {di.FreeSpaceMB / 1000f:F1} GB free {(di.IsSSD ? "(SDD)" : "")}"));
                    var subdirectory = HostStorage.GetSubdirectoryFor(storageLocation);
                    string defaultPath = null;
                    var currentPathIsOK = candidateVolumes.Any(di => currentDirectory.StartsWith(di.DrivePath));
                    if (currentPathIsOK) 
                    {
                        defaultPath = currentDirectory.Substring(0, currentDirectory.Length - subdirectory.Length);
                    }
                    var storageDescription = storageDescriptions[(int)storageLocation];
                    var targetPath = await ui.DisplayInputQuestionAsync($"Choose a base directory to store the {storageDescription} (a subdirectory '{subdirectory}' will be created). Candidate volumes: {volumeCandidates}", defaultPath);
                    if (!targetPath.Equals(defaultPath))
                    {
                        hostStorage.MoveConfigurableDirectoryTo(storageLocation, targetPath);
                    }
                }
                machineConfig.VirtualMachineClusterPath = hostStorage.VirtualMachineClusterDirectory;
                machineConfig.VirtualMachineDataPath = hostStorage.VirtualMachineDataDirectory;
                machineConfig.BackupPath = hostStorage.BackupDirectory;

                // 5. Configure a sandbox to host the local virtual machines
       
                ui.DisplayInstallStep(5, 6, "Configure a sandbox to host the local virtual machines");

                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                var recVmSpec = vmSpecs.RecommendedVMSpec;
                var maxVmSpec = vmSpecs.MaximumVMSpecOnThisMachine;

                machineConfig.Processors = Int32.Parse(await ui.DisplayInputQuestionAsync($"Maximum number of processors (min {minVmSpec.Compute.Processors}, max {maxVmSpec.Compute.Processors}, recommended {recVmSpec.Compute.Processors})", maxVmSpec.Compute.Processors.ToString()));
                machineConfig.MemoryGB = Int32.Parse(await ui.DisplayInputQuestionAsync($"Maximum memory in GB (min {minVmSpec.Compute.MemoryGB}, max {maxVmSpec.Compute.MemoryGB}, recommended {recVmSpec.Compute.MemoryGB})", maxVmSpec.Compute.MemoryGB.ToString()));

                machineConfig.VirtualMachineClusterSizeGB = Int32.Parse(await ui.DisplayInputQuestionAsync($"Maximum size of cluster software in GB (min {minVmSpec.Storage.ClusterDiskSizeGB}, max {maxVmSpec.Storage.ClusterDiskSizeGB}, recommended {recVmSpec.Storage.ClusterDiskSizeGB})", maxVmSpec.Storage.ClusterDiskSizeGB.ToString()));
                machineConfig.VirtualMachineDataSizeGB = Int32.Parse(await ui.DisplayInputQuestionAsync($"Maximum size of user data in GB (min {minVmSpec.Storage.DataDiskSizeGB}, max {maxVmSpec.Storage.DataDiskSizeGB}, recommended {recVmSpec.Storage.DataDiskSizeGB})", maxVmSpec.Storage.DataDiskSizeGB.ToString()));
                machineConfig.BackupSizeGB = Int32.Parse(await ui.DisplayInputQuestionAsync($"Maximum size of backups in GB (min {VMRequirements.MIN_HOST_BACKUPDIR_GB})", (Storage.GetDriveInfoFromPath(machineConfig.BackupPath).FreeSpaceMB / 1000).ToString()));

                // NO SSH port with WSL on Windows
                // machineConfig.SSHPort = Int32.Parse(await ui.DisplayInputQuestion($"Default SSH port forwarded on host machine", VMRequirements.DEFAULT_HOST_SSH_PORT.ToString()));
                machineConfig.KubernetesPort = Int32.Parse(await ui.DisplayInputQuestionAsync($"Default cluster admin port forwarded on host machine", VMRequirements.DEFAULT_HOST_Kubernetes_PORT.ToString()));
                machineConfig.HttpPort = Int32.Parse(await ui.DisplayInputQuestionAsync($"Default cluster http port forwarded on host machine", VMRequirements.DEFAULT_HOST_HttpIngress_PORT.ToString()));
                machineConfig.CanExposeHttpOnLAN = await ui.DisplayQuestionAsync($"Allow access to cluster http port from other machines on the local network");
                machineConfig.HttpsPort = Int32.Parse(await ui.DisplayInputQuestionAsync($"Default cluster https port forwarded on host machine", VMRequirements.DEFAULT_HOST_HttpsIngress_PORT.ToString()));
                machineConfig.CanExposeHttpsOnLAN = await ui.DisplayQuestionAsync($"Allow access to cluster https port from other machines on the local network");
                
                // 6. Download VM software images
                bool alpineImageOK = true;
                bool ubuntuImageOK = true;
                bool k3sExecutableOK = true;
                bool k3sImagesOK = true;
                bool helmExecutableOK = true;
                bool nerdctlExecutableOK = true;

                ui.DisplayInstallStep(6, 6, "Download Virtual Machine OS images and Kubernetes tools");

                var c27 = new LongRunningCommand($"Downloading Alpine Linux operating system image (v{WslDisk.alpineVersion})", WslDisk.alpineImageDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(WslDisk.alpineImageURL, WslDisk.alpineFileName, gunzip: true, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var alpineFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.alpineFileName));
                        alpineImageOK = alpineFile.Exists && alpineFile.Length == WslDisk.alpineImageDiskSize;
                        displayResult(alpineImageOK);
                    });

                var c28 = new LongRunningCommand($"Downloading Ubuntu Linux operating system image ({WslDisk.ubuntuRelease} {WslDisk.ubuntuVersion})", WslDisk.ubuntuImageDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(WslDisk.ubuntuImageURL, WslDisk.ubuntuFileName, gunzip: true, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var ubuntuFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.ubuntuFileName));
                        ubuntuImageOK = ubuntuFile.Exists && Math.Abs((ubuntuFile.Length - WslDisk.ubuntuImageDiskSize) / (float)WslDisk.ubuntuImageDiskSize) <= 0.25;
                        displayResult(ubuntuImageOK);
                    });

                var c29 = new LongRunningCommand($"Downloading Rancher K3s executable (v{VirtualMachine.k3sVersion})", VirtualMachine.k3sExecutableSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(VirtualMachine.k3sExecutableURL, VirtualMachine.k3sExecutableFileName, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var k3sExecFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.k3sExecutableFileName));
                        k3sExecutableOK = k3sExecFile.Exists && k3sExecFile.Length == VirtualMachine.k3sExecutableSize;
                        displayResult(k3sExecutableOK);
                    });

                var c30 = new LongRunningCommand($"Downloading Rancher K3s containers images (v{VirtualMachine.k3sVersion})", VirtualMachine.k3sImagesDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(VirtualMachine.k3sImagesURL, VirtualMachine.k3sImagesFileName, gunzip: true,
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var k3sImagesFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.k3sImagesFileName));
                        k3sImagesOK = k3sImagesFile.Exists && k3sImagesFile.Length == VirtualMachine.k3sImagesDiskSize;
                        displayResult(k3sImagesOK);
                    });

                var c31 = new LongRunningCommand($"Downloading Helm executable (v{VirtualMachine.helmVersion})", VirtualMachine.helmExecutableDownloadSize, "Bytes",
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

                var c31b = new LongRunningCommand($"Downloading nerdctl and buildkit executables (v{VirtualMachine.nerdctlVersion})", VirtualMachine.nerdctlBundleDownloadSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(VirtualMachine.nerdctlBundleURL, VirtualMachine.nerdctlFileName, gunzip: true,
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        // Extract nerdctl executable from the downloaded tar file
                        var nerdctlExecutablePath = Path.Combine(hostStorage.DownloadCacheDirectory, "nerdctl");
                        var buildctlExecutablePath = Path.Combine(hostStorage.DownloadCacheDirectory, "buildctl");
                        var buildkitdExecutablePath = Path.Combine(hostStorage.DownloadCacheDirectory, "buildkitd");
                        var nerdctlTarFile = Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.nerdctlFileName);
                        var nerdctlTmpDir = Path.Combine(hostStorage.DownloadCacheDirectory, "nerdctl-temp");
                        Directory.CreateDirectory(nerdctlTmpDir);
                        HostStorage.ExtractTarFile(nerdctlTarFile, nerdctlTmpDir);
                        File.Move(Path.Combine(nerdctlTmpDir, "bin", "nerdctl"), nerdctlExecutablePath);
                        File.Move(Path.Combine(nerdctlTmpDir, "bin", "buildctl"), buildctlExecutablePath);
                        File.Move(Path.Combine(nerdctlTmpDir, "bin", "buildkitd"), buildkitdExecutablePath);
                        Directory.Delete(nerdctlTmpDir, true);

                        var nerdctlExecFile = new FileInfo(nerdctlExecutablePath);
                        nerdctlExecutableOK = nerdctlExecFile.Exists && nerdctlExecFile.Length == VirtualMachine.nerdctlExecutableDiskSize;
                        displayResult(helmExecutableOK);
                    });

                ui.RunCommandsAndDisplayProgress(new LongRunningCommand[] { c27, c28, c29, c30, c31, c31b });
                if (!alpineImageOK || !ubuntuImageOK || !k3sExecutableOK || !k3sImagesOK || !helmExecutableOK || !nerdctlExecutableOK)
                {
                    return null;
                }

                return machineConfig;
            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
                return null;
            }
        }

        public static async Task<VirtualMachineConfig> CreateVirtualMachine(VirtualMachineConfig vmConfig, HostMachineConfig hostConfig, HostStorage hostStorage, InstallProcessUI ui)
        {
            try
            {
                var vmName = vmConfig.Name;

                // 1. Create VM disks
                bool virtualDiskClusterOK = true;
                bool virtualDiskDataOK = true;

                ui.DisplayInstallStep(1, 2, "Initialize local virtual machine disks");

                var clusterDisk = WslDisk.TryFindByName(vmConfig.Name, VirtualDiskFunction.Cluster, hostStorage);
                if (clusterDisk == null)
                {
                    var c24 = ui.DisplayCommandLaunch($"Initializing '{vmName}' cluster virtual disk");
                    clusterDisk = WslDisk.CreateFromOSImage(vmConfig.Name, Path.Combine(hostStorage.DownloadCacheDirectory, WslDisk.ubuntuFileName), hostStorage);
                    virtualDiskClusterOK = clusterDisk != null;
                    ui.DisplayCommandResult(c24, virtualDiskClusterOK);

                    if (vmConfig.Spec.GPU != null && vmConfig.Spec.GPU.GPUCount > 0)
                    {
                        var c25 = ui.DisplayCommandLaunch("Installing nvidia GPU software on cluster virtual disk");
                        ((WslDisk)clusterDisk).InstallNvidiaContainerRuntimeOnOSImage(hostStorage);
                        ui.DisplayCommandResult(c25, true);
                    }
                }
                if (!virtualDiskClusterOK) { return null; }

                var dataDisk = WslDisk.TryFindByName(vmConfig.Name, VirtualDiskFunction.Data, hostStorage);
                if (dataDisk == null)
                {
                    var c22 = ui.DisplayCommandLaunch($"Initializing '{vmName}' data virtual disk");
                    dataDisk = WslDisk.CreateBlank(vmConfig.Name, VirtualDiskFunction.Data, hostStorage);
                    virtualDiskDataOK = dataDisk != null;
                    ui.DisplayCommandResult(c22, virtualDiskDataOK);
                }
                if (!virtualDiskDataOK) { return null; }

                // 2. Configure WSL if needed

                ui.DisplayInstallStep(2, 2, "Apply local virtual machine config");

                var wslConfig = Wsl.Read_wslconfig();
                var needToUpdateWslConfig = wslConfig.NeedsToBeUpdatedForVmSpec(hostConfig.Processors, hostConfig.MemoryGB);

                var updateWslConfig = false;
                if (needToUpdateWslConfig)
                {
                    updateWslConfig = await ui.DisplayQuestionAsync($"Do you confirm you want to update the Windows Subsystem for Linux configuration to match your host sandbox configuration: {hostConfig.Processors} processors, {hostConfig.MemoryGB} GB memory ? This will affect all WSL distributions launched from your Windows account, not only wordslab.");
                }
                if (updateWslConfig && Wsl.IsRunning())
                {
                    updateWslConfig = await ui.DisplayQuestionAsync("All WSL distributions currently running will be stopped: please make sure you take all the necessary measures for a graceful shutdown before continuing. Are you ready now ?");
                }
                if (updateWslConfig)
                {
                    var c26 = ui.DisplayCommandLaunch($"Updating Windows Subsystem for Linux configuration to match your host sandbox configuration: {hostConfig.Processors} processors, {hostConfig.MemoryGB} GB memory");
                    wslConfig.UpdateToVMSpec(hostConfig.Processors, hostConfig.MemoryGB, restartIfNeeded: true);
                    ui.DisplayCommandResult(c26, true);
                }
                else
                {
                    var c27 = ui.DisplayCommandLaunch("No changes were applied to the Windows Subsystem for Linux configuration");
                    ui.DisplayCommandResult(c27, true);
                }

                return vmConfig;
            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
                return null;
            }
        }            
    }
}
