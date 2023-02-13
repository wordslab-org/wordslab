namespace wordslab.manager;

using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using wordslab.manager.console;
using wordslab.manager.console.host;

public static class ConsoleApp
{
    public static readonly Version Version = new Version(0, 8, 2);

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
          install
          config
            info
            update
          vm
            list
            status [machinename]
            create [machinename] [cores] [memory] [gpu] [disksizes]
            update [machinename] [cores] [memory] [gpu] [disksizes]
            start [machinename] ([cores] [memory] [gpu]) 
            stop [machinename] 
            delete [machinename]
          system
            info
            status
          storage
            info
            status
            quota [disk] [size]
            move [storagearea] [directory]
            analyze [storagearea]
            clean [storagearea] [retentionperiod]
          compute
            info
            status
            quota [cpu|gpu|memory] [number|size]
            analyze [cpu|gpu|memory]
          network
            info
            status
            config [platformservice] [portnumber] [exposelan]
          os
            info
            hypervisor [enable|disable]
            update [packagename]
        cloud
          install
          config
            info
            update
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
            update [machinename] [cores] [memory] [gpu] [disksizes]
            start [machinename] [timeout] ([cores] [memory] [gpu])
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
