using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuVMInstaller
    {
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

                var c3 = ui.DisplayCommandLaunch($"Host machine with at least {vmSpec.VmDiskSizeGB + vmSpec.ClusterDiskSizeGB + vmSpec.DataDiskSizeGB} (VM) + {VirtualMachineSpec.MIN_HOST_DISK_GB + VirtualMachineSpec.MIN_HOST_DOWNLOADDIR_GB} (host) GB free storage space");
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
                ui.DisplayCommandResult(c4, cpuVirtualization, cpuVirtualization ? null : "Please reboot to the UEFI or BIOS settings of your machine and enable the CPU virtualization instructions");
                if (!cpuVirtualization)
                {
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

                // 4. Download VM software images
                bool ubuntuImageOK = true;
                bool k3sExecutableOK = true;
                bool k3sImagesOK = true;
                bool helmExecutableOK = true;

                // wget https://cloud-images.ubuntu.com/minimal/releases/focal/release-20220201/ubuntu-20.04-minimal-cloudimg-amd64.img


                ui.DisplayInstallStep(4, 6, "Download Virtual Machine OS images and Kubernetes tools");

                var c18 = new LongRunningCommand($"Downloading Ubuntu Linux operating system image ({QemuDisk.ubuntuRelease} {QemuDisk.ubuntuVersion})", QemuDisk.ubuntuImageSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(QemuDisk.ubuntuImageURL, QemuDisk.ubuntuFileName, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult => {
                        var ubuntuFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, QemuDisk.ubuntuFileName));
                        ubuntuImageOK = ubuntuFile.Exists && ubuntuFile.Length == QemuDisk.ubuntuImageSize;
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

                var c20 = new LongRunningCommand($"Downloading Rancher K3s containers images (v{VirtualMachine.k3sVersion})", VirtualMachine.k3sImagesSize, "Bytes",
                    displayProgress => hostStorage.DownloadFileWithCache(VirtualMachine.k3sImagesURL, VirtualMachine.k3sImagesFileName, 
                                        progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                    displayResult =>
                    {
                        var k3sImagesFile = new FileInfo(Path.Combine(hostStorage.DownloadCacheDirectory, VirtualMachine.k3sImagesFileName));
                        k3sImagesOK = k3sImagesFile.Exists && k3sImagesFile.Length == VirtualMachine.k3sImagesSize;
                        displayResult(k3sImagesOK);
                    });

                var c21 = new LongRunningCommand($"Downloading Helm executable (v{VirtualMachine.helmVersion})", VirtualMachine.helmExecutableSize, "Bytes",
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
                        helmExecutableOK = helmExecFile.Exists && helmExecFile.Length == VirtualMachine.helmExtractedSize;
                        displayResult(helmExecutableOK);
                    });

                ui.RunCommandsAndDisplayProgress(new LongRunningCommand[] { c18, c19, c20, c21 });                

                if (!ubuntuImageOK || !k3sExecutableOK || !k3sImagesOK || !helmExecutableOK)
                {
                    return null;
                }

                // 5. Create VM disks
                bool virtualDiskVMOK = true;
                bool virtualDiskClusterOK = true;
                bool virtualDiskDataOK = true;

                ui.DisplayInstallStep(5, 6, "Initialize wordslab VM virtual disks");

                var dataDisk = QemuDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.Data, hostStorage);
                if (dataDisk == null)
                {
                    var c22 = ui.DisplayCommandLaunch("Initializing wordslab data virtual disk");
                    dataDisk = QemuDisk.CreateBlank(vmSpec.Name, VirtualDiskFunction.Data, vmSpec.DataDiskSizeGB, hostStorage);
                    virtualDiskDataOK = dataDisk != null;
                    ui.DisplayCommandResult(c22, virtualDiskDataOK);
                }
                if (!virtualDiskDataOK) { return null; }

                var clusterDisk = QemuDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.Cluster, hostStorage);
                if (clusterDisk == null)
                {
                    var c23 = ui.DisplayCommandLaunch("Initializing wordslab cluster virtual disk");
                    clusterDisk = QemuDisk.CreateBlank(vmSpec.Name, VirtualDiskFunction.Cluster, vmSpec.ClusterDiskSizeGB, hostStorage);
                    virtualDiskClusterOK = clusterDisk != null;
                    ui.DisplayCommandResult(c23, virtualDiskClusterOK);
                }
                if (!virtualDiskClusterOK) { return null; }

                var osDisk = QemuDisk.TryFindByName(vmSpec.Name, VirtualDiskFunction.OS, hostStorage);
                if (osDisk == null)
                {
                    var c24 = ui.DisplayCommandLaunch("Initializing wordslab VM virtual disk");
                    osDisk = QemuDisk.CreateFromOSImage(vmSpec.Name, Path.Combine(hostStorage.DownloadCacheDirectory, QemuDisk.ubuntuFileName), vmSpec.VmDiskSizeGB, hostStorage);
                    virtualDiskVMOK = osDisk != null;
                    ui.DisplayCommandResult(c24, virtualDiskVMOK);
                }
                if (!virtualDiskVMOK) { return null; }

                /*if (createVMWithGPUSupport)
                {
                    var c25 = ui.DisplayCommandLaunch("Installing nvidia GPU software on VM virtual disk");
                    ((WslDisk)osDisk).InstallNvidiaContainerRuntimeOnOSImage(clusterDisk);
                    ui.DisplayCommandResult(c25, true);
                }*/

                // 6. Configure and start local Virtual Machine
                bool vmConfigOK = true;
                bool vmInitOK = true;

                ui.DisplayInstallStep(6, 6, "Configure and start wordslab Virtual Machine");

                var localVM = QemuVM.TryFindByName(vmSpec.Name, hostStorage);

                var c27 = ui.DisplayCommandLaunch("Launching wordslab virtual machine and k3s cluster");
                VMEndpoint localVMEndpoint = null;
                if (!localVM.IsRunning())
                {
                    localVMEndpoint = localVM.Start(vmSpec);
                }
                else
                {
                    localVMEndpoint = localVM.Endpoint;
                }

                ui.DisplayCommandResult(c27, true, $"Virtual machine started : IP = {localVMEndpoint.IPAddress}, SSH port = {localVMEndpoint.SSHPort}");

                return localVM;
            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
                return null;
            }
        }

        public static async Task<bool> Uninstall(string vmName, HostStorage hostStorage, InstallProcessUI ui)
        {
            try
            {
                var localVM = QemuVM.TryFindByName(vmName, hostStorage);

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
