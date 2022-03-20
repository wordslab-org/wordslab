namespace wordslab.manager;

using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using wordslab.manager.cli;
using wordslab.manager.cli.host;

public static class ConsoleApp
{
    public static readonly Version Version = new Version(0, 0, 5);

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
        // --- Default command: launch the web application ---
        config.AddCommand<ManagerCommand>("manager")
            .WithDescription("Launch the wordslab manager web application");
        // ---

        /* Commands hierarchy
         
        manager

        host
          system
            info
            usage
          storage
            status
            quota [disk] [size]
            move [storagearea] [directory]
            analyze [storagearea]
            clean [storagearea] [retentionperiod]
          compute
            status
            quota [cpu|gpu|memory] [number|size]
            analyze [cpu|gpu|memory]
          network
            status
            config [platformservice] [portnumber] [exposelan]
          os
            status
            hypervisor [enable|disable]
            update [packagename]
          vm
            status
            create [cores] [memory] [gpu] [disksizes]
            update [cores] [memory] [gpu] [disksizes]
            start|stop
            delete
          secret
            list
            export [resourcepath] [secretfunction] [password] 
            import [resourcepath] [secretfunction] [password]
        cloud
          account
            list 
            connect [servicename]
            status [accountname]            
            forget [accountname]           
          bill
          vm
            list
            budget [accountname] [cores] [memory] [gpu] [disksizes]
            status [machinename]
            create [accountname] [machinename] [cores] [memory] [gpu] [disksizes]
            start [machinename] [timeout]
            stop [machinename]
            delete [machinename]
        cluster
          list
          status [clustername]
          storage
            status [clustername]            
            analyze [clustername] [storagearea]
            clean [clustername] [storagearea] [retentionperiod]
          compute
            status [clustername]
            analyze [clustername] [cpu|gpu|memory]    
          imagepack
            list
            install [imagepackfile]
            uninstall [imagepackname]
          environment
            list
            create [user|org|exec] [envname] [quotas]
            update [envname] [quotas]
            delete [envname]
        execenv
          list
          status [envname]
          storage
            status [envname]            
            analyze [envname] [storagearea]
            clean [envname] [storagearea] [retentionperiod]
          compute
            status [envname]
            analyze [envname] [cpu|gpu|memory]    
          app
            list
            install [appchartfile] [appchartparams] [ingressmappings]
            uninstall [appname]
          orglink
            create [envname] [orglinkname] [out:execenvkeyfile]
            connect [envname] [orglinkname] [in:orgenvkeyfile]
            delete [envname] [orglinkname]
          adminuser
            list
            create
            delete
        orgenv
          list
          status [envname]
          storage
            status [envname]            
            analyze [envname] [storagearea]
            clean [envname] [storagearea] [retentionperiod]
          compute
            status [envname]
            analyze [envname] [cpu|gpu|memory]
          identitylink
            list
            create [envname] [identitylinkname] [identityproviderproperties]
          githublink
            list
            create [envname] [githubreponame] [githubrepoproperties]
          execlink
            create [envname] [execlinkname] [out:execenvkeyfile]
            connect [envname] [execlinkname] [in:orgenvkeyfile]
            delete [envname] [execlinkname]
          adminuser
            list
            create
            delete

        version

        */

        config.AddBranch("host", config =>
        {
            config.SetDescription("Manage wordslab storage, compute and ports allocation on your local host machine");
            config.AddBranch("system", config =>
            {
                config.SetDescription("Display host machine system information and usage metrics");
                config.AddCommand<SystemInfoCommand>("info")
                    .WithDescription("Display host machine hardware and operating system information");
                config.AddCommand<SystemUsageCommand>("usage")
                    .WithDescription("Display host machine usage metrics (cpu, memory, storage, network)");
            });
            config.AddBranch("storage", config =>
            {
                config.SetDescription("Manage wordslab working directories and disk space quotas on your local host machine");
                config.AddCommand<StorageStatusCommand>("status")
                    .WithDescription("Display host machine working directories and disk space usage");

            });
            config.AddBranch("compute", config =>
            {
                config.SetDescription("Manage wordslab cpu, gpu, and memory quotas on your local host machine");
                config.AddCommand<ComputeStatusCommand>("status")
                    .WithDescription("Display host machine cpu, gpu, and memory usage");
            });
            config.AddBranch("network", config =>
            {
                config.SetDescription("Manage wordslab services ports and network traffic on your local host machine");
                config.AddCommand<NetworkStatusCommand>("status")
                    .WithDescription("Display wordslab services ports and network traffic on your host machine");
            });
            config.AddBranch("os", config =>
            {
                config.SetDescription("Manage operating system hypervisor and required packages on your local host machine");
                config.AddCommand<OsStatusCommand>("status")
                    .WithDescription("Display host machine hypervisor status and required os packages versions");
            });
            config.AddBranch("vm", config =>
            {
                config.SetDescription("Create and manage a virtual machine on your host machine to run a local wordslab cluster ");
                config.AddCommand<VmStatusCommand>("status")
                    .WithDescription("Display the status of the wordlab virtual machine on your local host");
            });
            config.AddBranch("secret", config =>
            {
                config.SetDescription("Import or export secrets used to access remote services on your local host machine");
                config.AddCommand<SecretListCommand>("list")
                    .WithDescription("List the secrets stored on your local host machine");
            });
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
            try
            {
                WebApp.Run(builder, settings.Port, context.Remaining.Raw.ToArray());
            }
            catch (Exception ex)
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
