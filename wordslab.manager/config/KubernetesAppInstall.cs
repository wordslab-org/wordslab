using Microsoft.EntityFrameworkCore;

namespace wordslab.manager.config
{
    [PrimaryKey(nameof(VirtualMachineName),nameof(YamlFileHash))]
    public class KubernetesAppInstall : KubernetesAppSpec
    { 
        public KubernetesAppInstall() { }

        public KubernetesAppInstall(string vmName, string yamlFileURL) 
        {
            VirtualMachineName = vmName;
            YamlFileURL = yamlFileURL;
            InstallDate = DateTime.Now;
        }

        public string VirtualMachineName { get; private set; }

        public string YamlFileURL { get; private set; }

        public DateTime InstallDate { get; private set; }

        public bool IsFullyDownloadedInContentStore { get; set; } = false;

        public long RemainingDownloadSize { get; set; }

        public DateTime? UninstallDate { get; set; }
    }

    [PrimaryKey(nameof(VirtualMachineName),nameof(Namespace))]
    public class KubernetesAppDeployment : BaseConfig
    {
        public KubernetesAppDeployment() { }

        public KubernetesAppDeployment(string vmName, string deploymentNamespace, KubernetesAppInstall app)
        {
            VirtualMachineName = vmName;
            App = app;
            Namespace = deploymentNamespace;
            DeploymentDate = DateTime.Now;
        }

        public string VirtualMachineName { get; private set; }

        public string Namespace { get; private set; }

        public KubernetesAppInstall App { get; private set; }

        public DateTime DeploymentDate { get; private set; }

        public DateTime? RemovalDate { get; set; }
    }
}
