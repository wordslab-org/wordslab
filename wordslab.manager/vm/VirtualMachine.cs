using System.Net;

namespace wordslab.manager.vm
{
    public abstract class VirtualMachine
    {
        public string Name { get; protected set; }

        public int Cores { get; protected set; }

        public int MemoryMB { get; protected set; }

        public VirtualDisk VmDisk { get; protected set; }

        public VirtualDisk ClusterDisk { get; protected set; }

        public VirtualDisk DataDisk { get; protected set; }

        public abstract bool IsRunning();

        public abstract VMEndpoint Start();

        public abstract void Stop();
    }

    public class VMEndpoint
    {
        public VMEndpoint(string ip, int sshPort, string kubeConfigPath)
        {
            Address = IPAddress.Parse(ip);
            SSHPort = sshPort;
            KubeConfigPath = kubeConfigPath;
        }

        public IPAddress Address { get; private set; }

        public int SSHPort { get; private set; }

        public string KubeConfigPath { get; private set; }
    }
}
