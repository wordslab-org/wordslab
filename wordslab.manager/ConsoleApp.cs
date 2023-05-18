namespace wordslab.manager;

using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using wordslab.manager.console;
using wordslab.manager.console.host;
using wordslab.manager.console.app;

public static class ConsoleApp
{
    public static readonly Version Version = new Version(0, 9, 1);

    public static int Run(string[] args, WebApplicationBuilder builder)
    {
        // Bridge ASP.NET dependency injection system with Spectre.Console dependency injection system
        var spectreServices = new TypeRegistrar(builder.Services);
        spectreServices.RegisterInstance(typeof(WebApplicationBuilder), builder);

        // Initialize and run a Spectre.Console console app
        spectreServices.RegisterInstance(typeof(ICommandsUI), new ConsoleCommandsUI());
        var commandApp = new CommandApp<ManagerCommand>(spectreServices);
        commandApp.Configure(config => ConfigureCommands(config));
        return commandApp.Run(args);
    }

    public static int InvokeFromAnotherUI(string[] args, IServiceCollection services, ICommandsUI ui)
    {
        // Bridge the calling app dependency injection system with Spectre.Console dependency injection system
        var spectreServices = new TypeRegistrar(services);

        // Initialize and run a Spectre.Console app with a custom UI
        spectreServices.RegisterInstance(typeof(ICommandsUI), ui);
        var commandApp = new CommandApp<ManagerCommand>(spectreServices);
        commandApp.Configure(config => ConfigureCommands(config));
        return commandApp.Run(args);
    }

    private static void ConfigureCommands(IConfigurator config)
    {
        // --- Default command: launch the web application ---
        config.AddCommand<ManagerCommand>("manager")
            .WithDescription("Launch the wordslab manager web application");
        // ---

        config.AddBranch("host", config =>
        {
            config.SetDescription("Manage wordslab virtual machines on your host machine");
            config.AddCommand<ConfigInitCommand>("init")
                        .WithDescription("Install software prequisites and configure your host machine sandbox");
            config.AddBranch("config", config =>
            {
                config.SetDescription("Display and update your host machine sandbox configuration");
                config.AddCommand<ConfigInfoCommand>("show")
                    .WithDescription("Display host machine sandbox configuration");
                config.AddCommand<ConfigUpdateCommand>("update")
                        .WithDescription("Update host machine sandbox configuration");
            });
            config.AddBranch("vm", config =>
            {
                config.SetDescription("Create and manage virtual machines on your host machine to run local wordslab clusters");
                config.AddCommand<VmListCommand>("list")
                    .WithDescription("List all wordslab virtual machines created on your local host");
                config.AddCommand<VmCreateCommand>("create")
                        .WithDescription("Create a new wordslab virtual machine on your local host");   
                config.AddCommand<VmStartCommand>("start")
                        .WithDescription("Start a local wordslab virtual machine on your local host");
                config.AddCommand<VmStopCommand>("stop")
                        .WithDescription("Stop a local wordslab virtual machine on your local host");
                config.AddCommand<VmStatusCommand>("status")
                        .WithDescription("Display the status of a specific wordslab virtual machine on your local host");
                config.AddCommand<VmAdviseCommand>("advise")
                    .WithDescription("Advise a minimum, recommended and maximum config for a local virtual machine");
                config.AddCommand<VmResizeCommand>("resize")
                        .WithDescription("Resize an existing wordslab virtual machine on your local host");
                config.AddCommand<VmDeleteCommand>("delete")
                        .WithDescription("DANGER - Delete a local wordslab virtual machine - ALL DATA WILL BE LOST");
            });
            config.AddBranch("system", config =>
            {
                config.SetDescription("Display host machine system information and usage metrics");
                config.AddCommand<SystemInfoCommand>("info")
                    .WithDescription("Display host machine hardware and operating system information");
                config.AddCommand<SystemStatusCommand>("status")
                    .WithDescription("Display host machine usage metrics: cpu, memory, storage, network");
            });
        });

        config.AddBranch("app", config =>
        {
            config.SetDescription("Manage kubernetes applications in your virtual machines");
            config.AddCommand<AppInfoCommand>("info")
                        .WithDescription("Show detailed information about a kubernetes app from its URL");
            config.AddCommand<AppListCommand>("list")
                .WithDescription("List all kubernetes apps downloaded in a running virtual machine");
            config.AddCommand<AppDownloadCommand>("download")
                        .WithDescription("Download a new kubernetes app in a running virtual machine");
            config.AddBranch("deployment", config =>
            {
                config.SetDescription("Manage kubernetes app deployments in your virtual machines");
                config.AddCommand<AppDeploymentListCommand>("list")
                    .WithDescription("List all kubernetes app deployments in a running virtual machine");
                config.AddCommand<AppDeploymentCreateCommand>("create")
                    .WithDescription("Deploy a previoulsy downloaded kubernetes app in a running virtual machine");
                config.AddCommand<AppDeploymentStopCommand>("stop")
                    .WithDescription("Temporarily free CPU and memory resources allocated to a kubernetes app deployment");
                config.AddCommand<AppDeploymentStartCommand>("start")
                    .WithDescription("Restart a previously stopped kubernetes app deployment in a running virtual machine");
                config.AddCommand<AppDeploymentDeleteCommand>("delete")
                    .WithDescription("Delete a kubernetes app deployment in a running virtual machine");
            });
            config.AddCommand<AppRemoveCommand>("remove")
                    .WithDescription("Remove all files previously downloaded for a kubernetes app in a vm");
        });

        config.AddCommand<VersionCommand>("version")
            .WithDescription("Display wordslab manager version info");
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
            throw new NotImplementedException("ManagerCommand is used for documentation only, it should be intercepted before launching the ConsoleApp");
        }

        public class Settings : CommandSettings
        {
            [Description("Port number used to launch the wordslab manager web app")]
            [CommandOption("-p|--port")]
            [DefaultValue(WebApp.DEFAULT_PORT)]
            public int? Port { get; set; }
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

