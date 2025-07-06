using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Globals.Information;
using NoMercyBot.Server.AppConfig;
using NoMercyBot.Services;
using NoMercyBot.Services.Seeds;

namespace NoMercyBot.Server;

public class Startup
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly StartupOptions _options;
    private readonly ILogger<Startup> _logger;
    
    public Startup(IApiVersionDescriptionProvider provider, StartupOptions options, ILogger<Startup> logger)
    {
        _provider = provider;
        _options = options;
        _logger = logger;
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
        
        SeedService seedService = app.ApplicationServices.GetRequiredService<SeedService>();
        
        ServiceResolver serviceResolver = app.ApplicationServices.GetRequiredService<ServiceResolver>();

        try
        {
            // Configure SQLite for better performance and UTF-8 support
            context?.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
            context?.Database.ExecuteSqlRaw("PRAGMA encoding = 'UTF-8'");
            
            // First check if the database exists - if not, create it
            bool dbExists = context?.Database.CanConnect() ?? false;
            
            if (!dbExists)
            {
                _logger.LogInformation("Database doesn't exist. Creating database and applying migrations...");
                context?.Database.Migrate();
            }
            else
            {
                // Check if migration history table exists and has the correct records
                bool migrationTableExists;
                try
                {
                    migrationTableExists = context?.Database
                        .ExecuteSqlRaw("SELECT COUNT(*) FROM __EFMigrationsHistory") >= 0;
                }
                catch
                {
                    migrationTableExists = false;
                }
                
                // Get list of applied migrations in the database
                List<string> appliedMigrations = [];
                if (migrationTableExists)
                {
                    appliedMigrations = context?.Database.GetAppliedMigrations().ToList() ?? [];
                }
                
                // Get list of available migrations in code
                List<string> availableMigrations = context?.Database.GetMigrations().ToList() ?? [];
                
                if (migrationTableExists && appliedMigrations.Count == availableMigrations.Count)
                {
                    _logger.LogInformation("Database is up to date. No migrations needed.");
                }
                else
                {
                    _logger.LogInformation("Checking for pending migrations...");
                    bool hasPendingMigrations = context?.Database.GetPendingMigrations().Any() ?? false;
                    
                    if (hasPendingMigrations)
                    {
                        try
                        {
                            context?.Database.Migrate();
                            _logger.LogInformation("Migrations applied successfully.");
                        }
                        catch (Exception ex) when (ex.Message.Contains("already exists"))
                        {
                            _logger.LogInformation("Tables already exist. Ensuring migration history is up to date...");
                            
                            try
                            {
                                if (migrationTableExists)
                                {
                                    // Don't delete - just ensure all migrations are recorded
                                    List<string> pendingMigrations = context?.Database.GetPendingMigrations().ToList() ?? [];
                                    string version = typeof(AppDbContext).Assembly.GetName().Version?.ToString() ?? "1.0.0";
                                    
                                    foreach (string migration in pendingMigrations)
                                    {
                                        try
                                        {
                                            context?.Database.ExecuteSqlRaw(
                                                "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, {1})", 
                                                migration, 
                                                version);
                                            _logger.LogInformation("Added migration {Migration} to history", migration);
                                        }
                                        catch
                                        {
                                            _logger.LogInformation("Failed to add migration {Migration} to history", migration);
                                        }
                                    }
                                }
                                else
                                {
                                    // Create the migrations history table
                                    context?.Database.ExecuteSqlRaw(@"
                                        CREATE TABLE __EFMigrationsHistory (
                                            MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                                            ProductVersion TEXT NOT NULL
                                        );");
                                    
                                    // Add all migrations to history
                                    string version = typeof(AppDbContext).Assembly.GetName().Version?.ToString() ?? "1.0.0";
                                    foreach (string migration in availableMigrations)
                                    {
                                        context?.Database.ExecuteSqlRaw(
                                            "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, {1})", 
                                            migration, 
                                            version);
                                    }
                                    _logger.LogInformation("Migration history table created and populated.");
                                }
                            }
                            catch (Exception innerEx)
                            {
                                _logger.LogInformation($"Failed to update migration history: {innerEx.Message}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No pending migrations found.");
                    }
                }
            }

            // Run seeds after ensuring the database is properly set up
            seedService.StartAsync(CancellationToken.None).Wait();
            
            // Initialize services
            serviceResolver.InitializeAllServices().Wait();
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error during database setup: {ErrorMessage}", ex.Message);
        }
        
        ApplicationConfiguration.ConfigureApp(app, _provider);
    }
}