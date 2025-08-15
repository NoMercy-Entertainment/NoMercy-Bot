using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Scripting;

namespace NoMercyBot.Services.Interfaces;

public interface IReward
{
    Guid RewardId { get; }
    string RewardTitle { get; }
    RewardPermission Permission { get; }
    Task Init(RewardScriptContext context);
    Task Callback(RewardScriptContext context);
}
