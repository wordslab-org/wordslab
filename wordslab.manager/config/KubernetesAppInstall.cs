using System.ComponentModel.DataAnnotations;

namespace wordslab.manager.config
{
    public class KubernetesAppInstall : BaseConfig
    {
        public KubernetesAppInstall() { }

        public KubernetesAppInstall(string vmName, string yamlFileURL, KubernetesAppSpec appSpec) 
        {
            VirtualMachineName = vmName;
            YamlFileHash = appSpec.YamlFileHash;
            YamlFileURL = yamlFileURL;
            Spec = appSpec;
            InstallDate = DateTime.Now;
        }

        [Key]
        public string VirtualMachineName { get; private set; }

        [Key]
        public string YamlFileHash { get; private set; }

        public string YamlFileURL { get; private set; }

        public KubernetesAppSpec Spec { get; private set; }

        public DateTime InstallDate { get; private set; }

        public DateTime? UninstallDate { get; set; }
    }

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

        [Key]
        public string VirtualMachineName { get; private set; }

        [Key]
        public string Namespace { get; private set; }

        public KubernetesAppInstall App { get; private set; }

        public DateTime DeploymentDate { get; private set; }

        public DateTime? RemovalDate { get; set; }
    }
}
