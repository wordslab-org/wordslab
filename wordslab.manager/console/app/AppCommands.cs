using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.apps;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console.app
{
    public class AppInfoCommand : Command<AppInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.MarkupLine("app info command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }

        public class Settings : CommandSettings
        {
            [Description("Kubernetes app URL")]
            [CommandArgument(0, "[url]")]
            public string AppURL { get; init; }
        }
    }

    public class VmNameSettings : CommandSettings
    {
        [Description("Virtual machine name")]
        [CommandArgument(0, "[vm]")]
        public string VmName { get; init; }
    }

    public class VmAndAppNameSettings : VmNameSettings
    {
        [Description("Kubernetes app name")]
        [CommandArgument(1, "[app]")]
        public string AppName { get; init; }
    }

    public abstract class AppCommand<TSettings> : Command<TSettings> where TSettings : VmNameSettings
    {
        protected readonly HostStorage hostStorage;
        protected readonly ConfigStore configStore;
        protected readonly VirtualMachinesManager vmManager;
        protected readonly KubernetesAppsManager appManager;

        public AppCommand(HostStorage hostStorage, ConfigStore configStore)
        {
            this.hostStorage = hostStorage;
            this.configStore = configStore;
            this.vmManager = new VirtualMachinesManager(hostStorage, configStore);
            this.appManager = new KubernetesAppsManager(configStore);
        }

        public override int Execute(CommandContext context, TSettings settings)
        {
            var vmName = settings.VmName;
            if (String.IsNullOrEmpty(vmName))
            {
                AnsiConsole.WriteLine("Virtual machine name argument is missing");
                return 1;
            }

            var vm = vmManager.TryFindLocalVM(vmName);
            if (vm == null)
            {
                AnsiConsole.WriteLine($"Could not find a local virtual machine named: {vmName}");
                return 1;
            }

            if (!vm.IsRunning())
            {
                AnsiConsole.WriteLine($"Virtual machine {vmName} is already running");
                return 1;
            }
             
            return ExecuteCommand(vm, context, settings);
        }

        protected abstract int ExecuteCommand(VirtualMachine vm, CommandContext context, TSettings settings);
    }

    public class AppListCommand : AppCommand<VmNameSettings>
    {
        public AppListCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameSettings settngs)
        {
            AnsiConsole.MarkupLine("app list command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }

    public class AppDownloadCommand : AppCommand<AppDownloadCommand.Settings>
    {
        public AppDownloadCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, Settings settngs)
        {
            AnsiConsole.MarkupLine("app download command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }

        public class Settings : VmNameSettings
        {
            [Description("Kubernetes app URL")]
            [CommandArgument(1, "[url]")]
            public string AppURL { get; init; }
        }
    }

    public class AppRemoveCommand : AppCommand<VmAndAppNameSettings>
    {
        public AppRemoveCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndAppNameSettings settngs)
        {
            AnsiConsole.MarkupLine("app remove command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }
}
