using TwitchLib.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace NoMercyBot.Services.Twitch
{
    public class TwitchChatService : IDisposable
    {
        private readonly TwitchClient _userClient;
        private readonly TwitchClient _botClient;
        private readonly ILogger<TwitchChatService> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceScope _scope;

        public TwitchChatService(ILogger<TwitchChatService> logger, IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _config = config;
            _scope = scopeFactory.CreateScope();
            AppDbContext dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Service? twitchService = dbContext.Services.FirstOrDefault(s => s.Name == "Twitch");
            if (twitchService == null || string.IsNullOrEmpty(twitchService.AccessToken))
                throw new InvalidOperationException("No Twitch service found or missing access token.");

            BotAccount? botAccount = dbContext.BotAccounts.FirstOrDefault();
            bool hasBotAccount = botAccount != null;

            ConnectionCredentials userCredentials = new(twitchService.UserName, twitchService.AccessToken);
            
            _userClient = new();
            _userClient.Initialize(userCredentials, twitchService.UserName);
            _userClient.OnConnected += OnConnected;
            _userClient.OnDisconnected += OnDisconnected;
            _userClient.ConnectAsync();

            ConnectionCredentials botCredentials = hasBotAccount 
                ? new(botAccount!.Username, botAccount.AccessToken) 
                : userCredentials;
            _botClient = new();
            _botClient.Initialize(botCredentials, twitchService.UserName);
            _botClient.OnConnected += OnConnected;
            _botClient.OnDisconnected += OnDisconnected;
            _botClient.ConnectAsync();
        }

        private void RefreshClients()
        {
            using IServiceScope scope = _scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Service? twitchService = dbContext.Services.FirstOrDefault(s => s.Name == "Twitch");
            if (twitchService == null || string.IsNullOrEmpty(twitchService.AccessToken))
                throw new InvalidOperationException("No Twitch service found or missing access token.");

            BotAccount? botAccount = dbContext.BotAccounts.FirstOrDefault();
            ConnectionCredentials connectionCredentials = new(
                twitchService.UserName,
                twitchService.AccessToken);
            ConnectionCredentials botCredentials = botAccount != null
                ? new(botAccount.Username, botAccount.AccessToken)
                : connectionCredentials;

            _userClient.Initialize(connectionCredentials);
            _botClient.Initialize(botCredentials);
        }

        public async Task SendMessageAsUser(string channel, string message)
        {
            try
            {
                await _userClient.SendMessageAsync(channel, message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send message as user. Attempting to refresh clients.");
                RefreshClients();
                await _userClient.SendMessageAsync(channel, message);
            }
        }
        
        public async Task SendReplyAsUser(string channel, string message, string replyToMessageId)
        {
            try
            {
                await _userClient.SendReplyAsync(channel, replyToMessageId, message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send reply as user. Attempting to refresh clients.");
                RefreshClients();
                await _userClient.SendReplyAsync(channel, replyToMessageId, message);
            }
        }

        public async Task SendMessageAsBot(string channel, string message)
        {
            try
            {
                await _botClient.SendMessageAsync(channel, message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send message as bot. Attempting to refresh clients.");
                RefreshClients();
                await _botClient.SendMessageAsync(channel, message);
            }
        }
        
        public async Task SendReplyAsBot(string channel, string message, string replyToMessageId)
        {
            try
            {
                await _botClient.SendReplyAsync(channel, replyToMessageId, message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send reply as bot. Attempting to refresh clients.");
                RefreshClients();
                await _botClient.SendReplyAsync(channel, replyToMessageId, message);
            }
        }

        public async Task SendOneOffMessage(string channel, string message, string? oauthToken = null)
        {
            using IServiceScope scope = _scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            BotAccount? botAccount = dbContext.BotAccounts.FirstOrDefault();
            Service? twitchService = dbContext.Services.FirstOrDefault(s => s.Name == "Twitch");
            if (twitchService == null || string.IsNullOrEmpty(twitchService.AccessToken))
                throw new InvalidOperationException("No Twitch service found or missing access token.");

            string username = botAccount?.Username ?? twitchService.UserName;
            string token = oauthToken ?? botAccount?.AccessToken ?? twitchService.AccessToken;

            TwitchClient tempClient = new();
            tempClient.Initialize(new(username, token));
            await tempClient.ConnectAsync();
            await Task.Delay(1000);
            await tempClient.SendMessageAsync(channel, message);
            await Task.Delay(500);
            await tempClient.DisconnectAsync();
        }

        public async Task<string[]> GetChatters(string channel)
        {
            RefreshClients();
            // await _botClient.OnExistingUsersDetected
            //     += (sender, e) => _logger.LogInformation($"Existing users detected in channel {channel}: {string.Join(", ", e.Users)}");
            return [];
        }

        public async Task SendThankYouAfterShoutout(string channel, string user)
        {
            string message = $"Thanks for the follow, {user}! Welcome to the channel!";
            await SendMessageAsBot(channel, message);
        }
        
        private Task OnConnected(object? sender, OnConnectedEventArgs e)
        {
            _logger.LogInformation($"Connected to Twitch as {e.BotUsername}");
            return Task.CompletedTask;
        }

        private Task OnDisconnected(object? sender, OnDisconnectedArgs e)
        {
            _logger.LogWarning($"Disconnected from Twitch: {e.BotUsername}");
        
            // _logger.LogInformation("Reconnecting to Twitch...");
            // RefreshClients();
            // _userClient.ConnectAsync();
            // _botClient.ConnectAsync();
                
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _userClient?.DisconnectAsync();
            _botClient?.DisconnectAsync();
            _scope?.Dispose();
        }
    }
}
