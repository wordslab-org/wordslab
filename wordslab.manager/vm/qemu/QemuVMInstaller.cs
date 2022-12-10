using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public static class QemuVMInstaller
    {
        // Note: before calling this method
        // - you must configure HostStorage directories location
        // - you must ask the user if they want to use a GPU
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
                bool osVersionOK = true;
                bool nativeHypervisorOK = true;
                bool nvidiaDriverVersionOK = true;

                ui.DisplayInstallStep(2, 6, "Check operating system requirements");

                if (OS.IsLinux)
                {
                    var c7 = ui.DisplayCommandLaunch("Checking if operating system version is Ubuntu x64 version 1804 or higher");
                    osVersionOK = Qemu.IsOsVersionOKForQemu();
                    ui.DisplayCommandResult(c7, osVersionOK, osVersionOK ? "" : $"Running {Linux.GetOSDistribution()} version {OS.GetOSVersion()}");

                    if (!osVersionOK)
                    {
                        if (Linux.GetOSDistribution() == "ubuntu")
                        {
                            var c8 = ui.DisplayCommandLaunch("Please execute the following command to upgrade your Ubuntu installation: sudo do-release-upgrade -d");
                            ui.DisplayCommandResult(c8, false);
                        }
                        return null;
                    }

                    var c7_1 = ui.DisplayCommandLaunch("Checking if Linux native hypervisor KVM (Kernel-based Virtual Machine) is available");
                    nativeHypervisorOK = OS.IsNativeHypervisorAvailable();
                    ui.DisplayCommandResult(c7_1, nativeHypervisorOK);

                    if (!nativeHypervisorOK)
                    {
                        var c8_1 = ui.DisplayCommandLaunch("Please refer to the following documentation to activate Linux KVM: https://wiki.ubuntu.com/kvm");
                        ui.DisplayCommandResult(c8_1, false);
                        return null;
                    }
                }
                else if (OS.IsMacOS)
                {
                    var c7 = ui.DisplayCommandLaunch("Checking if operating system version is MacOS x64 version Catalina (10.15) or higher");
                    osVersionOK = Qemu.IsOsVersionOKForQemu();
                    ui.DisplayCommandResult(c7, osVersionOK, osVersionOK ? "" : $"Running MacOS version {OS.GetOSVersion()}");

                    if (!osVersionOK)
                    {
                        var c8 = ui.DisplayCommandLaunch("Please follow this procedure to upgrade your MacOS installation: go to System Preferences / Software Update, click Upgrade Now");
                        ui.DisplayCommandResult(c8, false);
                        return null;
                    }

                    var c7_1 = ui.DisplayCommandLaunch("Checking if MacOS native hypervisor (Hypervisor Framework) is available");
                    nativeHypervisorOK = OS.IsNativeHypervisorAvailable();
                    ui.DisplayCommandResult(c7_1, nativeHypervisorOK);

                    if (!nativeHypervisorOK)
                    {
                        var c8_1 = ui.DisplayCommandLaunch("Please refer to the following documentation to activate Apple Hypervisor: https://developer.apple.com/documentation/hypervisor");
                        ui.DisplayCommandResult(c8_1, false);
                        return null;
                    }
                }

                if (createVMWithGPUSupport)
                {
                    if (OS.IsLinux)
                    {
                        var c6 = ui.DisplayCommandLaunch("Virtual Machine with GPU is not yet supported by wordslab on Linux in this version, it will be implemented as soon as possible");
                        ui.DisplayCommandResult(c6, false);
                        return null;
                    }
                    else if (OS.IsMacOS)
                    {
                        var c6 = ui.DisplayCommandLaunch("Virtual Machine with Nvidia GPU support is not planned for wordslab on MacOS");
                        ui.DisplayCommandResult(c6, false);
                        return null;
                    }

                    var c9 = ui.DisplayCommandLaunch("Checking Nvidia driver version");
                    var driverVersion = Nvidia.GetDriverVersion();
                    nvidiaDriverVersionOK = Nvidia.IsNvidiaDriverForLinux13Dec21OrLater(driverVersion);
                    ui.DisplayCommandResult(c9, nvidiaDriverVersionOK);

                    if (!nvidiaDriverVersionOK)
                    {
                        var c10 = ui.DisplayCommandLaunch("Please update your Nvidia driver to the latest version by running this command, then reboot: sudo ubuntu-drivers autoinstall");
                        ui.DisplayCommandResult(c10, false);
                        return null;
                    }
                }

                // 3. Check Host software dependencies
                bool packageManagerInstalled = true;
                bool cdRomToolInstalled = true;
                bool qemuInstalled = true;

                ui.DisplayInstallStep(3, 6, "Check host software dependencies");

                if (OS.IsLinux)
                {
                    var c12 = ui.DisplayCommandLaunch("Checking if apt package manager is available");
                    packageManagerInstalled = Linux.IsAptPackageManagerAvailable();
                    ui.DisplayCommandResult(c12, packageManagerInstalled);

                    if (!packageManagerInstalled)
                    {
                        var c13 = ui.DisplayCommandLaunch("Please install apt on your system by executing the following command: " + Linux.GetAptInstallCommand());
                        ui.DisplayCommandResult(c13, false);
                        return null;
                    }
                }
                else if (OS.IsMacOS)
                {
                    var c12 = ui.DisplayCommandLaunch("Checking if Homebrew package manager is available");
                    packageManagerInstalled = MacOS.IsHomebrewPackageManagerAvailable();
                    ui.DisplayCommandResult(c12, packageManagerInstalled);

                    if (!packageManagerInstalled)
                    {
                        var c13 = ui.DisplayCommandLaunch("Please install Homebrew on your system by executing the following command: " + MacOS.GetHomebrewInstallCommand());
                        ui.DisplayCommandResult(c13, false);
                        return null;
                    }
                }

                var c14 = ui.DisplayCommandLaunch("Checking if virtual disk image tool is installed");
                cdRomToolInstalled = Qemu.IsCDRomToolInstalled();
                ui.DisplayCommandResult(c14, cdRomToolInstalled);
                if (OS.IsMacOS)
                {
                    var c14_1 = ui.DisplayCommandLaunch("Installing virtual disk image tool");
                    Qemu.InstallCDRomTool();
                    ui.DisplayCommandResult(c14_1, true);
                }

                var c15 = ui.DisplayCommandLaunch("Checking if a recent version of qemu virtual machine emulator is installed");
                qemuInstalled = Qemu.IsInstalled() && Qemu.IsInstalledVersionSupported();
                ui.DisplayCommandResult(c15, qemuInstalled);
                if (OS.IsMacOS)
                {
                    var c15_1 = ui.DisplayCommandLaunch("Installing qemu virtual machine emulator");
                    Qemu.Install();
                    ui.DisplayCommandResult(c15_1, true);
                }

                if (OS.IsLinux && (!cdRomToolInstalled || !qemuInstalled))
                {
                    var c16 = ui.DisplayCommandLaunch("Please install qemu on your system by executing the following command: " + Qemu.GetLinuxInstallCommand());
                    ui.DisplayCommandResult(c16, false);
                    return null;
                }

                // 4. Check and configure host machine Storage
                bool storageSpecOK = true;

                ui.DisplayInstallStep(4, 6, "Check and configure host storage");

                var drivesInfo = Storage.GetDrivesInfo();
                foreach (var drivePath in drivesInfo.Keys)
                {
                    var driveInfo = drivesInfo[drivePath];
                    var c17 = ui.DisplayCommandLaunch($"Checking volume '{drivePath}'");
                    ui.DisplayCommandResult(c17, true, $"Volume '{drivePath}' : total size = {driveInfo.TotalSizeMB / 1000f:F1} GB, free space = {driveInfo.FreeSpaceMB / 1000f:F1} GB, is SSD = {driveInfo.IsSSD}");
                }

                var c18 = ui.DisplayCommandLaunch($"Checking minimum disk space requirements for cluster software (min {minVmSpec.Storage.ClusterDiskSizeGB + VMRequirements.MIN_HOST_DOWNLOADDIR_GB} GB{(minVmSpec.Storage.ClusterDiskIsSSD ? " on SSD" : "")}), user data (min {minVmSpec.Storage.DataDiskSizeGB} GB{(minVmSpec.Storage.DataDiskIsSSD ? " on SSD" : "")}), and backups (min {VMRequirements.MIN_HOST_BACKUPDIR_GB} GB)");
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
                foreach (var (storageLocation, currentDirectory, candidateVolumes) in storageLocations.Zip(currentDirectories, candidateVolumesArray))
                {
                    var volumeCandidates = String.Join(", ", candidateVolumes.Select(di => $"{di.DrivePath} [{(di.IsSSD ? "SDD" : "HDD")}] [{di.FreeSpaceMB / 1000f:F1} GB free]"));
                    var currentPathIsOK = candidateVolumes.Any(di => currentDirectory.StartsWith(di.DrivePath));
                    var defaultPath = currentPathIsOK ? currentDirectory : null;
                    var storageDescription = storageDescriptions[(int)storageLocation];
                    var targetPath = await ui.DisplayInputQuestionAsync($"Choose a base directory to store the {storageDescription} (a subdirectory {HostStorage.GetSubdirectoryFor(StorageLocation.VirtualMachineCluster)} will be created). Candidate volumes: ${volumeCandidates})", defaultPath);
                    if (!targetPath.Equals(currentDirectory))
                    {
                        hostStorage.MoveConfigurableDirectoryTo(storageLocation, targetPath);
                    }
                }
                machineConfig.VirtualMachineClusterPath = hostStorage.VirtualMachineDataDirectory;
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
                machineConfig.BackupSizeGB = Int32.Parse(await ui.DisplayInputQuestionAsync($"Maximum size of backups in GB (min {VMRequirements.MIN_HOST_BACKUPDIR_GB})", (Storage.GetDriveInfoFromPath(machineConfig.BackupPath).FreeSpaceMB/1000).ToString()));

                machineConfig.SSHPort = Int32.Parse(await ui.DisplayInputQuestionAsync($"Default SSH port forwarded on host machine", VMRequirements.DEFAULT_HOST_SSH_PORT.ToString()));
                machineConfig.KubernetesPort = Int32.Parse(await ui.DisplayInputQuestionAsync($"Default cluster admin port forwarded on host machine", VMRequirements.DEFAULT_HOST_Kubernetes_PORT.ToString()));
                machineConfig.HttpPort = Int32.Parse(await ui.DisplayInputQuestionAsync($"Default cluster http port forwarded on host machine", VMRequirements.DEFAULT_HOST_HttpIngress_PORT.ToString()));
                machineConfig.CanExposeHttpOnLAN = await ui.DisplayQuestionAsync($"Allow access to cluster http port from other machines on the local network");
                machineConfig.HttpsPort = Int32.Parse(await ui.DisplayInputQuestionAsync($"Default cluster https port forwarded on host machine", VMRequirements.DEFAULT_HOST_HttpsIngress_PORT.ToString()));
                machineConfig.CanExposeHttpsOnLAN = await ui.DisplayQuestionAsync($"Allow access to cluster https port from other machines on the local network");

                // 6. Download VM software images
                bool ubuntuImageOK = true;
                bool k3sExecutableOK = true;
                bool k3sImagesOK = true;
                bool helmExecutableOK = true;

                ui.DisplayInstallStep(6, 6, "Download Virtual Machine OS images and Kubernetes tools");

                var c28 = new LongRunningCommand($"Downloading Ubuntu Linux operating system image ({QemuDisk.ubuntuRelease} {QemuDisk.ubuntuVersion})", QemuDisk.ubuntuImageSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(QemuDisk.ubuntuImageURL, QemuDisk.ubuntuFileName,
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var ubuntuFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, QemuDisk.ubuntuFileName));
                        ubuntuImageOK = ubuntuFile.Exists && ubuntuFile.Length == QemuDisk.ubuntuImageSize;
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

                ui.RunCommandsAndDisplayProgress(new LongRunningCommand[] { c28, c29, c30, c31 });

                if (!ubuntuImageOK || !k3sExecutableOK || !k3sImagesOK || !helmExecutableOK)
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

                ui.DisplayInstallStep(1, 1, "Initialize local virtual machine disks");

                var clusterDisk = QemuDisk.TryFindByName(vmConfig.Name, VirtualDiskFunction.Cluster, hostStorage);
                if (clusterDisk == null)
                {
                    var c24 = ui.DisplayCommandLaunch($"Initializing '{vmName}' cluster virtual disk");
                    clusterDisk = QemuDisk.CreateFromOSImage(vmConfig.Name, Path.Combine(hostStorage.DownloadCacheDirectory, QemuDisk.ubuntuFileName), vmConfig.Spec.Storage.ClusterDiskSizeGB, hostStorage);
                    virtualDiskClusterOK = clusterDisk != null;
                    ui.DisplayCommandResult(c24, virtualDiskClusterOK);
                }
                if (!virtualDiskClusterOK) { return null; }

                // Nvidia GPU not yet supported with qemu on Linux
                /*if (createVMWithGPUSupport)
                {
                    var c25 = ui.DisplayCommandLaunch("Installing nvidia GPU software on cluster virtual disk");
                    ((WslDisk)osDisk).InstallNvidiaContainerRuntimeOnOSImage(clusterDisk);
                    ui.DisplayCommandResult(c25, true);
                }*/

                var dataDisk = QemuDisk.TryFindByName(vmConfig.Name, VirtualDiskFunction.Data, hostStorage);
                if (dataDisk == null)
                {
                    var c22 = ui.DisplayCommandLaunch($"Initializing '{vmName}' data virtual disk");
                    dataDisk = QemuDisk.CreateBlank(vmConfig.Name, vmConfig.Spec.Storage.DataDiskSizeGB, hostStorage);
                    virtualDiskDataOK = dataDisk != null;
                    ui.DisplayCommandResult(c22, virtualDiskDataOK);
                }
                if (!virtualDiskDataOK) { return null; }

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
