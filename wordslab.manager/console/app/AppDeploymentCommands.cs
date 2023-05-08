using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.console.app
{
    public class AppDeploymentListCommand : AppCommand<VmNameSettings>
    {
        public AppDeploymentListCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmNameSettings settngs)
        {
            AnsiConsole.MarkupLine("app deployment list command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }

    public class AppDeploymentCreateCommand : AppCommand<AppDeploymentCreateCommand.Settings>
    {
        public AppDeploymentCreateCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, Settings settngs)
        {
            AnsiConsole.MarkupLine("app deployment create command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }

        public class Settings : VmAndDeploymentNameSettings
        {
            [Description("Kubernetes app name")]
            [CommandArgument(2, "[app]")]
            public string AppName { get; init; }
        }
    }

        public class VmAndDeploymentNameSettings : VmNameSettings
    {
        [Description("Kubernetes app deployment namespace")]
        [CommandArgument(1, "[namespace]")]
        public string NamespaceName { get; init; }
    }

    public class AppDeploymentStopCommand : AppCommand<VmAndDeploymentNameSettings>
    {
        public AppDeploymentStopCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage,  configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settngs)
        {
            AnsiConsole.MarkupLine("app deployment stop command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }


    public class AppDeploymentStartCommand : AppCommand<VmAndDeploymentNameSettings>
    {
        public AppDeploymentStartCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settngs)
        {
            AnsiConsole.MarkupLine("app deployment start command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }

    public class AppDeploymentDeleteCommand : AppCommand<VmAndDeploymentNameSettings>
    {
        public AppDeploymentDeleteCommand(HostStorage hostStorage, ConfigStore configStore) : base(hostStorage, configStore)
        { }

        protected override int ExecuteCommand(VirtualMachine vm, CommandContext context, VmAndDeploymentNameSettings settngs)
        {
            AnsiConsole.MarkupLine("app deployment delete command not yet implemented");
            AnsiConsole.WriteLine();
            return 0;
        }
    }
}
