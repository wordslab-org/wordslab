using System.Net;

namespace wordslab.installer.infrastructure
{
    public class MachineConnection
    {
        public int Id { get; private set; }

        public string MachineName { get; private set; }

        public bool IsLocal { get; private set; }

        public bool IsVirtualMachineHost { get; private set; }

        public bool IsKubernetesClusterNode { get; private set; }

        public string Address { get; private set; }

        public string CredentialsFile { get; private set; }
    }
}
