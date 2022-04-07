using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuVMInstaller
    {
        public static VirtualMachine FindLocalVM(HostStorage hostStorage)
        {
            throw new NotImplementedException();
        }

        public static async Task<VirtualMachine> Install(VirtualMachineSpec vmSpec, HostStorage hostStorage, InstallProcessUI ui)
        {
            /*
if not:qemu-system-x86_64 --version

apt-get update -y
apt-get install -y qemu qemu-utils qemu-kvm 

if not:~/.ssh/id_rsa
ssh-keygen

wget https://cloud-images.ubuntu.com/minimal/releases/focal/release-20220201/ubuntu-20.04-minimal-cloudimg-amd64.img
             */

            throw new NotImplementedException();
        }

        public static async Task<bool> Uninstall(HostStorage hostStorage, InstallProcessUI ui)
        {
            throw new NotImplementedException();
        }
    }
}
