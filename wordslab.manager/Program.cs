using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Serilog;
using wordslab.manager;
using wordslab.manager.storage;

[assembly: InternalsVisibleToAttribute("wordslab.manager.test")]

// Configure the application host

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = "web" });

// builder properties:
// - IHostEnvironment       Environment     Gets or sets the name of the application or environment via: ApplicationName / EnvironmentName
// - IServiceCollection     Services        Dependency injection via: AddSingleton(Type, Object) / AddScoped(Type, Object) / AddTransient(Type, Object)
// - ConfigurationManager   Configuration   Setup configuration sources or Access configuration keys via: AppSettings[key] / ConnectionStrings[key]
// - ILoggingBuilder        Logging         Add logging providers 

// Initialize local storage and register local storage manager

var hostStorage = new HostStorage();
builder.Services.AddSingleton<HostStorage>(hostStorage);

// Configure logging to a local file with a daily rotation
// See: https://github.com/serilog/serilog-aspnetcore and https://github.com/serilog/serilog-sinks-file

var logPath = Path.Combine(hostStorage.LogsDirectory, "wordslab-.log");
Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.File(logPath, rollingInterval: RollingInterval.Day).CreateLogger();
builder.WebHost.UseSerilog();

try
{
    // Configure database connection and register an Entity Framework Core database context factory

    var databasePath = Path.Combine(hostStorage.ConfigDirectory, "wordslab-config.db");
    builder.Services.AddDbContextFactory<ConfigStore>(options => options.UseSqlite($"Data Source={databasePath}"));

    // Create the database if it doesn't exist and initialize the host storage directories

    using (var hostServiceProvider = builder.Services.BuildServiceProvider())
    {
        ConfigStore.CreateOrUpdateDbSchemaAndInitializeHostStorage(hostServiceProvider);
    }

    // Start a web application if launched without parameters or with the "manager" command
    if (args.Length == 0 || args[0] == "manager" || args[0] == "-p" || args[0] == "--port")
    {
        // Extract the optional "port" parameter
        var skipArgs = 0;
        var port = WebApp.DEFAULT_PORT;
        if (args.Length >= 1 && args[0] == "manager") skipArgs = 1;
        if(args.Length >= (skipArgs+2) && (args[skipArgs] == "-p" || args[skipArgs] == "--port"))
        {
            Int32.TryParse(args[skipArgs + 1], out port); 
            skipArgs += 2;
        }
        // Launch the web app        
        WebApp.Run(builder, port, args.Skip(skipArgs).ToArray());
        return 0;
    }
    // Start a console application if launched with any other command
    else
    {        
        return ConsoleApp.Run(builder, args);
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}