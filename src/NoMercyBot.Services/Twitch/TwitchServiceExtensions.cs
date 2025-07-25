﻿using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Other;

namespace NoMercyBot.Services.Twitch;

public static class TwitchServiceExtensions
{
    public static void AddTwitchServices(this IServiceCollection services)
    {
        services.AddSingleton<TwitchAuthService>();
        services.AddSingleton<BotAuthService>();
        services.AddSingleton<TwitchApiService>();
        services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<TwitchAuthService>());
        services.AddTransient<HtmlMetadataService>();
        services.AddTransient<TwitchMessageDecorator>();
    }
}