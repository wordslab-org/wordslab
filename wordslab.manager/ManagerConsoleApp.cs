using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
namespace wordslab.manager;

using System.ComponentModel;
using wordslab.manager.cli;

public static class ManagerConsoleApp
{
    public static readonly Version Version = new Version(0,1,1);

    public static int Run(string[] args)
    {
        var app = new CommandApp<ManagerCommand>();
        app.Configure(config => 
        {
            config.AddCommand<ManagerCommand>("manager")
                .WithDescription("Launch the wordslab manager web application");
            config.AddCommand<VersionCommand>("version")
                .WithDescription("Display wordslab manager version info");
            config.AddBranch("vm", config =>
            {
                config.SetDescription("Manage wordslab virtual machines");
                config.AddCommand<VmListCommand>("list")
                    .WithDescription("List all wordslab virtual machines with their current status");
            }
            );
        });

        return app.Run(args);
    }

    public class ManagerCommand : Command<ManagerCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            try
            {
                ManagerWebApp.Run(settings.Port, context.Remaining.Raw.ToArray());
            }
            catch(Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return -1;
            }
            return 0;
        }

        public class Settings : CommandSettings
        {
            [Description("Port number used to launch the wordslab manager web app")]
            [CommandOption("-p|--port")]
            [DefaultValue(ManagerWebApp.DEFAULT_PORT)]
            public int? Port { get; set; }
        }
    }

    public class VersionCommand : Command<VersionCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"wordslab manager version: {Version}");
            return 0;
        }

        public class Settings : CommandSettings
        {
        }
    }

}
