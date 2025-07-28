using TwitchLib.Client;
using TwitchLib.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using Microsoft.Extensions.DependencyInjection;

namespace NoMercyBot.Services.Twitch
{
    public class TwitchChatService : IDisposable
    {
        private readonly TwitchClient _userClient;
        private readonly TwitchClient _botClient;
        private readonly ILogger<TwitchChatService> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceScope _scope;
        private readonly AppDbContext _dbContext;
        private readonly bool _hasBotAccount;

        public TwitchChatService(ILogger<TwitchChatService> logger, IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _config = config;
            _scope = scopeFactory.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Service? twitchService = _dbContext.Services.FirstOrDefault(s => s.Name == "Twitch");
            if (twitchService == null || string.IsNullOrEmpty(twitchService.AccessToken))
                throw new InvalidOperationException("No Twitch service found or missing access token.");

            BotAccount? botAccount = _dbContext.BotAccounts.FirstOrDefault();
            _hasBotAccount = botAccount != null;

            ConnectionCredentials userCreds = new(
                twitchService.UserName,
                twitchService.AccessToken);
            _userClient = new();
            _userClient.Initialize(userCreds);
            _userClient.OnConnected += (s, e) => _logger.LogInformation($"User client connected to Twitch chat as {e.BotUsername}");
            _userClient.Connect();

            ConnectionCredentials botCreds = _hasBotAccount
                ? new(botAccount!.Username, botAccount.AccessToken)
                : userCreds;
            _botClient = new();
            _botClient.Initialize(botCreds);
            _botClient.OnConnected += (_, e) => _logger.LogInformation($"Bot client connected to Twitch chat as {e.BotUsername}");
            _botClient.Connect();
        }

        private void RefreshClients()
        {
            using IServiceScope scope = _scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Service? twitchService = dbContext.Services.FirstOrDefault(s => s.Name == "Twitch");
            if (twitchService == null || string.IsNullOrEmpty(twitchService.AccessToken))
                throw new InvalidOperationException("No Twitch service found or missing access token.");

            BotAccount? botAccount = dbContext.BotAccounts.FirstOrDefault();
            ConnectionCredentials userCreds = new(
                twitchService.UserName,
                twitchService.AccessToken);
            ConnectionCredentials botCreds = botAccount != null
                ? new(botAccount.Username, botAccount.AccessToken)
                : userCreds;

            _userClient.Initialize(userCreds);
            _botClient.Initialize(botCreds);
        }

        public void SendMessageAsUser(string channel, string message)
        {
            RefreshClients();
            _userClient.SendMessage(channel, message);
        }

        public void SendMessageAsBot(string channel, string message)
        {
            RefreshClients();
            _botClient.SendMessage(channel, message);
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
            tempClient.Connect();
            await Task.Delay(1000);
            tempClient.SendMessage(channel, message);
            await Task.Delay(500);
            tempClient.Disconnect();
        }

        public async Task<string[]> GetChatters(string channel)
        {
            RefreshClients();
            // await _botClient.OnExistingUsersDetected
            //     += (sender, e) => _logger.LogInformation($"Existing users detected in channel {channel}: {string.Join(", ", e.Users)}");
            return [];
        }

        public void SendThankYouAfterShoutout(string channel, string user)
        {
            string message = $"Thanks for the follow, {user}! Welcome to the channel!";
            SendMessageAsBot(channel, message);
        }

        public void Dispose()
        {
            _userClient?.Disconnect();
            _botClient?.Disconnect();
            _scope?.Dispose();
        }
    }
}
