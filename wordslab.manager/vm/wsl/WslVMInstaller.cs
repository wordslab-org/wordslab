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
                var c1 = ui.DisplayCommandLaunch("Checking if a recent Nvidia GPU (> GTX 1050) is available [optional]");
                string? gpu = null;
                bool gpuAvailable = false;
                gpu = Wsl.GetNvidiaGPUAvailableForWSL2();
                gpuAvailable = gpu != null;
                ui.DisplayCommandResult(c1, gpuAvailable, gpu);

                var useGpu = false;
                if (gpuAvailable)
                {
                    // Usage will tell us if this question is useful or not ?
                    // useGpu = await ui.DisplayQuestionAsync("Do you want to use this GPU in your local cluster ?");
                    useGpu = true;
                    if (useGpu)
                    {
                        var c1_2 = ui.DisplayCommandLaunch("Checking Nvidia driver version");
                        var driverVersionOK = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
                        ui.DisplayCommandResult(c1_2, driverVersionOK);

                        if (!driverVersionOK)
                        {
                            var c1_3 = ui.DisplayCommandLaunch("Please update your Nvidia driver to the latest version. Trying to launch Geforce experience ...");
                            Nvidia.TryOpenNvidiaUpdate();
                            ui.DisplayCommandResult(c1_3, true, "Go to the Pilots section and check if a newer version is available");

                            var driverUpdateOK = await ui.DisplayQuestionAsync("Did you manage to update the Nvidia driver to the latest version ?");
                            if (!driverUpdateOK)
                            {
                                return null;
                            }

                            var c1_4 = ui.DisplayCommandLaunch("Checking Nvidia driver version");
                            driverVersionOK = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
                            ui.DisplayCommandResult(c1_4, driverVersionOK, driverVersionOK ? null : "Nvidia driver version still not OK: please restart the install process without using the GPU");
                            if (!driverVersionOK)
                            {
                                return null;
                            }
                        }
                    }
                }

                var windowsVersionOk = false;
                if (!useGpu)
                {
                    var c2 = ui.DisplayCommandLaunch("Checking if operating system version is Windows 10 version 1903 or higher");
                    windowsVersionOk = Wsl.IsWindowsVersionOKForWSL2();
                    ui.DisplayCommandResult(c2, windowsVersionOk);
                }
                else
                {
                    var c2 = ui.DisplayCommandLaunch("Checking if operating system version is Windows 10 version 21H2 or higher");
                    windowsVersionOk = Wsl.IsWindowsVersionOKForWSL2WithGPU();
                    ui.DisplayCommandResult(c2, windowsVersionOk);
                }
                if (!windowsVersionOk)
                {
                    var c3 = ui.DisplayCommandLaunch("Launching Windows Update to upgrade operating system version");
                    ui.DisplayCommandResult(c3, true, "Please update Windows, reboot your machine if necessary, then launch this installer again");
                    Windows.OpenWindowsUpdate();
                    return  null;
                }

                var c4 = ui.DisplayCommandLaunch("Checking if Windows Subsystem for Linux 2 is already installed");
                var wsl2Installed = Wsl.IsWSL2AlreadyInstalled();
                ui.DisplayCommandResult(c4, wsl2Installed);

                if (!wsl2Installed)
                {
                    var c5 = ui.DisplayCommandLaunch("Checking if virtualization is enabled on this machine");
                    var cpuInfo = Compute.GetCPUInfo();
                    var virtualizationEnabled = Compute.IsCPUVirtualizationAvailable(cpuInfo);
                    ui.DisplayCommandResult(c5, virtualizationEnabled, virtualizationEnabled ? null :
                        "Please go to Windows Settings > Update & Security > Recovery (left menu) > Advanced Startup > Restart Now," +
                        " then select: Troubleshoot > Advanced options > UEFI firmware settings, and navigate menus to enable virtualization");
                    if (!virtualizationEnabled)
                    {
                        Windows.OpenWindowsUpdate();
                        return null;
                    }

                    var enableWsl = await ui.DisplayAdminScriptQuestionAsync(
                        "Activating Windows Virtual Machine Platform and Windows Subsystem for Linux requires ADMIN privileges. Are you OK to execute the following script as admin ?",
                        Windows.EnableWindowsSubsystemForLinux_script(hostStorage.ScriptsDirectory));
                    if (!enableWsl)
                    {
                        return null;
                    }

                    var c6 = ui.DisplayCommandLaunch("Activating Windows Virtual Machine Platform and Windows Subsystem for Linux");
                    var restartNeeded = Windows.EnableWindowsSubsystemForLinux(hostStorage.ScriptsDirectory, hostStorage.LogsDirectory);
                    ui.DisplayCommandResult(c6, true);
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

                var kernelVersionOK = true;
                if (useGpu)
                {
                    var c7 = ui.DisplayCommandLaunch("Checking Windows Subsystem for Linux kernel version (GPU support)");
                    kernelVersionOK = Wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
                    ui.DisplayCommandResult(c7, kernelVersionOK);
                }

                var updateKernel = true;
                if (kernelVersionOK)
                {
                    // Usage will tell us if this question is useful or not ?
                    // updateKernel = await ui.DisplayQuestionAsync("Do you want to update Windows Subsystem for Linux to the latest version ?");
                    updateKernel = false;
                }
                if (updateKernel)
                {
                    var c8 = ui.DisplayCommandLaunch("Updating Windows Subsystem for Linux kernel to the latest version");
                    Wsl.UpdateLinuxKernelVersion();
                    ui.DisplayCommandResult(c8, true);

                    if (useGpu)
                    {
                        var c9 = ui.DisplayCommandLaunch("Checking Windows Subsystem for Linux kernel version (GPU support)");
                        kernelVersionOK = Wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
                        ui.DisplayCommandResult(c9, kernelVersionOK, kernelVersionOK ? null : "Windows Subsystem for Linux kernel version still not OK: please restart the install process without using the GPU");
                        if (!kernelVersionOK)
                        {
                            return null;
                        }
                    }
                }

                // -- Versions last updated : January 9 2022 --

                // Alpine mini root filesystem: https://alpinelinux.org/downloads/
                var alpineVersion = "3.15.0";
                var alpineImageURL = $"https://dl-cdn.alpinelinux.org/alpine/v{alpineVersion.Substring(0, 4)}/releases/x86_64/alpine-minirootfs-{alpineVersion}-x86_64.tar.gz";
                var alpineImageSize = 5867520; // 2731445 compressed
                var alpineFileName = $"alpine-{alpineVersion}.tar";

                // Ubuntu minimum images: https://partner-images.canonical.com/oci/
                var ubuntuRelease = "focal";
                var ubuntuVersion = "20220105";
                var ubuntuImageURL = $"https://partner-images.canonical.com/oci/{ubuntuRelease}/{ubuntuVersion}/ubuntu-{ubuntuRelease}-oci-amd64-root.tar.gz";
                var ubuntuImageSize = 78499840; // 27746207 compressed
                var ubuntuFileName = $"ubuntu-{ubuntuRelease}-{ubuntuVersion}.tar";

                // Rancher k3s releases: https://github.com/k3s-io/k3s/releases/
                var k3sVersion = "1.22.5+k3s1";
                var k3sExecutableURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s";
                var k3sExecutableSize = 53473280;
                var k3sExecutableFileName = $"k3s-{k3sVersion}";
                var k3sImagesURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s-airgap-images-amd64.tar";
                var k3sImagesSize = 492856320;
                var k3sImagesFileName = $"k3s-airgap-images-{k3sVersion}.tar";

                // Helm releases: https://github.com/helm/helm/releases
                var helmVersion = "3.7.2";
                var helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
                var helmExecutableSize = 45731840; // 13870692 compressed
                var helmFileName = $"heml-{helmVersion}.tar";

                // nvidia container runtime versions: https://github.com/NVIDIA/nvidia-container-runtime/releases
                var nvidiaContainerRuntimeVersion = "3.7.0-1";

                var c10 = ui.DisplayCommandLaunchWithProgress($"Downloading Alpine Linux operating system image (v{alpineVersion})", alpineImageSize, "Bytes");
                var download1 = hostStorage.DownloadFileWithCache(alpineImageURL, alpineFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c10, totalBytesDownloaded));

                var c11 = ui.DisplayCommandLaunchWithProgress($"Downloading Ubuntu Linux operating system image ({ubuntuRelease} {ubuntuVersion})", ubuntuImageSize, "Bytes");
                var download2 = hostStorage.DownloadFileWithCache(ubuntuImageURL, ubuntuFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c11, totalBytesDownloaded));

                var c12 = ui.DisplayCommandLaunchWithProgress($"Downloading Rancher K3s executable (v{k3sVersion})", k3sExecutableSize, "Bytes");
                var download3 = hostStorage.DownloadFileWithCache(k3sExecutableURL, k3sExecutableFileName, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c12, totalBytesDownloaded));

                var c13 = ui.DisplayCommandLaunchWithProgress($"Downloading Rancher K3s containers images (v{k3sVersion})", k3sImagesSize, "Bytes");
                var download4 = hostStorage.DownloadFileWithCache(k3sImagesURL, k3sImagesFileName, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c13, totalBytesDownloaded));

                var c14 = ui.DisplayCommandLaunchWithProgress($"Downloading Helm executable (v{helmVersion})", helmExecutableSize, "Bytes");
                var download5 = hostStorage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c14, totalBytesDownloaded));

                Task.WaitAll(download1, download2, download3, download4, download5);

                // Extract helm executable from the downloaded tar file
                var helmExecutablePath = Path.Combine(hostStorage.DownloadCacheDirectory, "helm");
                if (!File.Exists(helmExecutablePath))
                {
                    var helmTarFile = Path.Combine(hostStorage.DownloadCacheDirectory, helmFileName);
                    var helmTmpDir = Path.Combine(hostStorage.DownloadCacheDirectory, "helm-temp");
                    Directory.CreateDirectory(helmTmpDir);
                    HostStorage.ExtractTarFile(helmTarFile, helmTmpDir);
                    File.Move(Path.Combine(helmTmpDir, "linux-amd64", "helm"), helmExecutablePath);
                    Directory.Delete(helmTmpDir, true);
                }

                // --- Initialize and start VM ---

                var wslDistribs = Wsl.list();
                var cachePath = hostStorage.DownloadCacheDirectory;

                if (!wslDistribs.Any(d => d.Distribution == "wordslab-data"))
                {
                    var c15 = ui.DisplayCommandLaunch("Initializing wordslab-data virtual disk");
                    Wsl.import("wordslab-data", hostStorage.VirtualMachineDataDirectory, Path.Combine(cachePath, alpineFileName), 2);
                    Wsl.execShell($"cp $(wslpath '{cachePath}')/wordslab-data-init.sh /root/wordslab-data-init.sh", "wordslab-data");
                    Wsl.execShell("chmod a+x /root/wordslab-data-init.sh", "wordslab-data");
                    Wsl.execShell($"/root/wordslab-data-init.sh '{cachePath}'", "wordslab-data");
                    Wsl.terminate("wordslab-data");
                    ui.DisplayCommandResult(c15, true);
                }

                if (!wslDistribs.Any(d => d.Distribution == "wordslab-cluster"))
                {
                    var c16 = ui.DisplayCommandLaunch("Initializing wordslab-cluster virtual disk");
                    Wsl.import("wordslab-cluster", hostStorage.VirtualMachineClusterDirectory, Path.Combine(cachePath, alpineFileName), 2);
                    Wsl.execShell($"cp $(wslpath '{cachePath}')/wordslab-cluster-init.sh /root/wordslab-cluster-init.sh", "wordslab-cluster");
                    Wsl.execShell("chmod a+x /root/wordslab-cluster-init.sh", "wordslab-cluster");
                    Wsl.execShell($"/root/wordslab-cluster-init.sh '{cachePath}' '{k3sImagesFileName}'", "wordslab-cluster");
                    Wsl.terminate("wordslab-cluster");
                    ui.DisplayCommandResult(c16, true);
                }

                if (!wslDistribs.Any(d => d.Distribution == "wordslab-os"))
                {
                    var c17 = ui.DisplayCommandLaunch("Initializing wordslab-os virtual machine");
                    Wsl.import("wordslab-os", hostStorage.VirtualMachineOSDirectory, Path.Combine(cachePath, ubuntuFileName), 2);
                    Wsl.execShell($"cp $(wslpath '{cachePath}')/wordslab-os-init.sh /root/wordslab-os-init.sh", "wordslab-os");
                    Wsl.execShell("chmod a+x /root/wordslab-os-init.sh", "wordslab-os");
                    ui.DisplayCommandResult(c17, true);

                    var c18 = ui.DisplayCommandLaunch("Installing Rancher K3s software inside virtual machine");
                    Wsl.execShell($"/root/wordslab-os-init.sh '{cachePath}' '{k3sExecutableFileName}' '{helmFileName}'", "wordslab-os", ignoreError: "perl: warning");
                    ui.DisplayCommandResult(c18, true);

                    if (useGpu)
                    {
                        var c19 = ui.DisplayCommandLaunch("Installing nvidia GPU software inside virtual machine");
                        Wsl.execShell($"/root/wordslab-cluster-start.sh", "wordslab-cluster");
                        Wsl.execShell("nvidia-smi -L", "wordslab-os");
                        Wsl.execShell($"/root/wordslab-gpu-init.sh '{cachePath}' '{nvidiaContainerRuntimeVersion}'", "wordslab-os", ignoreError: "perl: warning");
                        Wsl.terminate("wordslab-cluster");
                        ui.DisplayCommandResult(c19, true);
                    }

                    Wsl.terminate("wordslab-os");
                }

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
