using wordslab.installer.infrastructure.commands;
using wordslab.installer.localstorage;

namespace wordslab.installer.infrastructure
{
    // https://blazor-university.com/
    // https://jonhilton.net/blazor-dynamic-components/
    public interface InstallationUI
    {
        int DisplayCommandLaunch(string commandDescription);

        int DisplayCommandLaunchWithProgress(string commandDescription, long maxValue, string unit);

        void DisplayCommandProgress(int commandId, long currentValue);

        void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage= null);

        Task<bool> DisplayQuestionAsync(string question);

        Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent);
    }

    public class WindowsLocalVMClusterInstall
    {
        public static async Task<bool> Install(InstallationUI ui)
        {

            var c1 = ui.DisplayCommandLaunch("Checking if a recent Nvidia GPU (> GTX 1050) is available [optional]");
            var gpu = wsl.GetNvidiaGPUAvailableForWSL2();
            var gpuAvailable = gpu != null;
            ui.DisplayCommandResult(c1, gpuAvailable, gpu);

            var useGpu = false;
            if (gpuAvailable)
            {
                useGpu = await ui.DisplayQuestionAsync("Do you want to use this GPU in your local cluster ?");
                if (useGpu)
                {
                    var c1_2 = ui.DisplayCommandLaunch("Checking Nvidia driver version");
                    var driverVersionOK = wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
                    ui.DisplayCommandResult(c1_2, driverVersionOK);
                    
                    if(!driverVersionOK)
                    {
                        var c1_3 = ui.DisplayCommandLaunch("Please update your Nvidia driver to the latest version. Trying to launch Geforce experience ...");
                        nvidia.TryOpenNvidiaUpdate();
                        ui.DisplayCommandResult(c1_3, true, "Go to the Pilots section and check if a newer version is available");

                        var driverUpdateOK = await ui.DisplayQuestionAsync("Did you manage to update the Nvidia driver to the latest version ?");
                        if (!driverUpdateOK)
                        {
                            return false;
                        }

                        var c1_4 = ui.DisplayCommandLaunch("Checking Nvidia driver version");
                        driverVersionOK = wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
                        ui.DisplayCommandResult(c1_4, driverVersionOK, driverVersionOK?null:"Nvidia driver version still not OK: please restart the install process without using the GPU");
                        if (!driverVersionOK)
                        {
                            return false;
                        }
                    }
                }
            }

            var windowsVersionOk = false;
            if (!useGpu) 
            {
                var c2 = ui.DisplayCommandLaunch("Checking if operating system version is Windows 10 version 1903 or higher");
                windowsVersionOk = wsl.IsWindowsVersionOKForWSL2();
                ui.DisplayCommandResult(c2, windowsVersionOk);
            } 
            else
            {
                var c2 = ui.DisplayCommandLaunch("Checking if operating system version is Windows 10 version 21H2 or higher");
                windowsVersionOk = wsl.IsWindowsVersionOKForWSL2WithGPU();
                ui.DisplayCommandResult(c2, windowsVersionOk);
            }
            if(!windowsVersionOk)
            {
                var c3 = ui.DisplayCommandLaunch("Launching Windows Update to upgrade operating system version");
                ui.DisplayCommandResult(c3, true, "Please update Windows, reboot your machine if necessary, then launch this installer again");
                windows10.OpenWindowsUpdate();
                return false;
            }

            var c4 = ui.DisplayCommandLaunch("Checking if Windows Subsystem for Linux 2 is already installed");
            var wsl2Installed = wsl.IsWSL2AlreadyInstalled();
            ui.DisplayCommandResult(c4, wsl2Installed);

            if(!wsl2Installed)
            {
                var c5 = ui.DisplayCommandLaunch("Checking if virtualization is enabled on this machine");
                var virtualizationEnabled = windows10.IsVirtualizationEnabled();
                ui.DisplayCommandResult(c5, virtualizationEnabled, virtualizationEnabled?null:
                    "Please go to Windows Settings > Update & Security > Recovery (left menu) > Advanced Startup > Restart Now," +
                    " then select: Troubleshoot > Advanced options > UEFI firmware settings, and navigate menus to enable virtualization");
                if (!virtualizationEnabled)
                {
                    windows10.OpenWindowsUpdate();
                    return false;
                }

                var enableWsl =  await ui.DisplayAdminScriptQuestionAsync(
                    "Activating Windows Virtual Machine Platform and Windows Subsystem for Linux requires ADMIN privileges. Are you OK to execute the following script as admin ?",
                    windows10.EnableWindowsSubsystemForLinux_script());
                if(!enableWsl)
                {
                    return false;
                }

                var c6 = ui.DisplayCommandLaunch("Activating Windows Virtual Machine Platform and Windows Subsystem for Linux");
                var restartNeeded = windows10.EnableWindowsSubsystemForLinux();
                ui.DisplayCommandResult(c6, true);
                if (restartNeeded)
                {
                    var restartNow = await ui.DisplayQuestionAsync("You need to restart your computer to finish Windows Subsystem for Linux installation. Restart now ?");
                    if(restartNow)
                    {
                        windows10.ShutdownAndRestart();
                    }
                    return false;
                }
            }

            var kernelVersionOK = true;
            if (useGpu)
            {
                var c7 = ui.DisplayCommandLaunch("Checking Windows Subsystem for Linux kernel version (GPU support)");
                kernelVersionOK = wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
                ui.DisplayCommandResult(c7, kernelVersionOK);
            }

            var updateKernel = true;
            if(kernelVersionOK)
            {
                updateKernel = await ui.DisplayQuestionAsync("Do you want to update Windows Subsystem for Linux to the latest version ?");
            }
            if (updateKernel)
            {
                var c8 = ui.DisplayCommandLaunch("Updating Windows Subsystem for Linux kernel to the latest version");
                wsl.UpdateLinuxKernelVersion();
                ui.DisplayCommandResult(c8, true);

                if (useGpu)
                {
                    var c9 = ui.DisplayCommandLaunch("Checking Windows Subsystem for Linux kernel version (GPU support)");
                    kernelVersionOK = wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
                    ui.DisplayCommandResult(c9, kernelVersionOK, kernelVersionOK?null: "Windows Subsystem for Linux kernel version still not OK: please restart the install process without using the GPU");
                    if(!kernelVersionOK)
                    {
                        return false;
                    }
                }
            }

            // -- Versions last updated : January 9 2022 --

            // Alpine mini root filesystem: https://alpinelinux.org/downloads/
            var alpineVersion = "3.15.0";
            var alpineImageURL = $"https://dl-cdn.alpinelinux.org/alpine/v{alpineVersion.Substring(0,4)}/releases/x86_64/alpine-minirootfs-{alpineVersion}-x86_64.tar.gz";
            var alpineImageSize = 2731445;
            var alpineFileName = $"alpine-{alpineVersion}.tar";

            // Ubuntu minimum images: https://partner-images.canonical.com/oci/
            var ubuntuRelease = "focal";
            var ubuntuVersion = "20220105";
            var ubuntuImageURL = $"https://partner-images.canonical.com/oci/{ubuntuRelease}/{ubuntuVersion}/ubuntu-{ubuntuRelease}-oci-amd64-root.tar.gz";
            var ubuntuImageSize = 27746207;
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
            var helmExecutableSize = 13870692;
            var helmFileName = $"k3s-airgap-images-{k3sVersion}.tar";

            // nvidia container runtime versions: https://github.com/NVIDIA/nvidia-container-runtime/releases
            var nvidiaContainerRuntimeVersion = "3.7.0-1";

            var storage = LocalStorageManager.Instance;

            var c10 = ui.DisplayCommandLaunchWithProgress($"Downloading Alpine Linux operating system image (v{alpineVersion})", alpineImageSize, "Bytes");
            var download1 = storage.DownloadFileWithCache(alpineImageURL, alpineFileName, gunzip:true, progressCallback:(totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c10, totalBytesDownloaded));

            var c11 = ui.DisplayCommandLaunchWithProgress($"Downloading Ubuntu Linux operating system image ({ubuntuRelease} {ubuntuVersion})", ubuntuImageSize, "Bytes");
            var download2 = storage.DownloadFileWithCache(ubuntuImageURL, ubuntuFileName, gunzip:true, progressCallback:(totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c11, totalBytesDownloaded));

            var c12 = ui.DisplayCommandLaunchWithProgress($"Downloading Rancher K3s executable (v{k3sVersion})", k3sExecutableSize, "Bytes");
            var download3 = storage.DownloadFileWithCache(k3sExecutableURL, k3sExecutableFileName, progressCallback:(totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c12, totalBytesDownloaded));

            var c13 = ui.DisplayCommandLaunchWithProgress($"Downloading Rancher K3s containers images (v{k3sVersion})", k3sImagesSize, "Bytes");
            var download4 = storage.DownloadFileWithCache(k3sImagesURL, k3sImagesFileName, progressCallback:(totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c13, totalBytesDownloaded));

            var c14 = ui.DisplayCommandLaunchWithProgress($"Downloading Helm executable (v{helmVersion})", helmExecutableSize, "Bytes");
            var download5 = storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip:true, progressCallback:(totalFileSize, totalBytesDownloaded, progressPercentage) => ui.DisplayCommandProgress(c14, totalBytesDownloaded));

            Task.WaitAll(download1, download2, download3, download4, download5);

            // Extract helm executable from the downloaded tar file
            var helmTarFile = Path.Combine(storage.DownloadCacheDirectory.FullName, helmFileName);
            var helmTmpDir = Path.Combine(storage.DownloadCacheDirectory.FullName, "helm-temp");
            Directory.CreateDirectory(helmTmpDir);
            LocalStorageManager.ExtractTar(helmTarFile, helmTmpDir);
            File.Move(Path.Combine(helmTmpDir,"linux-amd64","helm"), Path.Combine(storage.DownloadCacheDirectory.FullName,"helm"));
            Directory.Delete(helmTmpDir, true);

            // --- Initialize and start VM ---

            var c15 = ui.DisplayCommandLaunch("Creating, installing and launching wordslab virtual machine and k3s cluster");
            Command.ExecuteShellScript(Path.Combine(storage.DownloadCacheDirectory.FullName, "wordslab-install.bat"),
                $"{storage.DownloadCacheDirectory.FullName} {alpineFileName} {ubuntuFileName} {k3sExecutableFileName} {k3sImagesFileName} {helmFileName} {nvidiaContainerRuntimeVersion} " +
                $"{storage.VirtualMachineOSDirectory.FullName} {storage.VirtualMachineClusterDirectory.FullName} {storage.VirtualMachineDataDirectory.FullName}",
                timeoutSec: 300);
            ui.DisplayCommandResult(c15, true);

            return true;
        }
    }
}
