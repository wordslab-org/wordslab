using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using wordslab.manager.os;
using wordslab.manager.vm.qemu;

namespace wordslab.manager.config
{
    public enum VirtualMachineState
    {
        Starting,
        Running,
        Failed,
        Stopped,
        Killed
    }

    [PrimaryKey(nameof(Name),nameof(StartTimestamp))]
    public class VirtualMachineInstance : BaseConfig
    {
        private VirtualMachineInstance() { }

        public VirtualMachineInstance(string name, VirtualMachineConfig config, ComputeSpec computeStartArguments = null, GPUSpec gpuStartArguments = null, List<string> startArgumentsMessages = null)
        {
            Name = name;
            Config = config;
            ComputeStartArguments = computeStartArguments;
            GPUStartArguments = gpuStartArguments;
            if (startArgumentsMessages != null)
            {
                StartArgumentsMessages = string.Join(". ", startArgumentsMessages);
            }
            State = VirtualMachineState.Starting;
            StartTimestamp = DateTime.Now;
        }

        public void Started(int vmProcessId, string vmIPAddress, string kubeconfig, string executionMessages = null)
        {
            State = VirtualMachineState.Running;
            VmProcessId = vmProcessId;
            VmIPAddress = vmIPAddress;
            Kubeconfig = kubeconfig;
            ExecutionMessages = executionMessages;
        }

        public void Failed(string executionMessages = null)
        {
            State = VirtualMachineState.Failed;
            StopTimestamp = DateTime.Now;
            ExecutionMessages = executionMessages;
        }

        public void Stopped(string executionMessages = null)
        {
            State = VirtualMachineState.Stopped;
            StopTimestamp = DateTime.Now;
            ExecutionMessages = executionMessages;
        }

        public void Killed(string executionMessages = null)
        {
            State = VirtualMachineState.Killed;
            StopTimestamp = DateTime.Now;
            ExecutionMessages = executionMessages;
        }

        public string Name { get; set; }

        public DateTime StartTimestamp { get; internal set; }

        // Config and start arguments overrides

        public VirtualMachineConfig Config { get; set; }

        public ComputeSpec? ComputeStartArguments { get; set; }

        public GPUSpec? GPUStartArguments { get; set; }

        public string? StartArgumentsMessages { get; set; }

        // Execution state

        public VirtualMachineState State { get; set; }

        public int VmProcessId { get; set; }

        public string? VmIPAddress { get; set; }

        public string? Kubeconfig { get; set; }

        public DateTime? StopTimestamp { get; set; }

        public string? ExecutionMessages { get; set; }

        // URLs to access the instance

        public string GetSSHCommandParams()
        {
            if(OS.IsWindows)
            {
                // No SSH port with WSL on Windows
                return null;
            }

            // SSH to a qemu virtual machine
            if(Config.ForwardSSHPortOnLocalhost)
            {
                return $"-p {Config.HostSSHPort} {QemuDisk.ubuntuImageUser}@127.0.0.1";
            }
            else
            {
                return $"-p {Config.Spec.Network.SSHPort} {QemuDisk.ubuntuImageUser}@{VmIPAddress}";
            }
        }

        public string GetKubernetesServerURL()
        {
            if (Config.ForwardKubernetesPortOnLocalhost)
            {
                return $"https://127.0.0.1:{Config.HostKubernetesPort}";
            }
            else
            {
                return $"https://{VmIPAddress}:{Config.Spec.Network.KubernetesPort}";
            }
        }

        public string GetHttpAddressAndPort()
        {
            if(Config.ForwardHttpIngressPortOnLocalhost)
            {
                var ip = "127.0.0.1";
                if (Config.AllowHttpAccessFromLAN) 
                {
                    ip = Network.GetIPAddressOnLAN();
                }
                var formattedPort = Config.HostHttpIngressPort == 80 ? "" : $":{Config.HostHttpIngressPort}";
                return $"{ip}{formattedPort}";
            }
            else
            {
                var formattedPort = Config.Spec.Network.HttpIngressPort == 80 ? "" : $":{Config.Spec.Network.HttpIngressPort}";
                return $"{VmIPAddress}{formattedPort}";
            }
        }

