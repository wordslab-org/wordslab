using wordslab.manager.config;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.os;

namespace wordslab.manager.apps
{
    public class KubernetesAppsManager
    {
        private ConfigStore configStore;

        public KubernetesAppsManager(ConfigStore configStore)
        {
            this.configStore = configStore;
        }

        public static void DisplayKubernetesAppSpec(KubernetesAppSpec appSpec, InstallProcessUI ui)
        {
            ui.DisplayInformationLine($"App name: {appSpec.Name}");
            ui.DisplayInformationLine($"Title: {appSpec.Title}");
            ui.DisplayInformationLine($"Description: {appSpec.Description}");
            ui.DisplayInformationLine();
            ui.DisplayInformationLine($"Version: {appSpec.Version}");
            ui.DisplayInformationLine($"Date: {appSpec.Date}");
            ui.DisplayInformationLine($"ID: {appSpec.YamlFileHash}");
            ui.DisplayInformationLine();
            ui.DisplayInformationLine($"Home page: {appSpec.HomePage}");
            ui.DisplayInformationLine($"Source: {appSpec.Source}");
            ui.DisplayInformationLine($"Author: {appSpec.Author}");
            ui.DisplayInformationLine($"Licence: {appSpec.Licence}");
            ui.DisplayInformationLine();
            if (appSpec.IngressRoutes.Count > 0)
            {
                ui.DisplayInformationLine("User interface entry points:");
                foreach (var ingressRouteInfo in appSpec.IngressRoutes)
                {
                    bool isHttps = ingressRouteInfo.IsHttps;
                    foreach (var pathInfo in ingressRouteInfo.Paths)
                    {
                        ui.DisplayInformationLine($"- {pathInfo.Title}: http{(isHttps ? 's' : null)}://[virtualmachine]/{appSpec.NamespaceDefault}{pathInfo.Path}");
                    }
                }
                ui.DisplayInformationLine();
            }
            if (appSpec.Services.Values.Where(s => !String.IsNullOrEmpty(s.Title)).Any())
            {
                ui.DisplayInformationLine("Services exposed to other apps:");
                foreach (var serviceInfo in appSpec.Services.Values.Where(s => !String.IsNullOrEmpty(s.Title)))
                {
                    ui.DisplayInformationLine($"- Service: {serviceInfo.Name}");
                    ui.DisplayInformationLine($"  . title: {serviceInfo.Title}");
                    ui.DisplayInformationLine($"  . description: {serviceInfo.Description}");
                    ui.DisplayInformationLine($"  . port: {serviceInfo.Port}");
                }
                ui.DisplayInformationLine();
            }
            if (appSpec.ContainerImages.Count > 0)
            {
                ui.DisplayInformationLine("Container images to download:");
                ui.DisplayInformationLine($"- Total download size: {appSpec.ContainerImagesLayers().Sum(l => l.Size) / 1024 / 1024} MB");
                foreach (var containerImageInfo in appSpec.ContainerImages)
                {
                    ui.DisplayInformationLine($"- Container: {containerImageInfo.Name}");
                    ui.DisplayInformationLine($"  . layers size: {containerImageInfo.Layers.Sum(l => l.Size) / 1024 / 1024} MB");
                    ui.DisplayInformationLine($"  . digest: {containerImageInfo.Digest}");
                }
                ui.DisplayInformationLine();
            }
            if (appSpec.PersistentVolumes.Count > 0)
            {
                ui.DisplayInformationLine("Persistent volumes to store application data:");
                foreach (var persistentVolumeInfo in appSpec.PersistentVolumes.Values)
                {
                    ui.DisplayInformationLine($"- Volume: {persistentVolumeInfo.Name}");
                    ui.DisplayInformationLine($"  . title: {persistentVolumeInfo.Title}");
                    ui.DisplayInformationLine($"  . description: {persistentVolumeInfo.Description}");
                    if (persistentVolumeInfo.StorageRequest.HasValue)
                    {
                        ui.DisplayInformationLine($"  . storage request: {persistentVolumeInfo.StorageRequest / 1024 / 1024} MB");
                    }
                    if (persistentVolumeInfo.StorageLimit.HasValue)
                    {
                        ui.DisplayInformationLine($"  . storage limit: {persistentVolumeInfo.StorageLimit / 1024 / 1024} MB");
                    }
                }
                ui.DisplayInformationLine();
            }
        }

