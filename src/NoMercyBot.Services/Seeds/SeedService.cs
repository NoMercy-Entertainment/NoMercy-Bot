using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Other;
using Serilog.Events;

namespace NoMercyBot.Services.Seeds;

public class SeedService : IHostedService
{
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SeedService> _logger;

    public SeedService(IServiceScopeFactory serviceScopeFactory, ILogger<SeedService> logger)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database seeding process");
        
        try
        {
            // Ensure database is created
            await EnsureDatabaseCreated(_dbContext);
            
            // Seed services first (they're required by other seeds)
            await ServiceSeed.Init(_dbContext);
            
            // Seed event subscriptions
            await EventSubscriptionSeed.Init(_dbContext);
            
            // Get the PronounService to load pronouns from the API
            PronounService pronounService = _scope.ServiceProvider.GetRequiredService<PronounService>();
            await pronounService.LoadPronouns();
            
            _logger.LogInformation("Successfully completed database seeding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private static async Task EnsureDatabaseCreated(DbContext context)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception e)
        {
            Logger.Setup(e.Message, LogEventLevel.Error);
        }
    }
}
