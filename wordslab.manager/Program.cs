﻿using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Serilog;
using wordslab.manager.storage;

[assembly: InternalsVisibleToAttribute("wordslab.manager.test")]

// Configure the application host

var builder = WebApplication.CreateBuilder(args);

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
        ConfigStore.CreateDbIfNotExistsAndInitializeHostStorage(hostServiceProvider);
    }

    // Start a console application (which may then start a web application in ManagerCommand)
    return wordslab.manager.ConsoleApp.Run(builder, args);
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