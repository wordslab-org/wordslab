using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.os;

namespace wordslab.manager.console.host
{    public abstract class ConfigCommand<TSettings> : Command<TSettings> where TSettings : CommandSettings
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;
        protected readonly VirtualMachinesManager vmManager;

        public ConfigCommand(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
        }
    }

    public class ConfigInitCommand : ConfigCommand<ConfigInitCommand.Settings>
    {        
        public ConfigInitCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var installUI = new ConsoleProcessUI();
            var vmManager = new VirtualMachinesManager(hostStorage, configStore);
            var config = AsyncUtil.RunSync(() => vmManager.ConfigureHostMachine(installUI));
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
        public ConfigInfoCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (configStore.HostMachineConfig == null)
            { 
                AnsiConsole.WriteLine("Host machine config not yet initialized: please execute 'wordslab host init' first");
                AnsiConsole.WriteLine();
                return 0;
            }

            var config = configStore.HostMachineConfig;
            AnsiConsole.WriteLine($"Host machine config: {config.HostName}");
            AnsiConsole.WriteLine("  Storage");
            AnsiConsole.WriteLine($"  - vm cluster disks: {config.VirtualMachineClusterPath} (max {config.VirtualMachineClusterSizeGB} GB)");
            AnsiConsole.WriteLine($"  - vm data disks: {config.VirtualMachineDataPath} (max {config.VirtualMachineDataSizeGB} GB)");
            AnsiConsole.WriteLine($"  - backups: {config.BackupPath} (max {config.BackupSizeGB} GB)");
            AnsiConsole.WriteLine("  Compute");
            AnsiConsole.WriteLine($"  - max vm processors: {config.Processors}");
            AnsiConsole.WriteLine($"  - max vm memory: {config.MemoryGB} GB");
            AnsiConsole.WriteLine($"  - can use GPU? {config.CanUseGPUs}");
            AnsiConsole.WriteLine("  Network");
            if (!OS.IsWindows)
            {
                AnsiConsole.WriteLine($"  - SSH ports range: {config.SSHPort}-{config.SSHPort + 9}");
            }
            AnsiConsole.WriteLine($"  - Kubernetes ports range: {config.KubernetesPort}-{config.KubernetesPort+9}");
            AnsiConsole.WriteLine($"  - Http ports range: {config.HttpPort}-{config.HttpPort+9}");
            AnsiConsole.WriteLine($"  - can expose Http on LAN? {config.CanExposeHttpOnLAN}");
            AnsiConsole.WriteLine($"  - Https ports range: {config.HttpsPort}-{config.HttpsPort+9}");
            AnsiConsole.WriteLine($"  - can expose Https on LAN? {config.CanExposeHttpsOnLAN}");
            AnsiConsole.WriteLine();

            return 0;
        }

        public class Settings : CommandSettings
        { }
    }

    public class ConfigUpdateCommand : ConfigCommand<ConfigUpdateCommand.Settings>
    {
        public ConfigUpdateCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (configStore.HostMachineConfig == null)
            {
                AnsiConsole.WriteLine("Host machine config not yet initialized: please execute 'wordslab host init' first");
                AnsiConsole.WriteLine();
                return 1;
            }

            AnsiConsole.WriteLine("ERROR: host config update command not yet implemented");
            return -1;
        }

        public class Settings : CommandSettings
        { }
    }
}