        public static void DisplayKubernetesAppInstall(KubernetesAppInstall appInstall, InstallProcessUI ui)
        {
            DisplayKubernetesAppSpec(appInstall, ui);

            ui.DisplayInformationLine($"Install date: {appInstall.InstallDate}");
            if (appInstall.IsFullyDownloadedInContentStore)
            {
                ui.DisplayInformationLine($"Download status: OK - ready to use");
            }
            else
            {
                ui.DisplayInformationLine($"Download status: in progress - {appInstall.RemainingDownloadSize / 1024 / 1024} MB remaining");
            }
            ui.DisplayInformationLine();
        }

        public async Task<KubernetesAppInstall> DownloadKubernetesApp(VirtualMachine vm, string yamlFileUrl, InstallProcessUI ui)
        {
            try
            {
                // 1. Download and analyze Kubernetes application yaml file
                var cmd1 = ui.DisplayCommandLaunch($"Downloading kubernetes app metadata from {yamlFileUrl} ...");
                KubernetesAppInstall appInstall = await KubernetesApp.ImportMetadataFromYamlFileAsync(vm.Name, yamlFileUrl, configStore);
                ui.DisplayCommandResult(cmd1, true);

                // 2. Display app properties
                DisplayKubernetesAppSpec(appInstall, ui);

                // 3. Check total download size and confirm images download
                var cmd2 = ui.DisplayCommandLaunch($"Checking remaining download size for app {appInstall.Name} in virtual machine {vm.Name} ...");
                var downloadSizes = new List<long>();
                foreach (var imageInfo in appInstall.ContainerImages)
                {
                    downloadSizes.Add(Kubernetes.CheckImageBytesToDownload(imageInfo, vm));
                }
                appInstall.RemainingDownloadSize = downloadSizes.Sum();
                if (appInstall.UninstallDate.HasValue) { appInstall.UninstallDate = null; }
                configStore.SaveChanges();

                var needToDownload = $"{appInstall.RemainingDownloadSize / 1024 / 1024} MB";
                ui.DisplayCommandResult(cmd2, true, needToDownload);

                if(appInstall.RemainingDownloadSize > 0)
                {
                    var confirmInstall = await ui.DisplayQuestionAsync($"Do you confirm you want to download {needToDownload} in virtual machine {vm.Name}?");
                    if (!confirmInstall)
                    {
                        return null;
                    }

                    // 3. Download container images in content store
                    var downloadCommands = new List<LongRunningCommand>();
                    for (var i = 0; i < appInstall.ContainerImages.Count; i++)
                    {
                        var downloadSize = downloadSizes[i];
                        if (downloadSize > 0)
                        {
                            var imageInfo = appInstall.ContainerImages[i];
                            var c = new LongRunningCommand($"{imageInfo.Name}", downloadSize, "Bytes",
                                displayProgress => Kubernetes.DownloadImageInContentStoreWithProgress(imageInfo, vm, timeoutSec:3600,
                                        progressHandler: (totalFileSize, totalBytesDownloaded, progressPercentage) => displayProgress(totalBytesDownloaded)),
                                displayResult => displayResult(true)
                                );
                            downloadCommands.Add(c);
                        }
                    }
                    if (downloadCommands.Count > 0)
                    {
                        ui.RunCommandsAndDisplayProgress(downloadCommands.ToArray());
                    }
                }

                // 4. Saving kubernetes app install
                var cmd3 = ui.DisplayCommandLaunch($"Saving {appInstall.Name} app status in virtual machine {vm.Name} ...");
                appInstall.RemainingDownloadSize = 0;
                appInstall.IsFullyDownloadedInContentStore = true;
                configStore.SaveChanges();
                ui.DisplayCommandResult(cmd3, true);

                return appInstall;
            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
                return null;
            }
        }

