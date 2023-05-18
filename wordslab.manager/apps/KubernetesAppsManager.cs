using wordslab.manager.config;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.os;
using Spectre.Console;

namespace wordslab.manager.apps
{
    public class KubernetesAppsManager
    {
        private ConfigStore configStore;

        public KubernetesAppsManager(ConfigStore configStore)
        {
            this.configStore = configStore;
        }

        public static void DisplayKubernetesAppSpec(KubernetesAppSpec appSpec, InstallProcessUI ui, string vmAddressAndPort = null, string deploymentNamespace = null)
        {
            if (vmAddressAndPort == null) vmAddressAndPort = "[virtualmachine]";
            if (deploymentNamespace == null) deploymentNamespace = appSpec.NamespaceDefault;

            DisplayKubernetesAppIdentity(appSpec, ui);
            ui.DisplayInformationLine();
            ui.DisplayInformationLine($"Date: {appSpec.Date}");
            ui.DisplayInformationLine($"ID: {appSpec.YamlFileHash}");
            ui.DisplayInformationLine();
            ui.DisplayInformationLine($"Home page: {appSpec.HomePage}");
            ui.DisplayInformationLine($"Source: {appSpec.Source}");
            ui.DisplayInformationLine($"Author: {appSpec.Author}");
            ui.DisplayInformationLine($"Licence: {appSpec.Licence}");
            ui.DisplayInformationLine();
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
            if (appSpec.Services.Values.Where(s => !String.IsNullOrEmpty(s.Title)).Any())
            {
                ui.DisplayInformationLine("Services exposed to other apps:");
                foreach (var serviceInfo in appSpec.Services.Values.Where(s => !String.IsNullOrEmpty(s.Title)))
                {
                    ui.DisplayInformationLine($"- Service: {serviceInfo.Name}");
                    ui.DisplayInformationLine($"  . title: {serviceInfo.Title}");
                    ui.DisplayInformationLine($"  . description: {serviceInfo.Description}");
                    ui.DisplayInformationLine($"  . port: {serviceInfo.Port}");
                    ui.DisplayInformationLine($"  . URL: {serviceInfo.Url(deploymentNamespace)}");
                }
                ui.DisplayInformationLine();
            }
            DisplayKubernetesAppEntryPoints(appSpec, ui, vmAddressAndPort, deploymentNamespace);
        }

        public static void DisplayKubernetesAppIdentity(KubernetesAppSpec appSpec, InstallProcessUI ui)
        {
            ui.DisplayInformationLine($"App name: {appSpec.Name}");
            ui.DisplayInformationLine($"Title: {appSpec.Title}");
            ui.DisplayInformationLine($"Description: {appSpec.Description}");
            ui.DisplayInformationLine($"Version: {appSpec.Version}");
        }

        public static void DisplayKubernetesAppEntryPoints(KubernetesAppSpec appSpec, InstallProcessUI ui, string vmAddressAndPort, string deploymentNamespace)
        {
            if (appSpec.IngressRoutes.Count > 0)
            {
                ui.DisplayInformationLine("User interface entry points:");
                foreach (var ingressRouteInfo in appSpec.IngressRoutes)
                {
                    var urlsAndTitles = ingressRouteInfo.UrlsAndTitles(vmAddressAndPort, deploymentNamespace);
                    foreach (var urlAndTitle in urlsAndTitles)
                    {
                        ui.DisplayInformationLine($"- {urlAndTitle.Item2}: {urlAndTitle.Item1}");
                    }
                }
                ui.DisplayInformationLine();
            }
        }

