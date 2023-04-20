using System.ComponentModel.DataAnnotations;

namespace wordslab.manager.config
{
    public class KubernetesAppInstall : BaseConfig
    {
        [Key]
        public string VirtualMachineName { get; set; }

        [Key]
        public string Namespace { get; set; }

        public DateTime InstallDate { get; set; }

        public DateTime? UninstallDate { get; set; }

        public KubernetesAppSpec Spec { get; set; }
    }
}
