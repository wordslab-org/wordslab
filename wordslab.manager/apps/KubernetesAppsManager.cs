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
            await installUI.DisplayQuestionAsync("What do you want to install ?");
            throw new NotImplementedException();
        }

        public List<KubernetesAppInstall> ListKubernetesApps(VirtualMachine vm)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UninstallKubernetesApp(KubernetesAppInstall app, VirtualMachine vm, InstallProcessUI installUI)
        {
            await installUI.DisplayQuestionAsync("Are you sure you want to uninstall ?");
            throw new NotImplementedException();
        }
    }
}
