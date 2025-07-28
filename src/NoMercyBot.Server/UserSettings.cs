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
            List<Configuration> configuration = context.Configurations
                .Where(configuration => string.IsNullOrWhiteSpace(configuration.SecureValue) && string.IsNullOrEmpty(configuration.Value))
                .ToList();

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
                    // await QueueRunner.SetWorkerCount(Config.QueueWorkers.Key, setting.Value.ToInt());
                    break;
                case "cronRunners":
                    Config.CronWorkers = new(Config.CronWorkers.Key, setting.Value.ToInt());
                    // await QueueRunner.SetWorkerCount(Config.CronWorkers.Key, setting.Value.ToInt());
                    break;
                case "swagger":
                    Config.Swagger = setting.Value.ToBoolean();
                    break;
                case "DnsServer":
                    // Config.DnsServer is readonly, cannot set
                    break;
                case "InternalClientPort":
                    Config.InternalClientPort = int.Parse(setting.Value);
                    break;
                case "QueueWorkers":
                    Config.QueueWorkers = new(Config.QueueWorkers.Key, setting.Value.ToInt());
                    break;
                case "CronWorkers":
                    Config.CronWorkers = new(Config.CronWorkers.Key, setting.Value.ToInt());
                    break;
                case "UseTts":
                    Config.UseTts = setting.Value.ToBoolean();
                    break;
                case "SaveTtsToDisk":
                    Config.SaveTtsToDisk = setting.Value.ToBoolean();
                    break;
                case "UseFrankerfacezEmotes":
                    Config.UseFrankerfacezEmotes = setting.Value.ToBoolean();
                    break;
                case "UseBttvEmotes":
                    Config.UseBttvEmotes = setting.Value.ToBoolean();
                    break;
                case "UseSevenTvEmotes":
                    Config.UseSevenTvEmotes = setting.Value.ToBoolean();
                    break;
                case "UseChatCodeSnippets":
                    Config.UseChatCodeSnippets = setting.Value.ToBoolean();
                    break;
                case "UseChatHtmlParser":
                    Config.UseChatHtmlParser = setting.Value.ToBoolean();
                    break;
                case "UseChatOgParser":
                    Config.UseChatOgParser = setting.Value.ToBoolean();
                    break;
            }
        }
    }
}