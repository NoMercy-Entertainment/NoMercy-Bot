﻿using Microsoft.Extensions.DependencyInjection;

namespace NoMercyBot.Services.Spotify;

public static class SpotifyServiceExtensions
{
    public static IServiceCollection AddSpotifyServices(this IServiceCollection services)
    {
        services.AddSingleton<SpotifyApiService>();
        services.AddSingleton<SpotifyAuthService>();
        services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<SpotifyAuthService>());
        
        return services;
    }
}