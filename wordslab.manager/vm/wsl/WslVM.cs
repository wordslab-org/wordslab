using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslVM : VirtualMachine
    {
        public static List<VirtualMachine> ListLocalVMs(ConfigStore configStore, HostStorage storage)
        {
            var vms = new List<VirtualMachine>();
            try
            {
                var vmNames = WslDisk.ListVMNamesFromClusterDisks(storage);
                var wslDistribs = Wsl.list();
                foreach (var vmName in wslDistribs.Join(vmNames, d => d.Distribution, name => VirtualDisk.GetServiceName(name, VirtualDiskFunction.Cluster), (d, n) => n).OrderBy(s => s))
                {
                    var vm = FindByName(vmName, configStore, storage);
                    vms.Add(vm);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not list the local virtual machines because one of them is in an inconsistent state : {e.Message}");
            }

            return vms;
        }

        public static VirtualMachine FindByName(string vmName, ConfigStore configStore, HostStorage storage)
        {
            // Find the the virtual machine virtual disks files in host storage
            var clusterDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            var dataDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (clusterDisk == null | dataDisk == null)
            {
                throw new FileNotFoundException($"Could not find virtual disks for a local virtual machine named {vmName}");
            }

            // Find the virtual machine configuration in the database
            var vmConfig = configStore.TryGetVirtualMachineConfig(vmName);
            if (vmConfig == null)
            {
                throw new Exception($"Could not find a configuration record for a local virtual machine named {vmName}");
            }

            // Initialize the virtual machine and its running state
            var vm = new WslVM(vmConfig, clusterDisk, dataDisk, configStore, storage);
            return vm;
        }

        public WslVM(VirtualMachineConfig vmConfig, VirtualDisk clusterDisk, VirtualDisk dataDisk, ConfigStore configStore, HostStorage storage)
            : base(vmConfig, clusterDisk, dataDisk, configStore, storage)
        {
            if (vmConfig.VmProvider != VirtualMachineProvider.Wsl)
            {
                throw new ArgumentException("VmProvider should be Wsl");
            }

            // Initialize the running state
            IsRunning();
        }

        public override bool IsRunning()
        {
            // Find running distribution
            var wslDistribFound = false;
            try
            {
                var wslDistribs = Wsl.list();
                wslDistribFound = wslDistribs.Any(d => d.Distribution == ClusterDisk.ServiceName && d.IsRunning);
            }
            catch { }

            // Sync with config database state
            if (wslDistribFound && RunningInstance == null)
            {
                var lastRunningInstance = configStore.TryGetLastVirtualMachineInstance(Name);
                if (lastRunningInstance == null || lastRunningInstance.VmProcessId != Wsl.GetVirtualMachineProcessId())
                {
                    throw new InvalidOperationException($"The WSL virtual machine {Name} was launched outside of wordslab manager: please stop it from your terminal before you can use it from within wordslab manager again");
                }
                else
                {
                    RunningInstance = lastRunningInstance;
                }
            }
            if (!wslDistribFound && RunningInstance != null)
            {
                RunningInstance.Killed($"A running WSL distribution for virtual machine {Name} was not found: it was killed outside of wordslab manager");
                configStore.SaveChanges();
                RunningInstance = null;
            }

            return wslDistribFound;
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

            // Update machine-wide WSL config if needed and allowed
            var wslConfig = Wsl.Read_wslconfig();
            var hostConfig = configStore.HostMachineConfig;
            if (hostConfig.Processors != wslConfig.processors || hostConfig.MemoryGB != wslConfig.memoryMB * 1024)
            {
                if (!Wsl.IsRunning())
                {
                    wslConfig.UpdateToVMSpec(hostConfig.Processors, hostConfig.MemoryGB);
                }
                else
                {
                    throw new Exception("Could not update machine-wide WSL configuration to match the current host machine sandbox params: another WSL virtual machine is already running");
                }
            }

            // Start the virtual machine
            try
            {
                // Start the two WSL distributions
                DataDisk.StartService();
                ClusterDisk.StartService();

                // Start k3s inside the virtual machine
                Wsl.execShell($"/root/{WslDisk.clusterDiskStartupScript} {DataDisk.ServiceName}", ClusterDisk.ServiceName, ignoreError: "screen size is bogus");

                // Here's an example PowerShell command to add a port proxy that listens on port 4000 on the host and connects it to port 4000 to the WSL 2 VM with IP address 192.168.101.100.
                // netsh interface portproxy add v4tov4 listenport=4000 listenaddress=0.0.0.0 connectport=4000 connectaddress=192.168.101.100

                // Get virtual machine IP and kubeconfig
                string ip = null;
                Wsl.execShell("hostname -I | grep -Eo \"^[0-9\\.]+\"", ClusterDisk.ServiceName, outputHandler: output => ip = output);
                string kubeconfig = null;
                Wsl.execShell("cat /etc/rancher/k3s/k3s.yaml", ClusterDisk.ServiceName, outputHandler: output => kubeconfig = output);

                var processId = Wsl.GetVirtualMachineProcessId();
                vmInstance.Started(processId, ip, kubeconfig, null);
                configStore.SaveChanges();
            }
            catch (Exception e)
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
                Wsl.terminate(ClusterDisk.ServiceName);
                DataDisk.StopService();

                RunningInstance.Stopped(null);
                configStore.SaveChanges();
            }
            catch (Exception e)
            {
                RunningInstance.Killed(e.Message);
                configStore.SaveChanges();
            }

            // Reset the running instance
            RunningInstance = null;
        }
    }
}
