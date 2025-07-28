using Asp.Versioning.ApiExplorer;
using NoMercyBot.Database;
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
        ServiceConfiguration.ConfigureServices(services);
        
        services.AddSingleton(_options);
    }

    public void Configure(IApplicationBuilder app)
    {
        TokenStore.Initialize(app.ApplicationServices);
        
        SeedService seedService = app.ApplicationServices.GetRequiredService<SeedService>();
        
        ServiceResolver serviceResolver = app.ApplicationServices.GetRequiredService<ServiceResolver>();

        // Ensure the database is created and migrated
        seedService.StartAsync(CancellationToken.None).Wait();

        // Initialize services
        serviceResolver.InitializeAllServices().Wait();
        
        ApplicationConfiguration.ConfigureApp(app, _provider);
    }
}