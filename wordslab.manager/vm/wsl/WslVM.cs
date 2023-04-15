﻿using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.vm.wsl
{
    public class WslVM : VirtualMachine
    {
        public static List<string> ListLocalVMs(HostStorage storage)
        {
            var vmNames = WslDisk.ListVMNamesFromClusterDisks(storage);
            var wslDistribs = Wsl.list();
            // A WSL VM is composed of a virtual disk file AND a WSL distribution
            return wslDistribs.Join(vmNames, d => d.Distribution, name => VirtualDisk.GetServiceName(name, VirtualDiskFunction.Cluster), (d, n) => n).OrderBy(s => s).ToList();
        }           

        public static VirtualMachine TryFindByName(VirtualMachineConfig vmConfig, ConfigStore configStore, HostStorage storage)
        {
            var vmName = vmConfig.Name;

            // Find the the virtual machine virtual disks files in host storage
            var clusterDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Cluster, storage);
            var dataDisk = WslDisk.TryFindByName(vmName, VirtualDiskFunction.Data, storage);
            if (clusterDisk == null | dataDisk == null)
            {
                return null;
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
        }

        protected override int FindRunningProcessId()
        {
            var wslDistribFound = false;
            try
            {
                var wslDistribs = Wsl.list();
                wslDistribFound = wslDistribs.Any(d => d.Distribution == ClusterDisk.ServiceName && d.IsRunning);
            }
            catch { }

            var runningProcessId = -1;
            if(wslDistribFound)
            {
                runningProcessId = Wsl.GetVirtualMachineProcessId();
            }
            return runningProcessId;
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
                // Start the WSL distribution for the data disk
                DataDisk.StartService();

                // Start k3s inside the virtual machine on the cluster disk
                Wsl.execShell($"/root/{VirtualDisk.k3sStartupScript} 'wsl/{DataDisk.ServiceName}'", ClusterDisk.ServiceName, timeoutSec:300, ignoreError: "screen size is bogus");

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

                // Create the host network configuration
                var portsConfig = Config.GetNetworkPortsConfig();
                Network.CreateNetworkConfig(Name, vmInstance.VmIPAddress, portsConfig, storage.ScriptsDirectory, storage.LogsDirectory);
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
                Kill(RunningInstance, e.Message);
            }

            // Reset the running instance
            RunningInstance = null;
            
            // Delete all other vm artifacts
            CleanupAfterStopOrKill();
        }

        protected override void CleanupAfterStopOrKill()
        {
            // Delete the host network configuration
            var portsConfig = Config.GetNetworkPortsConfig();
            Network.DeleteNetworkConfig(Name, portsConfig, storage.ScriptsDirectory, storage.LogsDirectory);
        }

        public override int ExecuteCommand(string command, string commandArguments = "", int timeoutSec = 10, Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            if(!String.IsNullOrEmpty(commandArguments))
            {
                command = $"{command} {commandArguments}";
            }
            return Wsl.execShell(command, ClusterDisk.ServiceName, timeoutSec: timeoutSec, outputHandler: outputHandler, errorHandler: errorHandler, exitCodeHandler: exitCodeHandler);
        }
    }
}
