using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager;

public static class ManagerWebApp
{
    public const int DEFAULT_PORT = 3088;

    // This attribute is a workaround to fix a bug in Blazor server when publishing with assembly trimming
    // https://github.com/dotnet/aspnetcore/issues/37143#issuecomment-931726256
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HeadOutlet))]
    public static void Run(int? port, string[] args)
    {
        Console.WriteLine($"wordslab manager v{ManagerConsoleApp.Version}");
        Console.WriteLine();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
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
        if (!builder.Environment.IsDevelopment())
        {
            try
            {
                WebBrowser.Open(url);
            }
            catch (Exception) { } // Ignore exceptions - best effort only
        }
        
        Console.WriteLine("Press Ctrl-C to exit the application ...");
        Console.WriteLine();

        app.Run(url);
    }
}
