using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Globals.Information;
using NoMercyBot.Services.Interfaces;

namespace NoMercyBot.Services.Twitch.Scripting;

public class RewardScriptLoader
{
    private readonly TwitchRewardService _rewardService;
    private readonly TwitchChatService _twitchChatService;
    private readonly TwitchApiService _twitchApiService;
    private readonly ILogger<RewardScriptLoader> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppDbContext _appDbContext;

    public RewardScriptLoader(
        TwitchRewardService rewardService,
        TwitchChatService twitchChatService,
        TwitchApiService twitchApiService,
        AppDbContext appDbContext,
        ILogger<RewardScriptLoader> logger,
        IServiceScopeFactory scopeFactory)
    {
        _rewardService = rewardService;
        _twitchChatService = twitchChatService;
        _twitchApiService = twitchApiService;
        _appDbContext = appDbContext;
        _logger = logger;
        IServiceScope scope = scopeFactory.CreateScope();
        _serviceProvider = scope.ServiceProvider;
    }

    public async Task LoadAllAsync()
    {
        foreach (string file in Directory.GetFiles(AppFiles.RewardsPath, "*.cs"))
        {
            await LoadScriptAsync(file);
        }
    }

    private async Task LoadScriptAsync(string filePath)
    {
        string scriptCode = await File.ReadAllTextAsync(filePath);
        string rewardName = Path.GetFileNameWithoutExtension(filePath);
        try
        {
            ScriptOptions options = ScriptOptions.Default;

            IEnumerable<string> assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location);

            options = options.AddReferences(assemblies);

            IReward reward = await CSharpScript.EvaluateAsync<IReward>(scriptCode, options);

            TwitchReward twitchReward = new()
            {
                RewardId = reward.RewardId,
                RewardTitle = reward.RewardTitle,
                Permission = reward.Permission,
                Callback = async ctx =>
                {
                    RewardScriptContext scriptCtx = new()
                    {
                        Channel = ctx.Channel,
                        BroadcasterId = ctx.BroadcasterId,
                        RewardId = ctx.RewardId,
                        RewardTitle = ctx.RewardTitle,
                        RedemptionId = ctx.RedemptionId,
                        User = ctx.User,
                        UserId = ctx.UserId,
                        UserLogin = ctx.UserLogin,
                        UserDisplayName = ctx.UserDisplayName,
                        UserInput = ctx.UserInput,
                        Cost = ctx.Cost,
                        Status = ctx.Status,
                        RedeemedAt = ctx.RedeemedAt,
                        ReplyAsync = ctx.ReplyAsync,
                        RefundAsync = ctx.RefundAsync,
                        FulfillAsync = ctx.FulfillAsync,
                        CancellationToken = ctx.CancellationToken,
                        DatabaseContext = _appDbContext,
                        ServiceProvider = _serviceProvider,
                        TwitchChatService = _twitchChatService,
                        TwitchApiService = _twitchApiService
                    };

                    await reward.Callback(scriptCtx);
                }
            };

            RewardScriptContext scriptCtx = new()
            {
                DatabaseContext = _appDbContext,
                ServiceProvider = _serviceProvider,
                TwitchChatService = _twitchChatService,
                TwitchApiService = _twitchApiService
            };

            await reward.Init(scriptCtx);

            _rewardService.RegisterReward(twitchReward);

            _logger.LogInformation("Loaded reward script: {RewardName}", rewardName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load reward script: {FilePath}", filePath);
        }
    }
}