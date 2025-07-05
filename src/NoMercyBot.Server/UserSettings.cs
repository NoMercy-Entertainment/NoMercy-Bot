using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Extensions;
using NoMercyBot.Globals.Information;
using NoMercyBot.Globals.SystemCalls;

namespace NoMercyBot.Server;

public static class UserSettings
{
    public static bool TryGetUserSettings(out Dictionary<string, string> settings)
    {
        settings = new();

        try
        {
            using AppDbContext context = new();
            List<Configuration> configuration = context.Configurations.ToList();

            foreach (Configuration? config in configuration) settings[config.Key] = config.Value;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void ApplySettings(Dictionary<string, string> settings)
    {
        foreach (KeyValuePair<string, string> setting in settings)
        {
            Logger.App($"Configuration: {setting.Key} = {setting.Value}");
            switch (setting.Key)
            {
                case "internalPort":
                    Config.InternalServerPort = int.Parse(setting.Value);
                    break;
                case "queueRunners":
                    Config.QueueWorkers = new(Config.QueueWorkers.Key, setting.Value.ToInt());
                    break;
                case "cronRunners":
                    Config.CronWorkers = new(Config.CronWorkers.Key, setting.Value.ToInt());
                    break;
                case "swagger":
                    Config.Swagger = setting.Value.ToBoolean();
                    break;
            }
        }
    }
}