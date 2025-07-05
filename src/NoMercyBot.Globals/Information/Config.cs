namespace NoMercyBot.Globals.Information;

public static class Config
{

    public static readonly string DnsServer = "1.1.1.1";

    public static string UserAgent => $"NoMercyBot/{Software.Version} ( admin@nomercy.tv )";
    
    public static int InternalServerPort { get; set; } = 6037;
    public static int InternalClientPort { get; set; } = 6038;

    public static bool Swagger { get; set; } = true;

    public static KeyValuePair<string, int> QueueWorkers { get; set; } = new("queue", 1);
    public static KeyValuePair<string, int> CronWorkers { get; set; } = new("cron", 1);
}