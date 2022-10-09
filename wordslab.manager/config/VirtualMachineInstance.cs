using System.ComponentModel.DataAnnotations;

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

        [Key]
        public string Name { get; set; }

        [Key]
        public int Id { get; set; }

        // Config and start arguments overrides

        public VirtualMachineConfig Config { get; set; }

        public ComputeSpec ComputeStartArguments { get; set; }

        public GPUSpec GPUStartArguments { get; set; }

        public string StartArgumentsMessages { get; set; }

        // Execution state

        public VirtualMachineState State { get; set; }

        public int VmProcessId { get; set; }

        public string VmIPAddress { get; set; }

        public string Kubeconfig { get; set; }

        public DateTime StartTimestamp { get; set; }

        public DateTime StopTimestamp { get; set; }

        public string ExecutionMessages { get; set; }

        // Comparison

        public override bool Equals(object? obj)
        {
            var instance = obj as VirtualMachineInstance;
            if (instance == null) return false;

            var equals = true;
            equals = equals && instance.Name == Name;
            equals = equals && instance.Config == Config;
            equals = equals && instance.ComputeStartArguments == ComputeStartArguments;
            equals = equals && instance.GPUStartArguments == GPUStartArguments;
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
    }
}
