using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Services.Emotes;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Twitch.Scripting;
using NoMercyBot.Services.Widgets;

namespace NoMercyBot.Services.Twitch;

public static class TwitchServiceExtensions
{
    public static void AddTwitchServices(this IServiceCollection services)
    {
        services.AddSingleton<TwitchAuthService>();
        services.AddSingleton<BotAuthService>();
        services.AddSingleton<TwitchApiService>();
        services.AddSingleton<TwitchChatService>();
        services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<TwitchAuthService>());
        services.AddSingleton<UserChannelRefreshService>();
        services.AddTransient<HtmlMetadataService>();
        services.AddTransient<TwitchMessageDecorator>();
        services.AddTransient<TwitchCommandService>();
        services.AddSingleton<TwitchRewardService>();
        services.AddSingleton<TwitchRewardChangeService>();
        services.AddSingleton<RaidSuggestionService>();
        services.AddSingleton<RewardScriptLoader>();
        services.AddSingleton<RewardChangeScriptLoader>();

        services.AddSingletonHostedService<TwitchBadgeService>();
        services.AddSingletonHostedService<ShoutoutQueueService>();
        services.AddSingletonHostedService<AdScheduleService>();
        services.AddHostedService<ClaudeIpcService>();

        // Watch streaks now arrive via channel.chat.notification (notice_type: "watch_streak");
        // ChatEventHandler dispatches to this service. No IRC connection.
        services.AddSingleton<WatchStreakService>();
    }
}
