using System.Net;

namespace wordslab.manager.vm
{
    public abstract class VirtualMachine
    {
        public string Name { get; protected set; }

        public int Cores { get; protected set; }

        public int MemoryMB { get; protected set; }

        public abstract bool Resize(int cores, int memoryMB, bool useGPU, string gpuType);

        public VirtualDisk OsDisk { get; protected set; }

        public VirtualDisk ClusterDisk { get; protected set; }

        public VirtualDisk DataDisk { get; protected set; }

        public abstract bool IsInstalled();

        public abstract IPAddress Start();

        public abstract bool Stop();

        public abstract bool Delete();
    }
}
