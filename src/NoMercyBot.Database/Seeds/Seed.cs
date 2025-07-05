namespace NoMercyBot.Database.Seeds;

public static class Seed
{
    public static async Task Init()
    {
        await using AppDbContext dbContext = new();
        await ServiceSeed.SeedServices(dbContext);
    }
}