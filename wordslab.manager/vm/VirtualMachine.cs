using System.Net;
using System.Text.RegularExpressions;
using wordslab.manager.storage;

namespace wordslab.manager.vm
{
    public abstract class VirtualMachine
    {
        protected HostStorage storage;

        public VirtualMachine(string name, int processors, int memoryGB, int osDiskSizeGB, int clusterDiskSizeGB, int dataDiskSizeGB, HostStorage storage)
        {
            if(!NAME_REGEX.IsMatch(name))
            {
                throw new ArgumentException("A virtual machine name can only contain lowercase letters, digits and -");
            }
            Name = name;
            Processors = processors;
            MemoryGB = memoryGB;

            this.osDiskSizeGB = osDiskSizeGB;
            this.clusterDiskSizeGB = clusterDiskSizeGB;
            this.dataDiskSizeGB = dataDiskSizeGB;

            this.storage = storage;
        }

        private static readonly Regex NAME_REGEX = new Regex("^[a-z0-9-]+$");

        public string Name { get; protected set; }

        public int Processors { get; protected set; }

        public int MemoryGB { get; protected set; }

        protected int osDiskSizeGB;
        protected int clusterDiskSizeGB;
        protected int dataDiskSizeGB;

        public VirtualDisk OsDisk { get; protected set; }

        public VirtualDisk ClusterDisk { get; protected set; }

        public VirtualDisk DataDisk { get; protected set; }

        public abstract bool IsRunning();

        public abstract VMEndpoint Start();

        protected VMEndpoint endpoint;

        public VMEndpoint Endpoint
        {
            get
            {
                if(endpoint == null)
                {
                    if(IsRunning())
                    {
                        endpoint = VMEndpoint.Load(storage, Name);
                    }
                }
                return endpoint;
            }
        }

        public abstract void Stop();
    }

    public class VMEndpoint
    {
        public VMEndpoint(string name, string ip, int sshPort, string kubeConfigPath)
        {
            Name = name;
            Address = IPAddress.Parse(ip);
            SSHPort = sshPort;
            KubeConfigPath = kubeConfigPath;
        }

        public string Name {  get; private set; }

        public IPAddress Address { get; private set; }

        public int SSHPort { get; private set; }

        public string KubeConfigPath { get; private set; }

        public static string GetFilePath(HostStorage storage, string name)
        {
            return Path.Combine(storage.ConfigDirectory, "vm", $"{name}.endpoint");
        }

        public void Save(HostStorage storage)
        {            
            using(StreamWriter sw = new StreamWriter(GetFilePath(storage, Name)))
            {
                sw.WriteLine(Address.ToString());
                sw.WriteLine(SSHPort);
                sw.WriteLine(KubeConfigPath);
            }
        }

        public static VMEndpoint Load(HostStorage storage, string name)
        {
            var filepath = GetFilePath(storage, name);
            if (File.Exists(filepath))
            {
                using (StreamReader sr = new StreamReader(filepath))
                {
                    var ip = sr.ReadLine();
                    var sshPort = Int32.Parse(sr.ReadLine());
                    var kubeconfigPath = sr.ReadLine();
                    return new VMEndpoint(name, ip, sshPort, kubeconfigPath);
                }
            }
            else
            {
                return null;
            }
        }

        public static void Delete(HostStorage storage, string name)
        {
            var filepath = GetFilePath(storage, name);
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }
    }
}
