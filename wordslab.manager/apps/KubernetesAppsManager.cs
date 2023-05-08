using wordslab.manager.config;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.apps
{
    public class KubernetesAppsManager
    {
        private ConfigStore configStore;

        public KubernetesAppsManager(ConfigStore configStore)
        {
            this.configStore = configStore;
        }

        public async Task<KubernetesAppInstall> InstallKubernetesApp(VirtualMachine vm, InstallProcessUI installUI)
        {
            // 1. Download and analyze Kubernetes application yaml file

            // 2. Display app properties, disk space usage, and confirm installation

            // 3. Download container images in content store

            // TO DO support RESUME Kubernetes app install

            await installUI.DisplayQuestionAsync("What do you want to install ?");
            throw new NotImplementedException();
        }

        public List<KubernetesAppInstall> ListKubernetesApps(VirtualMachine vm)
        {
            return configStore.ListKubernetesAppsInstalledOn(vm.Name);
        }

        public async Task<bool> UninstallKubernetesApp(KubernetesAppInstall app, VirtualMachine vm, InstallProcessUI installUI)
        {
            // 1. Check that there are no active deployments

            // 2. Display app properties, disk usage, and confirm uninstall
            
            // 3. Delete container images, mark KubernetesApp as uninstalled in the database

            await installUI.DisplayQuestionAsync("Are you sure you want to uninstall ?");
            throw new NotImplementedException();
        }

        public async Task<KubernetesAppDeployment> DeployKubernetesApp(VirtualMachine vm, InstallProcessUI installUI)
        {
            // 1. Display app properties

            // 2. Choose a namespace

            // 3. Deploy and display test URLS

            await installUI.DisplayQuestionAsync("What do you want to install ?");
            throw new NotImplementedException();
        }

        public List<KubernetesAppDeployment> ListKubernetesAppDeployments(VirtualMachine vm)
        {
            return configStore.ListKubernetesAppsDeployedOn(vm.Name);
        }

        public async Task<KubernetesAppDeployment> RemoveKubernetesAppDeployment(VirtualMachine vm, InstallProcessUI installUI)
        {
            // 1. Display deployment properties

            // 2. Select volumes to preserve

            // 3. Remove and display preserved volumes

            await installUI.DisplayQuestionAsync("What do you want to install ?");
            throw new NotImplementedException();
        }
    }
}
