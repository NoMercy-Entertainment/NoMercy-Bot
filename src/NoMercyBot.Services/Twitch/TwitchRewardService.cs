using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Other;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch;

public enum RewardPermission
{
    Broadcaster,
    Moderator,
    Vip,
    Subscriber,
    Everyone
}

public class RewardContext
{
    public Channel Channel { get; set; } = null!;
    public User User { get; set; } = null!;
    public User Broadcaster { get; set; } = null!;
    public string ChannelId { get; set; } = null!;
    public string BroadcasterId { get; set; } = null!;
    public Guid RewardId { get; set; }
    public string RewardTitle { get; set; } = null!;
    public string RedemptionId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string UserLogin { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserInput { get; set; }
    public int Cost { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset RedeemedAt { get; set; }
    public Func<string, Task> ReplyAsync { get; set; } = null!;
    public Func<Task> RefundAsync { get; set; } = null!;
    public Func<Task> FulfillAsync { get; set; } = null!;
    public required AppDbContext DatabaseContext { get; init; } = null!;
    public TwitchRewardService RewardService { get; set; } = null!;
    public TwitchApiService TwitchApiService { get; set; } = null!;
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public CancellationToken CancellationToken { get; set; }
}

public class TwitchReward
{
    public Guid RewardId { get; set; }
    public string? RewardTitle { get; set; }
    public RewardPermission Permission { get; set; } = RewardPermission.Everyone;
    public object? Storage { get; set; }
    public Func<RewardContext, Task> Callback { get; set; } = null!;
}

public class TwitchRewardService
{
    private static readonly ConcurrentDictionary<string, TwitchReward> RewardsByTitle = new();
    private static readonly ConcurrentDictionary<Guid, TwitchReward> RewardsById = new();
    private readonly ILogger<TwitchRewardService> _logger;
    private readonly AppDbContext _appDbContext;
    private readonly TwitchChatService _twitchChatService;
    private readonly TwitchApiService _twitchApiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly PermissionService _permissionService;

    public TwitchRewardService(
        AppDbContext appDbContext,
        TwitchChatService twitchChatService,
        TwitchApiService twitchApiService,
        PermissionService permissionService,
        IServiceScopeFactory scopeFactory,
        ILogger<TwitchRewardService> logger)
    {
        _logger = logger;
        _appDbContext = appDbContext;
        _twitchChatService = twitchChatService;
        _twitchApiService = twitchApiService;
        IServiceScope scope = scopeFactory.CreateScope();
        _serviceProvider = scope.ServiceProvider;
        _permissionService = permissionService;

        LoadRewardsFromDatabase();
    }

    private void LoadRewardsFromDatabase()
    {
        List<Reward> dbRewards = _appDbContext.Rewards.Where(r => r.IsEnabled).ToList();
        foreach (Reward dbReward in dbRewards)
            RegisterReward(new()
            {
                RewardId = dbReward.Id,
                RewardTitle = dbReward.Title,
                Permission = Enum.TryParse(dbReward.Permission, true, out RewardPermission perm)
                    ? perm
                    : RewardPermission.Everyone,
                Callback = async ctx => await ctx.ReplyAsync(dbReward.Response)
            });
    }

    public bool RegisterReward(TwitchReward reward)
    {
        if (reward.RewardId != Guid.Empty) RewardsById.TryAdd(reward.RewardId, reward);

        if (!string.IsNullOrEmpty(reward.RewardTitle))
            RewardsByTitle.TryAdd(reward.RewardTitle.ToLowerInvariant(), reward);

        _logger.LogInformation("Registered/Updated reward: {RewardTitle} (ID: {RewardId})",
            reward.RewardTitle ?? "Unknown", reward.RewardId);
        return true;
    }

    private bool RemoveReward(string identifier)
    {
        bool removedById = false;
        bool removedByTitle = RewardsByTitle.TryRemove(identifier.ToLowerInvariant(), out _);

        // Try to parse as Guid for ID removal
        if (Guid.TryParse(identifier, out Guid guidId)) removedById = RewardsById.TryRemove(guidId, out _);

        return removedById || removedByTitle;
    }

    public bool UpdateReward(TwitchReward reward)
    {
        if (reward.RewardId != Guid.Empty) RewardsById[reward.RewardId] = reward;

        if (!string.IsNullOrEmpty(reward.RewardTitle)) RewardsByTitle[reward.RewardTitle.ToLowerInvariant()] = reward;

        return true;
    }

    public IEnumerable<TwitchReward> ListRewards()
    {
        return RewardsById.Values.Concat(RewardsByTitle.Values).Distinct();
    }

    public async Task ExecuteReward(ChannelPointsCustomRewardRedemptionArgs args)
    {
        string twitchRewardId = args.Notification.Payload.Event.Reward.Id;
        string twitchRedeemId = args.Notification.Payload.Event.Id;
        string rewardTitle = args.Notification.Payload.Event.Reward.Title;
        string broadcasterId = args.Notification.Payload.Event.BroadcasterUserId;
        string broadcasterLogin = args.Notification.Payload.Event.BroadcasterUserLogin;

        // Try to find reward by converting Twitch string ID to Guid for database lookup
        TwitchReward? reward = null;

        // First try to find by Twitch reward ID converted to Guid
        if (Guid.TryParse(twitchRewardId, out Guid rewardGuid)) RewardsById.TryGetValue(rewardGuid, out reward);

        // Fallback to title lookup
        if (reward == null) RewardsByTitle.TryGetValue(rewardTitle.ToLowerInvariant(), out reward);

        if (reward != null)
        {
            // Check permissions
            User? user = _appDbContext.Users.FirstOrDefault(u => u.Id == args.Notification.Payload.Event.UserId);
            user ??= await _twitchApiService.FetchUser(id: args.Notification.Payload.Event.UserId);

            User? broadcaster = _appDbContext.Users.FirstOrDefault(u => u.Id == broadcasterId);
            broadcaster ??= await _twitchApiService.FetchUser(id: broadcasterId);

            Channel? channel = _appDbContext.Channels.FirstOrDefault(c => c.Id == broadcasterId);

            string userType = DetermineUserType(user, broadcasterId);

            if (!_permissionService.HasMinLevel(userType, reward.Permission.ToString().ToLowerInvariant()))
            {
                _logger.LogWarning("User {User} lacks permission {RequiredPermission} for reward {RewardTitle}",
                    args.Notification.Payload.Event.UserLogin, reward.Permission, rewardTitle);

                // Refund the points by updating redemption status to CANCELED
                await _twitchApiService.UpdateRedemptionStatus(broadcasterId, twitchRewardId, twitchRedeemId,
                    "CANCELED");

                await _twitchChatService.SendMessageAsBot(
                    broadcasterLogin,
                    $"@{args.Notification.Payload.Event.UserName}, you don't have permission to use this reward. Your points have been refunded.");

                return;
            }

            RewardContext context = new()
            {
                Channel = channel,
                User = user,
                Broadcaster = broadcaster,
                BroadcasterId = broadcasterId,
                RewardId = reward.RewardId, // Use the Guid from our reward
                RewardTitle = rewardTitle,
                RedemptionId = args.Notification.Payload.Event.Id,
                UserId = args.Notification.Payload.Event.UserId,
                UserLogin = args.Notification.Payload.Event.UserLogin,
                UserDisplayName = args.Notification.Payload.Event.UserName,
                UserInput = args.Notification.Payload.Event.UserInput,
                Cost = args.Notification.Payload.Event.Reward.Cost,
                Status = args.Notification.Payload.Event.Status,
                RedeemedAt = args.Notification.Payload.Event.RedeemedAt,
                RewardService = this,
                ServiceProvider = _serviceProvider,
                TwitchApiService = _twitchApiService,
                DatabaseContext = _appDbContext,
                ReplyAsync = async (reply) =>
                {
                    await _twitchChatService.SendMessageAsBot(broadcasterLogin, reply);
                },
                RefundAsync = async () =>
                {
                    await _twitchApiService.UpdateRedemptionStatus(broadcasterId, twitchRewardId, twitchRedeemId,
                        "CANCELED");
                },
                FulfillAsync = async () =>
                {
                    await _twitchApiService.UpdateRedemptionStatus(broadcasterId, twitchRewardId, twitchRedeemId,
                        "FULFILLED");
                }
            };

            try
            {
                await reward.Callback(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing reward {RewardTitle} for user {User}",
                    rewardTitle, args.Notification.Payload.Event.UserLogin);

                // Refund on error
                await _twitchApiService.UpdateRedemptionStatus(broadcasterId, twitchRewardId, twitchRedeemId,
                    "CANCELED");

                await _twitchChatService.SendMessageAsBot(
                    broadcasterLogin,
                    $"@{args.Notification.Payload.Event.UserName}, there was an error processing your reward. Your points have been refunded.");
            }
        }
        else
        {
            _logger.LogDebug("No handler found for reward: {RewardTitle} (ID: {RewardId})", rewardTitle,
                twitchRewardId);
        }
    }

    private string DetermineUserType(User? user, string broadcasterId)
    {
        if (user?.Id == broadcasterId)
            return "broadcaster";

        // Add logic here to check for moderator, VIP, subscriber status
        // This would require additional database tables or API calls
        // For now, default to "everyone"
        return "everyone";
    }

    public async Task AddOrUpdateUserRewardAsync(Guid rewardId, string? rewardTitle, string response,
        string permission = "everyone", bool isEnabled = true, string? description = null)
    {
        Reward? dbReward = await _appDbContext.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId);
        if (dbReward == null)
        {
            dbReward = new()
            {
                Id = rewardId,
                Title = rewardTitle,
                Response = response,
                Permission = permission,
                IsEnabled = isEnabled,
                Description = description
            };
            await _appDbContext.Rewards.AddAsync(dbReward);
        }
        else
        {
            dbReward.Title = rewardTitle;
            dbReward.Response = response;
            dbReward.Permission = permission;
            dbReward.IsEnabled = isEnabled;
            dbReward.Description = description;
            _appDbContext.Rewards.Update(dbReward);
        }

        await _appDbContext.SaveChangesAsync();

        RegisterReward(new()
        {
            RewardId = dbReward.Id,
            RewardTitle = dbReward.Title,
            Permission = Enum.TryParse<RewardPermission>(dbReward.Permission, true, out RewardPermission perm)
                ? perm
                : RewardPermission.Everyone,
            Callback = async ctx => await ctx.ReplyAsync(dbReward.Response)
        });
    }

    public async Task<bool> RemoveUserRewardAsync(string identifier)
    {
        Guid? rewardId = Guid.TryParse(identifier, out Guid result) ? result : null;
        Reward? dbReward =
            await _appDbContext.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId || r.Title == identifier);
        if (dbReward == null) return false;

        _appDbContext.Rewards.Remove(dbReward);
        await _appDbContext.SaveChangesAsync();
        RemoveReward(identifier);
        return true;
    }
}