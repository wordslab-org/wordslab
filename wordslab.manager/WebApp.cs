namespace wordslab.manager; 

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

public static class WebApp
{
    public const int DEFAULT_PORT = 8080;

    // This attribute is a workaround to fix a bug in Blazor server when publishing with assembly trimming
    // https://github.com/dotnet/aspnetcore/issues/37143#issuecomment-931726256
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HeadOutlet))]
    public static void Run(WebApplicationBuilder builder, int? port, string[] args)
    {
        Console.WriteLine($"wordslab manager v{ConsoleApp.Version}");
        Console.WriteLine();

        // Register ASP.NET services for the web application

        builder.Services.AddRazorPages();
        builder.Services.Configure<RazorPagesOptions>(options => options.RootDirectory = "/web/Pages");
        builder.Services.AddServerSideBlazor();

        // Initialize the web application host

        var app = builder.Build();

        // app properties:
        // - IConfiguration             Configuration   Access the application configuration keys
        // - IHostEnvironment           Environment     Get ApplicationName and EnvironmentName
        // - IHostApplicationLifetime   Lifetime	    Run custom code on application events via: ApplicationStarted / ApplicationStopping / ApplicationStopped	
        // - ILogger                    Logger	        Default logger for the application via: LogInformation(EventId, String, Object[])
        // - IServiceProvider           Services	    Get registered dependency via: GetService(Type)

        // Configure the ASP.NET request middleware

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        // Set port number
        var url = $"http://localhost:{port}";
        Console.WriteLine($"Open a browser at the following URL : {url}");
        Console.WriteLine();

        // Open browser
        try
        {
            WebBrowser.Open(url);
        }
        catch (Exception) { } // Ignore exceptions - best effort only
        
        Console.WriteLine("Press Ctrl-C to exit the application ...");
        Console.WriteLine();

        app.Run(url);
    }
}
