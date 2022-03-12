namespace wordslab.manager;

using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using wordslab.manager.cli;

public static class ConsoleApp
{
    public static readonly Version Version = new Version(0,0,5);

    public static int Run(WebApplicationBuilder builder, string[] args)
    {
        // Bridge ASP.NET dependency injection system with Spectre.Console dependency injection system
        var spectreServices = new TypeRegistrar(builder.Services);
        spectreServices.RegisterInstance(typeof(WebApplicationBuilder), builder);

        // Initialize and run a Spectre.Console console app
        var commandApp = new CommandApp<ManagerCommand>(spectreServices);
        commandApp.Configure(config => ConfigureCommands(config));
        return commandApp.Run(args);
    }

    private static void ConfigureCommands(IConfigurator config)
    {
        // Default command
        config.AddCommand<ManagerCommand>("manager")
            .WithDescription("Launch the wordslab manager web application");

        config.AddCommand<VersionCommand>("version")
            .WithDescription("Display wordslab manager version info");

        config.AddBranch("vm", config =>
        {
            config.SetDescription("Manage wordslab virtual machines");
            config.AddCommand<VmListCommand>("list")
                .WithDescription("List all wordslab virtual machines with their current status");
        });
    }

    public class ManagerCommand : Command<ManagerCommand.Settings>
    {
        private WebApplicationBuilder builder;

        public ManagerCommand(WebApplicationBuilder builder)
        {
            this.builder = builder;
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            try
            {
                WebApp.Run(builder, settings.Port, context.Remaining.Raw.ToArray());
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
            [DefaultValue(WebApp.DEFAULT_PORT)]
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

    private class TypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceCollection _builder;

        public TypeRegistrar(IServiceCollection builder)
        {
            _builder = builder;
        }

        public ITypeResolver Build()
        {
            return new TypeResolver(_builder.BuildServiceProvider());
        }

        public void Register(Type service, Type implementation)
        {
            _builder.AddSingleton(service, implementation);
        }

        public void RegisterInstance(Type service, object implementation)
        {
            _builder.AddSingleton(service, implementation);
        }

        public void RegisterLazy(Type service, Func<object> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            _builder.AddSingleton(service, (provider) => func());
        }
    }
    private class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceProvider _provider;

        public TypeResolver(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public object Resolve(Type type)
        {
            if (type == null)
            {
                return null;
            }

            return _provider.GetService(type);
        }

        public void Dispose()
        {
            if (_provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