        public string GetHttpsAddressAndPort()
        {
            if (Config.ForwardHttpsIngressPortOnLocalhost)
            {
                var ip = "127.0.0.1";
                if (Config.AllowHttpsAccessFromLAN)
                {
                    ip = Network.GetIPAddressOnLAN();
                }
                var formattedPort = Config.HostHttpsIngressPort == 443 ? "" : $":{Config.HostHttpsIngressPort}";
                return $"{ip}{formattedPort}";
            }
            else
            {
                var formattedPort = Config.Spec.Network.HttpsIngressPort == 443 ? "" : $":{Config.Spec.Network.HttpsIngressPort}";
                return $"{VmIPAddress}{formattedPort}";
            }
        }

        // Comparison

        public override bool Equals(object? obj)
        {
            var instance = obj as VirtualMachineInstance;
            if (instance == null) return false;

            var equals = true;
            equals = equals && instance.Name == Name;
            equals = equals && instance.Config.Equals(Config);
            equals = equals && ((instance.ComputeStartArguments == null && ComputeStartArguments == null) || instance.ComputeStartArguments.Equals(ComputeStartArguments));
            equals = equals && ((instance.GPUStartArguments == null && GPUStartArguments == null) || instance.GPUStartArguments.Equals(GPUStartArguments));
            equals = equals && instance.StartArgumentsMessages == StartArgumentsMessages;
            equals = equals && instance.State == State;
            equals = equals && instance.VmProcessId == VmProcessId;
            equals = equals && instance.VmIPAddress == VmIPAddress;
            equals = equals && instance.Kubeconfig == Kubeconfig;
            equals = equals && instance.StartTimestamp == StartTimestamp;
            equals = equals && instance.StopTimestamp == StopTimestamp;
            equals = equals && instance.ExecutionMessages == ExecutionMessages;
            return equals;
        }

        // Display

        public class DisplayStatus
        {
            public string State;
            public string StartedOn;
            public string StoppedOn;
            public string RunningTime;
            public string Processors;
            public string ProcessorsUsage;
            public string Memory;
            public string MemoryUsage;
            public string GPU;
            public string GPUUsage;
        }

        public DisplayStatus GetDisplayStatus()
        {
            var status = new DisplayStatus();
            status.State = State.ToString().ToLowerInvariant();
            status.StartedOn = StartTimestamp.ToString("MM/dd/yy HH:mm:ss");
            status.StoppedOn = StopTimestamp==null?"":StopTimestamp.Value.ToString("MM/dd/yy HH:mm:ss");
            status.RunningTime = DateTime.Now.Subtract(StartTimestamp).ToString(@"d\.hh\:mm\:ss");
            status.Processors = ComputeStartArguments.Processors.ToString();
            status.Memory = $"{ComputeStartArguments.MemoryGB} GB";
            status.GPU = GPUStartArguments.ToString();

            if (State == VirtualMachineState.Running)
            {
                status.ProcessorsUsage = $"{Compute.GetPercentCPUTime()}%";
                var vmProcess = Process.GetProcessById(VmProcessId);
                if (vmProcess != null)
                { 
                    status.MemoryUsage = $"{vmProcess.WorkingSet64 / 1024d / 1024d / 1024d:0.00} GB";
                }                
                if(GPUStartArguments.GPUCount > 0)
                {
                    foreach(var gpuUsage in Compute.GetNvidiaGPUsUsage())
                    {
                        if(status.GPUUsage == null) 
                        {
                            status.GPUUsage = "";
                        }
                        else
                        {
                            status.GPUUsage += " | ";
                        }
                        status.GPUUsage += $"{gpuUsage.PercentGPUTime}% and {gpuUsage.MemoryUsedMB/1024d:0.00} GB used";
                    }
                }
            }

            return status;
        }
    }
}
