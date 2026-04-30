using Newtonsoft.Json;

namespace NoMercyBot.Services.Twitch.Dto;

/// <summary>
/// Wire-format reply from GET /helix/channels/ads.
/// All numeric fields come back as strings in the JSON.
/// </summary>
public class AdScheduleResponse
{
    [JsonProperty("data")]
    public List<AdScheduleDto> Data { get; set; } = [];
}

public class AdScheduleDto
{
    [JsonProperty("next_ad_at")]
    public DateTime? NextAdAt { get; set; }

    [JsonProperty("last_ad_at")]
    public DateTime? LastAdAt { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }

    [JsonProperty("preroll_free_time")]
    public int PrerollFreeTime { get; set; }

    [JsonProperty("snooze_count")]
    public int SnoozeCount { get; set; }

    [JsonProperty("snooze_refresh_at")]
    public DateTime? SnoozeRefreshAt { get; set; }
}

/// <summary>
/// Convenient view of the schedule for downstream consumers.
/// Computes time-to-next-ad on the fly so callers don't have to.
/// </summary>
public class AdSchedule
{
    public DateTime? NextAdAt { get; init; }
    public DateTime? LastAdAt { get; init; }
    public int DurationSeconds { get; init; }
    public int PrerollFreeTimeSeconds { get; init; }
    public int SnoozeCount { get; init; }
    public DateTime? SnoozeRefreshAt { get; init; }

    public TimeSpan? TimeUntilNextAd => NextAdAt.HasValue ? NextAdAt.Value - DateTime.UtcNow : null;

    public bool HasUpcomingAd => NextAdAt.HasValue && NextAdAt.Value > DateTime.UtcNow;
}
