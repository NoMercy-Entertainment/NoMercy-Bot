using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Globals.Information;
using NoMercyBot.Server.AppConfig;
using NoMercyBot.Services;

namespace NoMercyBot.Server;

public class Startup
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly StartupOptions _options;
    
    public Startup(IApiVersionDescriptionProvider provider, StartupOptions options)
    {
        _provider = provider;
        _options = options;
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName("NoMercyBot")
            .PersistKeysToFileSystem(new(AppFiles.ConfigPath));
    
        ServiceConfiguration.ConfigureServices(services);
        
        services.AddSingleton(_options);
    }

    public void Configure(IApplicationBuilder app)
    {
        TokenStore.Initialize(app.ApplicationServices);
    
        using IServiceScope? serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope();
        AppDbContext? context = serviceScope?.ServiceProvider.GetRequiredService<AppDbContext>();
        
        ServiceResolver serviceResolver = app.ApplicationServices.GetRequiredService<ServiceResolver>();
        serviceResolver.InitializeAllServices().Wait();

        try
        {
            // Check if migration is needed
            if (context?.Database.GetPendingMigrations().Any() == true)
            {
                context.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
                context.Database.ExecuteSqlRaw("PRAGMA encoding = 'UTF-8'");
                context.Database.Migrate();
                
                Console.WriteLine("Media database migrations applied.");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying migrations: {ex.Message}");
        }
        
        ApplicationConfiguration.ConfigureApp(app, _provider);
        
    }
}