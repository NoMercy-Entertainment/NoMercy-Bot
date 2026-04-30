using System.Net.WebSockets;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Twitch;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Client;

namespace NoMercyBot.Services;

public static class EventSubServiceCollectionExtensions
{
    public static IServiceCollection AddEventSubServices(this IServiceCollection services)
    {
        // Register WebsocketClient as transient with a factory that pre-arms each
        // instance: replace the internal ClientWebSocket with one that has
        // CollectHttpResponseDetails=true so we can read Retry-After / Ratelimit-Reset
        // headers when the upgrade is rejected with a 429. TwitchLib's
        // EventSubWebsocketClient resolves WebsocketClient from DI on every connect,
        // so this guarantees every connect attempt is armed.
        services.AddTransient<WebsocketClient>(sp =>
        {
            ILogger<WebsocketClient>? logger = sp.GetService<ILogger<WebsocketClient>>();
            WebsocketClient wc = new(logger);

            FieldInfo? webSocketField = typeof(WebsocketClient).GetField(
                "_webSocket",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (webSocketField != null)
            {
                // Dispose the unconfigured original CWS the constructor created
                // before swapping in our flagged one — otherwise the original
                // ClientWebSocket leaks until the next GC pass.
                ClientWebSocket? original = webSocketField.GetValue(wc) as ClientWebSocket;

                ClientWebSocket fresh = new();
                fresh.Options.CollectHttpResponseDetails = true;
                webSocketField.SetValue(wc, fresh);

                original?.Dispose();
            }
            return wc;
        });

        // Register the EventSub websocket client via explicit factory. Without this,
        // DI sees two viable constructors (ILoggerFactory and the 3-arg DI one) and
        // throws "constructors are ambiguous" the moment we register WebsocketClient
        // above. Explicit factory picks the 3-arg ctor unambiguously and threads our
        // pre-armed WebsocketClient through.
        services.AddSingleton<EventSubWebsocketClient>(sp => new EventSubWebsocketClient(
            sp.GetRequiredService<ILogger<EventSubWebsocketClient>>(),
            sp,
            sp.GetRequiredService<WebsocketClient>()
        ));

        // Probe reads HttpStatusCode + HttpResponseHeaders off whichever ClientWebSocket
        // is currently inside the EventSubWebsocketClient (the library may have replaced it).
        services.AddSingleton<RateLimitProbe>();

        // Register the TwitchApiService as singleton to avoid duplicate instances
        services.AddSingleton<TwitchApiService>();

        // Register the EventSub services as singletons to ensure events work properly
        services.AddSingleton<TwitchEventSubService>();
        services.AddScoped<DiscordEventSubService>();
        services.AddScoped<ObsEventSubService>();

        // Register the TwitchWebsocketHostedService as a hosted service
        // This needs to be after TwitchEventSubService to ensure proper dependency resolution
        services.AddHostedService<TwitchWebsocketHostedService>();

        // Register the provider-specific services to the IEventSubService interface
        services.AddSingleton<IEventSubService>(sp =>
            sp.GetRequiredService<TwitchEventSubService>()
        );
        services.AddScoped<IEventSubService, DiscordEventSubService>(sp =>
            sp.GetRequiredService<DiscordEventSubService>()
        );
        services.AddScoped<IEventSubService, ObsEventSubService>(sp =>
            sp.GetRequiredService<ObsEventSubService>()
        );

        return services;
    }
}
