using System.Net;

namespace wordslab.manager.vm.googlecloud
{
    public class GoogleCloudVM : VirtualMachine
    {
        public override bool Delete()
        {
            throw new NotImplementedException();
        }

        public override bool IsInstalled()
        {
            throw new NotImplementedException();
        }

        public override bool Resize(int cores, int memoryMB, bool useGPU, string gpuType)
        {
            throw new NotImplementedException();
        }

        public override IPAddress Start()
        {
            throw new NotImplementedException();
        }

        public override bool Stop()
        {
            throw new NotImplementedException();
        }
    }
}
