using System.ComponentModel.DataAnnotations;

namespace wordslab.manager.config
{
    public enum VirtualMachineProvider
    {
        Wsl,
        Qemu,
        GoogleCloud
    }

    public class VirtualMachineConfig
    {
        private VirtualMachineConfig() { }

        public VirtualMachineConfig(string name, 
            VirtualMachineSpec spec, VirtualMachineProvider vmProvider, string vmModelName, bool isPreemptible,
            bool forwardSSHPortOnLocalhost, int hostSSHPort, bool forwardKubernetesPortOnLocalhost, int hostKubernetesPort, 
            bool forwardHttpIngressPortOnLocalhost, int hostHttpIngressPort, bool allowHttpAccessFromLAN, bool forwardHttpsIngressPortOnLocalhost, int hostHttpsIngressPort, bool allowHttpsAccessFromLAN)
        {
            Name = name;
            Spec = spec;
            VmProvider = vmProvider;
            VmModelName = vmModelName;
            IsPreemptible = isPreemptible;
            ForwardSSHPortOnLocalhost = forwardSSHPortOnLocalhost;
            HostSSHPort = hostSSHPort;
            ForwardKubernetesPortOnLocalhost = forwardKubernetesPortOnLocalhost;
            HostKubernetesPort = hostKubernetesPort;
            ForwardHttpIngressPortOnLocalhost = forwardHttpIngressPortOnLocalhost;
            HostHttpIngressPort = hostHttpIngressPort;
            AllowHttpAccessFromLAN = allowHttpAccessFromLAN;
            ForwardHttpsIngressPortOnLocalhost = forwardHttpsIngressPortOnLocalhost;
            HostHttpsIngressPort = hostHttpsIngressPort;
            AllowHttpsAccessFromLAN = allowHttpsAccessFromLAN;
        }

        [Key]
        public string Name { get; set; }

        // Specification

        public VirtualMachineSpec Spec { get; set; } = new VirtualMachineSpec();

        // Implementation

        public VirtualMachineProvider VmProvider { get; set; }    

        public string VmModelName { get; set; }

        public bool IsPreemptible { get; set; }

        // Host access

        public bool ForwardSSHPortOnLocalhost { get; set; }
        public int HostSSHPort { get; set; }

        public bool ForwardKubernetesPortOnLocalhost { get; set; }
        public int HostKubernetesPort { get; set; }

        public bool ForwardHttpIngressPortOnLocalhost { get; set; }
        public int HostHttpIngressPort { get; internal set; }
        public bool AllowHttpAccessFromLAN { get; set; }

        public bool ForwardHttpsIngressPortOnLocalhost { get; set; }
        public int HostHttpsIngressPort { get; internal set; }
        public bool AllowHttpsAccessFromLAN { get; set; }

        // Comparison

        public override bool Equals(object? obj)
        {
            var vm = obj as VirtualMachineConfig;
            if(vm == null) return false;

            var equals = true;
            equals = equals && vm.Name == Name;
            equals = equals && vm.Spec == Spec;
            equals = equals && vm.VmProvider == VmProvider;
            equals = equals && vm.VmModelName == VmModelName;
            equals = equals && vm.IsPreemptible == IsPreemptible;
            equals = equals && vm.ForwardSSHPortOnLocalhost == ForwardSSHPortOnLocalhost;
            equals = equals && vm.HostSSHPort == HostSSHPort;
            equals = equals && vm.ForwardKubernetesPortOnLocalhost == ForwardKubernetesPortOnLocalhost;
            equals = equals && vm.HostKubernetesPort == HostKubernetesPort;
            equals = equals && vm.ForwardHttpIngressPortOnLocalhost == ForwardHttpIngressPortOnLocalhost;
            equals = equals && vm.HostHttpIngressPort == HostHttpIngressPort;
            equals = equals && vm.AllowHttpAccessFromLAN == AllowHttpAccessFromLAN;
            equals = equals && vm.ForwardHttpsIngressPortOnLocalhost == ForwardHttpsIngressPortOnLocalhost;
            equals = equals && vm.HostHttpsIngressPort == HostHttpsIngressPort;
            equals = equals && vm.AllowHttpsAccessFromLAN == AllowHttpsAccessFromLAN;

            return equals;
        }
    }
}
