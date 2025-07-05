using Microsoft.EntityFrameworkCore;
using NoMercyBot.Globals.SystemCalls;
using Serilog.Events;

namespace NoMercyBot.Database.Seeds;

public static class Seed
{
    public static async Task Init()
    {
        await using AppDbContext dbContext = new();
        await EnsureDatabaseCreated(dbContext);
        
        await ServiceSeed.SeedServices(dbContext);
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