using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.os;

namespace wordslab.manager.console.host
{    
    public abstract class ConfigCommand<TSettings> : CommandWithUI<TSettings> where TSettings : CommandSettings
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;

        public ConfigCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(ui)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
        }
    }

    public class ConfigInitCommand : ConfigCommand<ConfigInitCommand.Settings>
    {        
        public ConfigInitCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var vmManager = new VirtualMachinesManager(hostStorage, configStore);
            var config = AsyncUtil.RunSync(() => vmManager.ConfigureHostMachine(UI));
            if (config == null)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public class Settings : CommandSettings
        { }
    }

    public class ConfigInfoCommand : ConfigCommand<ConfigInfoCommand.Settings>
    {
        public ConfigInfoCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (configStore.HostMachineConfig == null)
            { 
                UI.WriteLine("Host machine config not yet initialized: please execute 'wordslab host init' first");
                UI.WriteLine();
                return 0;
            }

            var config = configStore.HostMachineConfig;
            UI.WriteLine($"Host machine config: {config.HostName}");
            UI.WriteLine("  Storage");
            UI.WriteLine($"  - vm cluster disks: {config.VirtualMachineClusterPath} (max {config.VirtualMachineClusterSizeGB} GB)");
            UI.WriteLine($"  - vm data disks: {config.VirtualMachineDataPath} (max {config.VirtualMachineDataSizeGB} GB)");
            UI.WriteLine($"  - backups: {config.BackupPath} (max {config.BackupSizeGB} GB)");
            UI.WriteLine("  Compute");
            UI.WriteLine($"  - max vm processors: {config.Processors}");
            UI.WriteLine($"  - max vm memory: {config.MemoryGB} GB");
            UI.WriteLine($"  - can use GPU? {config.CanUseGPUs}");
            UI.WriteLine("  Network");
            if (!OS.IsWindows)
            {
                UI.WriteLine($"  - SSH ports range: {config.SSHPort}-{config.SSHPort + 9}");
            }
            UI.WriteLine($"  - Kubernetes ports range: {config.KubernetesPort}-{config.KubernetesPort+9}");
            UI.WriteLine($"  - Http ports range: {config.HttpPort}-{config.HttpPort+9}");
            UI.WriteLine($"  - can expose Http on LAN? {config.CanExposeHttpOnLAN}");
            UI.WriteLine($"  - Https ports range: {config.HttpsPort}-{config.HttpsPort+9}");
            UI.WriteLine($"  - can expose Https on LAN? {config.CanExposeHttpsOnLAN}");
            UI.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }

    public class ConfigUpdateCommand : ConfigCommand<ConfigUpdateCommand.Settings>
    {
        public ConfigUpdateCommand(HostStorage hostStorage, ConfigStore configStore, ICommandsUI ui) : base(hostStorage, configStore, ui)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (configStore.HostMachineConfig == null)
            {
                UI.WriteLine("Host machine config not yet initialized: please execute 'wordslab host init' first");
                UI.WriteLine();
                return 0;
            }

            UI.WriteLine("Checking local virtual machines state ...");
            UI.WriteLine();

            var vmManager = new VirtualMachinesManager(hostStorage, configStore);
            var vms = vmManager.ListLocalVMs();
            var runningVMs = vms.Where(vm => vm.IsRunning()).Count();
            if(runningVMs > 0)
            {
                UI.WriteLine($"{runningVMs} local virtual machines are currently running: you need to stop them before you can update the sandbox configuration");
                UI.WriteLine();
                return 1;
            }

            AsyncUtil.RunSync(() => vmManager.UpdateHostMachineConfig(UI));
            return 0;
        }

        public class Settings : CommandSettings
        { }
    }
}
