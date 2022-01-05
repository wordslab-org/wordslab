using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Runtime.InteropServices;
using wordslab.installer.localstorage;

Console.WriteLine($"wordslab-installer {wordslab.installer.AppVersion.Name}");
Console.WriteLine();

Console.WriteLine("Starting application :");
Console.WriteLine($"- Executable : {Process.GetCurrentProcess().MainModule.FileName}");

// Initialize local storage
var localStorageManager = new LocalStorageManager();

// Configure logging to file
// https://github.com/serilog/serilog-aspnetcore
// https://github.com/serilog/serilog-sinks-file
var logPath = Path.Combine(localStorageManager.LogsDirectory.FullName, "wordslab.installer.txt");
Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

Console.WriteLine($"- Log files  : {logPath}");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add data persistence services
var databasePath = Path.Combine(localStorageManager.ConfigDirectory.FullName, "wordslab-config.db");
builder.Services.AddSingleton<LocalStorageManager>(localStorageManager);
builder.Services.AddDbContextFactory<ConfigStore>(options => options.UseSqlite($"Data Source={databasePath}"));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

Console.WriteLine($"- Database   : {databasePath}");

// Choose fixed port number
var url = "http://localhost:5678";
builder.WebHost.UseUrls(url); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
} 
else
{
    app.UseDeveloperExceptionPage();
}
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Initialize database
CreateDbIfNotExists(app);

Console.WriteLine();
Console.WriteLine($"Open a browser at the following URL : {url}");

// Open browser
if (!builder.Environment.IsDevelopment())
{    
    OpenBrowser(url);
}

Console.WriteLine();
Console.WriteLine("Press Ctrl-C to exit the apllication ...");

app.Run();

static void CreateDbIfNotExists(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContextFactory = services.GetRequiredService<IDbContextFactory<ConfigStore>>();
            using var configStore = dbContextFactory.CreateDbContext();
            configStore.Database.EnsureCreated();
            configStore.Initialize();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred creating the config database.");
        }
    }
}

static void OpenBrowser(string url)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        Process.Start("xdg-open", url);  // Works ok on linux
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        Process.Start("open", url); // Not tested
    }
}