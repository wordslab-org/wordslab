using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.qemu
{
    public class QemuVM : VirtualMachine
    {
        public static List<string> ListLocalVMs(HostStorage storage)
        {
            // A Qemu VM is only materialized by a virtual file on disk
            return QemuDisk.ListVMNamesFromClusterDisks(storage);
        }

        public static VirtualMachine TryFindByName(VirtualMachineConfig vmConfig, ConfigStore configStore, HostStorage storage)
        {
            var vmName = vmConfig.Name;

            // Find the the virtual machine virtual disks files in host storage
            var clusterDisk = QemuDisk.TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            var dataDisk = QemuDisk.TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (clusterDisk == null || dataDisk == null)
            {
                throw new Exception($"Could not find virtual disks for a local virtual machine named {vmName}");
            }
            
            // Initialize the virtual machine and its running state
            var vm = new QemuVM(vmConfig, clusterDisk, dataDisk, configStore, storage);
            return vm;
        }

        internal QemuVM(VirtualMachineConfig vmConfig, VirtualDisk clusterDisk, VirtualDisk dataDisk, ConfigStore configStore, HostStorage storage)
            : base(vmConfig, clusterDisk, dataDisk, configStore, storage)
        {
            if (vmConfig.VmProvider != VirtualMachineProvider.Qemu)
            {
                throw new ArgumentException("VmProvider should be Qemu");
            }

            // Initialize the running state
            IsRunning();
        }

        public override bool IsRunning()
        {
            // Find running process
            var qemuProc = Qemu.TryFindVirtualMachineProcess(ClusterDisk.StoragePath);
           
            // Sync with config database state
            if(qemuProc != null && RunningInstance == null)
            {
                var lastRunningInstance = configStore.TryGetLastVirtualMachineInstance(Name);
                if(lastRunningInstance == null || lastRunningInstance.VmProcessId != qemuProc.PID)
                {
                    throw new InvalidOperationException($"The qemu virtual machine {Name} was launched outside of wordslab manager: please stop it from your terminal before you can use it from within wordslab manager again");
                }
                else
                {
                    RunningInstance = lastRunningInstance;
                }
            }
            if(qemuProc == null && RunningInstance != null)
            {
                RunningInstance.Killed($"A running qemu process for virtual machine {Name} was not found: it was killed outside of wordslab manager");
                configStore.SaveChanges();
                RunningInstance = null;
            }

            return qemuProc != null;
        }

        public override VirtualMachineInstance Start(ComputeSpec computeStartArguments = null, GPUSpec gpuStartArguments = null)
        {
            // If the VM is already running, do nothing
            if (IsRunning())
            {
                return RunningInstance;
            }

            // Check start arguments and create a VM instance (state = Starting)
            var vmInstance = CheckStartArgumentsAndCreateInstance(computeStartArguments, gpuStartArguments);

            // Start the virtual machine
            try
            {
                // Start qemu process
                var processId = Qemu.StartVirtualMachine(computeStartArguments.Processors, computeStartArguments.MemoryGB, ClusterDisk.StoragePath, DataDisk.StoragePath, Config.HostSSHPort, Config.HostHttpIngressPort, Config.HostHttpsIngressPort, Config.HostKubernetesPort);

                // Start k3s inside the virtual machine
                SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", Config.HostSSHPort, $"sudo ./{VirtualDisk.k3sStartupScript} wordslab-data");

                // Get virtual machine IP and kubeconfig
                string ip = null;
                SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", Config.HostSSHPort, "hostname -I | grep -Eo \"^[0-9\\.]+\"", outputHandler: output => ip = output);
                string kubeconfig = null;
                SshClient.ExecuteRemoteCommand("ubuntu", "127.0.0.1", Config.HostSSHPort, "cat /etc/rancher/k3s/k3s.yaml", outputHandler: output => kubeconfig = output);

                vmInstance.Started(processId, ip, kubeconfig, null);
                configStore.SaveChanges();
            }
            catch(Exception e)
            {
                vmInstance.Failed(e.Message);
                configStore.SaveChanges();

                throw new Exception($"Failed to start virtual machine {Name}: {e.Message}");
            }

            // Set the running instance
            RunningInstance = vmInstance;
            return RunningInstance;
        }

        public override void Stop()
        {
            if (!IsRunning())
            {
                return;
            }

            // Stop the virtual machine
            try
            {
                Qemu.StopVirtualMachine(RunningInstance.VmProcessId);

                RunningInstance.Stopped(null);
                configStore.SaveChanges();
            }
            catch(Exception e)
            {
                RunningInstance.Killed(e.Message);
                configStore.SaveChanges();
            }

            // Reset the running instance
            RunningInstance = null;
        }
    }
}
