using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using wordslab.installer.localstorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add data persistence services
var localStorageManager = new LocalStorageManager();
builder.Services.AddSingleton<LocalStorageManager>(localStorageManager);
builder.Services.AddDbContextFactory<ConfigStore>(options => options.UseSqlite($"Data Source={Path.Combine(localStorageManager.ConfigDirectory.FullName,"wordslab-config.db")}"));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
} 
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

CreateDbIfNotExists(app);

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