        public List<KubernetesAppInstall> ListKubernetesApps(VirtualMachine vm)
        {
            return configStore.ListKubernetesAppsInstalledOn(vm.Name);
        }

        public async Task<bool> RemoveKubernetesApp(KubernetesAppInstall app, VirtualMachine vm, InstallProcessUI ui)
        {
            try
            {
                // 1. Check that there are no active deployments
                var cmd1 = ui.DisplayCommandLaunch($"Checking that there are no active deployment of application {app.Name} with ID {app.YamlFileHash} in virtual machine {vm.Name} ...");
                var appDeployments = configStore.ListKubernetesAppDeployments(vm.Name, app);
                if(appDeployments.Count > 0)
                {
                    ui.DisplayCommandResult(cmd1, false, $"Active deployments of this application found in namespaces: {string.Join(',',appDeployments.Select(d=>d.Namespace))}");
                    return false;
                }
                else
                {
                    ui.DisplayCommandResult(cmd1, true);
                }

                // 2. Display app properties, disk usage, and confirm uninstall
                DisplayKubernetesAppInstall(app, ui);

                var confirm = await ui.DisplayQuestionAsync($"Do you confirm that you want to remove the appplication {app.Name} from the virtual machine {vm.Name}?");
                if(!confirm)
                {
                    return false;
                }

                // 3. Delete container images, mark KubernetesApp as uninstalled in the database
                foreach (var imageInfo in app.ContainerImages)
                {
                    // Delete image only if it is not used by other applications
                    if (!imageInfo.UsedByKubernetesApps.Any(userApp => userApp.YamlFileHash != app.YamlFileHash))
                    {
                        var cmd = ui.DisplayCommandLaunch($"Deleting image {imageInfo.Name} ({imageInfo.Layers.Sum(l => l.Size)/1024/1024} MB) ...");
                        Kubernetes.DeleteImageFromContentStore(imageInfo, vm);
                        ui.DisplayCommandResult(cmd, true);
                    }
                }

                var cmd3 = ui.DisplayCommandLaunch($"Removing app {app.Name} from virtual machine {vm.Name} ...");
                app.IsFullyDownloadedInContentStore = false;
                app.RemainingDownloadSize = app.ContainerImagesLayers().Sum(l => l.Size);
                app.UninstallDate = DateTime.Now;
                configStore.SaveChanges();
                ui.DisplayCommandResult(cmd3, true);

                return true;
            }
            catch (Exception ex)
            {
                ui.DisplayCommandError(ex.Message);
                return false;
            }
        }

        public async Task<KubernetesAppDeployment> DeployKubernetesApp(VirtualMachine vm, InstallProcessUI ui)
        {
            // 1. Display app properties

            // 2. Choose a namespace

            // 3. Deploy and display test URLS

            await ui.DisplayQuestionAsync("What do you want to install ?");
            throw new NotImplementedException();
        }

        public List<KubernetesAppDeployment> ListKubernetesAppDeployments(VirtualMachine vm)
        {
            return configStore.ListKubernetesAppsDeployedOn(vm.Name);
        }

        public async Task<KubernetesAppDeployment> RemoveKubernetesAppDeployment(VirtualMachine vm, InstallProcessUI ui)
        {
            // 1. Display deployment properties

            // 2. Select volumes to preserve

            // 3. Remove and display preserved volumes

            await ui.DisplayQuestionAsync("What do you want to install ?");
            throw new NotImplementedException();
        }
    }
}
