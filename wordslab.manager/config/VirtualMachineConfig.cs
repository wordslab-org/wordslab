using System.ComponentModel.DataAnnotations;
using wordslab.manager.os;

namespace wordslab.manager.config
{
    public enum VirtualMachineProvider
    {
        Unspecified,
        Wsl,
        Qemu,
        GoogleCloud
    }

    public class VirtualMachineConfig
    {
        private VirtualMachineConfig() { }

        public VirtualMachineConfig(string name, VirtualMachineSpec spec, 
            VirtualMachineProvider vmProvider, string vmModelName = null, bool isPreemptible = false,
            bool forwardSSHPortOnLocalhost = false, int hostSSHPort = 0, bool forwardKubernetesPortOnLocalhost = false, int hostKubernetesPort = 0, 
            bool forwardHttpIngressPortOnLocalhost = false, int hostHttpIngressPort = 0, bool allowHttpAccessFromLAN = false,
            bool forwardHttpsIngressPortOnLocalhost = false, int hostHttpsIngressPort = 0, bool allowHttpsAccessFromLAN = false)
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

        public string? VmModelName { get; set; }

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

        internal List<Network.PortConfig> GetNetworkPortsConfig()
        {
            var portsConfig = new List<Network.PortConfig>();
            if(ForwardSSHPortOnLocalhost)
            {
                portsConfig.Add(new Network.PortConfig() { VmPort=Spec.Network.SSHPort, HostPort=HostSSHPort, AllowAccessFromLAN=false });
            }
            if (ForwardKubernetesPortOnLocalhost)
            {
                portsConfig.Add(new Network.PortConfig() { VmPort = Spec.Network.KubernetesPort, HostPort = HostKubernetesPort, AllowAccessFromLAN = false });
            }
            if (ForwardHttpIngressPortOnLocalhost)
            {
                portsConfig.Add(new Network.PortConfig() { VmPort = Spec.Network.HttpIngressPort, HostPort = HostHttpIngressPort, AllowAccessFromLAN = AllowHttpAccessFromLAN });
            }
            if (ForwardHttpsIngressPortOnLocalhost)
            {
                portsConfig.Add(new Network.PortConfig() { VmPort = Spec.Network.HttpsIngressPort, HostPort = HostHttpsIngressPort, AllowAccessFromLAN = AllowHttpsAccessFromLAN });
            }
            return portsConfig;
        }

        // Comparison

        public override bool Equals(object? obj)
        {
            var vm = obj as VirtualMachineConfig;
            if(vm == null) return false;

            var equals = true;
            equals = equals && vm.Name == Name;
            equals = equals && vm.Spec.Equals(Spec);
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