        public static void DisplayKubernetesAppInstall(KubernetesAppInstall appInstall, InstallProcessUI ui, string vmAddressAndPort = null, string deploymentNamespace = null)
        {
            DisplayKubernetesAppSpec(appInstall, ui, vmAddressAndPort, deploymentNamespace);

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

        public async static Task DisplayKubernetesAppDeployments(VirtualMachine vm, InstallProcessUI ui, ConfigStore configStore)
        {
            var appDeployments = configStore.ListKubernetesAppsDeployedOn(vm.Name);
            if (appDeployments.Count > 0)
            {
                ui.DisplayInformationLine($"Applications deployed on virtual machine {vm.Name}:");
                ui.DisplayInformationLine();
                foreach (var appDeployment in appDeployments)
                {
                    await DisplayKubernetesAppDeployment(vm, ui, configStore, appDeployment);
                }
            }
        }

        public static async Task DisplayKubernetesAppDeployment(VirtualMachine vm, InstallProcessUI ui, ConfigStore configStore, KubernetesAppDeployment appDeployment)
        {
            ui.DisplayInformationLine($"[Namespace: {appDeployment.Namespace}]");
            ui.DisplayInformationLine();
            ui.DisplayInformationLine($"Status: {appDeployment.State} since {(appDeployment.LastStateTimestamp.Value.ToString("MM/dd/yyyy HH:mm"))}");
            ui.DisplayInformationLine();
            DisplayKubernetesAppIdentity(appDeployment.App, ui);
            await KubernetesApp.ParseYamlFileContent(appDeployment.App, loadContainersMetadata: false, configStore);
            ui.DisplayInformationLine();
            if (appDeployment.State == AppDeploymentState.Running)
            {
                DisplayKubernetesAppEntryPoints(appDeployment.App, ui, vm.RunningInstance.GetHttpAddressAndPort(), appDeployment.Namespace);
            }
        }

        public static async Task<bool> WaitForKubernetesAppEntryPoints(KubernetesAppDeployment app, VirtualMachine vm, int timeoutSec = 60)
        {
            var vmAddressAndPort = vm.RunningInstance.GetHttpAddressAndPort();
            var deploymentNamespace = app.Namespace;
            var entryPoints = app.App.IngressRoutes.SelectMany(r => r.UrlsAndTitles(vmAddressAndPort, deploymentNamespace)).Select(p => p.Item1);

            using (var client = new HttpClient())
            {
                var startTimestamp = DateTime.Now;
                bool allUrlsOK = false;
                while (!allUrlsOK)
                {
                    if ((DateTime.Now - startTimestamp).TotalSeconds > timeoutSec)
                    {
                        return false;
                    }
                    await Task.Delay(1000);
                    allUrlsOK = true;
                    foreach(var url in entryPoints)
                    {
                        var response = await client.GetAsync(url);
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            allUrlsOK = false;
                            break;
                        }
                    }
                }
            }
            return true;
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
                DisplayKubernetesAppInstall(app, ui, vmAddressAndPort: vm.RunningInstance.GetHttpAddressAndPort());

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

        public async Task<KubernetesAppDeployment> DeployKubernetesApp(KubernetesAppInstall app, VirtualMachine vm, InstallProcessUI ui)
        {
            string namespaceCreated = null;
            try
            {
                // 1. Display app properties
                var cmd1 = ui.DisplayCommandLaunch($"Checking if app {app.Name} is ready to use in virtual machine {vm.Name} ...");
                if(app.IsFullyDownloadedInContentStore)
                {
                    ui.DisplayCommandResult(cmd1 , true);
                }
                else
                {
                    ui.DisplayCommandResult(cmd1, false, $"Please finish downloading this application: you still need to download {app.RemainingDownloadSize/1024/1024} MB");
                    return null;
                }

                DisplayKubernetesAppInstall(app, ui);

                // 2. Choose a namespace
                var deploymentNamespace = await ui.DisplayInputQuestionAsync($"Choose a namespace for this deployment of application {app.Name} in virtual machine {vm.Name}", app.NamespaceDefault);

                var cmd2 = ui.DisplayCommandLaunch($"Checking if namespace {deploymentNamespace} is still free to use in virtual machine {vm.Name} ...");
                var namespaces = Kubernetes.GetAllNamespaces(vm);
                if(!namespaces.Contains(deploymentNamespace))
                {
                    ui.DisplayCommandResult(cmd2 , true);
                }
                else
                {
                    ui.DisplayCommandResult(cmd2, false, $"The following namespaces are already used {string.Join(',',namespaces.Where(n => !n.StartsWith("kube-") && n!="default"))}");
                    return null;
                }

                // 3. Deploy and display test URLS
                var cmd3 = ui.DisplayCommandLaunch($"Deploying application {app.Name} in namespace {deploymentNamespace} on virtual machine {vm.Name} ...");
                namespaceCreated = deploymentNamespace;
                var yamlFileContentForDeployment = KubernetesApp.GetYamlFileContentForDeployment(app, deploymentNamespace);
                var exitCode = Kubernetes.ApplyYamlFileAndWaitForResources(yamlFileContentForDeployment, deploymentNamespace, vm);
                if(exitCode != 0)
                {
                    throw new Exception($"kubernetes app deployment failed with exit code {exitCode}");
                }
                
                var appDeployment = new KubernetesAppDeployment(vm.Name, deploymentNamespace, app);
                configStore.AddAppDeployment(appDeployment);
                configStore.SaveChanges();
                ui.DisplayCommandResult(cmd3, true);

                // 4. Wait until all application entrypoints are ready, for one minute max
                var cmd4 = ui.DisplayCommandLaunch($"The application {app.Name} is starting: this may take up to one minute ...");
                var successful = await WaitForKubernetesAppEntryPoints(appDeployment, vm);
                appDeployment.Started();
                configStore.SaveChanges();
                if (successful)
                {
                    ui.DisplayCommandResult(cmd4 , true);
                }
                else
                {
                    ui.DisplayCommandResult(cmd4, false, "The application wasn't completely ready after one minute: you may have to wait a little bit longer before you can use all user entry points");
                }

                DisplayKubernetesAppEntryPoints(app, ui, vm.RunningInstance.GetHttpAddressAndPort(), deploymentNamespace);

                return appDeployment;
            }
            catch (Exception ex)
            {
                // Cleanup if an exception occurs in the middle of a deployment
                if (namespaceCreated != null)
                {
                    try { Kubernetes.TryDeleteNamespace(namespaceCreated, vm); } catch { }
                }

                ui.DisplayCommandError(ex.Message);
                return null;
            }
}

        public List<KubernetesAppDeployment> ListKubernetesAppDeployments(VirtualMachine vm)
        {
            return configStore.ListKubernetesAppsDeployedOn(vm.Name);
        }

        public void StopKubernetesAppDeployment(KubernetesAppDeployment app, VirtualMachine vm)
        {
            var exitCode = Kubernetes.DeleteResourcesFromNamespace(app.Namespace, true, vm);
            if (exitCode != 0)
            {
                throw new Exception($"kubernetes app deployment stop failed with exit code {exitCode}");
            }
            else
            {
                app.Stopped();
                configStore.SaveChanges();
            }
        }

        public void RestartKubernetesAppDeployment(KubernetesAppDeployment app, VirtualMachine vm)
        {
            var deploymentNamespace = app.Namespace;
            var yamlFileContentForDeployment = KubernetesApp.GetYamlFileContentForDeployment(app.App, deploymentNamespace);
            var exitCode = Kubernetes.ApplyYamlFileAndWaitForResources(yamlFileContentForDeployment, deploymentNamespace, vm, createNamespace: false);
            if (exitCode != 0)
            {
                throw new Exception($"kubernetes app deployment restart failed with exit code {exitCode}");
            }
            else
            {
                app.Started();
                configStore.SaveChanges();
            }
        }

        public async Task<bool> DeleteKubernetesAppDeployment(KubernetesAppDeployment app, VirtualMachine vm, InstallProcessUI ui)
        {
            try 
            { 
                // 1. Display deployment properties
                DisplayKubernetesAppDeployment(vm, ui, configStore, app);

                // 2. Confirm deletion
                var confirm = await ui.DisplayQuestionAsync("Are you sure you want to delete this deployment - ALL USER DATA WILL BE LOST FOREVER - ?", false);

                // 3. Delete namespace and all user data
                if (confirm)
                {
                    var cmd = ui.DisplayCommandLaunch($"Deleting application deployment in namespace {app.Namespace} ...");
                    var exitCode = Kubernetes.DeleteResourcesFromNamespace(app.Namespace, false, vm);
                    if (exitCode != 0)
                    {
                        ui.DisplayCommandResult(cmd, false, $"kubernetes app deployment delete failed with exit code {exitCode}");
                        return false;
                    }
                    else
                    {
                        configStore.RemoveKubernetesAppDeployment(vm.Name, app.Namespace);
                        ui.DisplayCommandResult(cmd, true);
                        return true;
                    }
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
