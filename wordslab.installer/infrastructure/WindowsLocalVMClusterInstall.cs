using wordslab.installer.infrastructure.commands;

namespace wordslab.installer.infrastructure
{
    // https://blazor-university.com/
    // https://jonhilton.net/blazor-dynamic-components/
    public interface InstallationUI
    {
        int DisplayCommandLaunch(string commandDescription);

        int DisplayCommandLaunchWithProgress(string commandDescription, int maxValue, string unit);

        void DisplayCommandProgress(int commandId, int currentValue);

        void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage= null);

        Task<bool> DisplayQuestionAsync(string question);

        Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent);
    }

    public class WindowsLocalVMClusterInstall
    {
        public async Task<bool> Install(InstallationUI ui)
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





            return true;
        }
    }
}
