using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Services.Twitch.Dto;
using NoMercyBot.Services.Widgets;

namespace NoMercyBot.Services.Twitch;

/// <summary>
/// Polls Twitch's ad-schedule endpoint while the stream is live and pushes
/// "ad coming up" notifications to widgets and chat. Twitch only fires
/// channel.ad_break.begin at the moment the ad starts; this fills in the
/// advance-warning gap by surfacing next_ad_at.
/// </summary>
public class AdScheduleService : IHostedService, IDisposable
{
    private readonly TwitchApiService _twitchApiService;
    private readonly TwitchChatService _twitchChatService;
    private readonly IWidgetEventService _widgetEventService;
    private readonly ILogger<AdScheduleService> _logger;

    private CancellationTokenSource? _cts;
    private Task? _pollTask;

    // Conservative cadence — Twitch's edge has been touchy about this account
    // and ad schedule rarely changes between polls.
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    // Warn thresholds — if we cross these between polls, fire a one-shot event.
    // (Order matters; we fire each one at most once per upcoming-ad slot.)
    private static readonly TimeSpan[] WarnThresholds =
    [
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(10),
    ];

    // Single chat warning, fired once per upcoming-ad slot at the same point Twitch's
    // own moderator popup appears (~3 min). Reset when next_ad_at changes (snooze, etc.)
    // so a snooze re-arms the warning naturally.
    private static readonly TimeSpan ChatWarnThreshold = TimeSpan.FromMinutes(3);

    // Track which thresholds have already fired for the current upcoming ad,
    // keyed by the NextAdAt timestamp. Cleared when next_ad_at changes.
    private DateTime? _trackedNextAdAt;
    private readonly HashSet<TimeSpan> _firedThresholds = [];
    private bool _firedChatWarn;

    public AdScheduleService(
        TwitchApiService twitchApiService,
        TwitchChatService twitchChatService,
        IWidgetEventService widgetEventService,
        ILogger<AdScheduleService> logger
    )
    {
        _twitchApiService = twitchApiService;
        _twitchChatService = twitchChatService;
        _widgetEventService = widgetEventService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AdScheduleService starting (poll interval: {Seconds}s)",
            PollInterval.TotalSeconds
        );
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollTask = Task.Run(() => PollLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AdScheduleService stopping");
        if (_cts != null)
            await _cts.CancelAsync();

        if (_pollTask != null)
        {
            try
            {
                await _pollTask;
            }
            catch (OperationCanceledException) { }
        }
    }

    private async Task PollLoopAsync(CancellationToken token)
    {
        // Defer the first poll briefly so auth is fully settled on bot start.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!token.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdScheduleService poll failed");
            }

            try
            {
                await Task.Delay(PollInterval, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken token)
    {
        string? broadcasterId = _twitchApiService.Service?.UserId;
        if (string.IsNullOrEmpty(broadcasterId))
            return;

        AdSchedule? schedule = await _twitchApiService.GetAdSchedule(broadcasterId);
        if (schedule == null)
            return;

        // Always publish the latest schedule for any widget that wants it.
        await _widgetEventService.PublishEventAsync(
            "twitch.ad.schedule",
            new
            {
                next_ad_at = schedule.NextAdAt,
                last_ad_at = schedule.LastAdAt,
                duration_seconds = schedule.DurationSeconds,
                preroll_free_time_seconds = schedule.PrerollFreeTimeSeconds,
                snooze_count = schedule.SnoozeCount,
                snooze_refresh_at = schedule.SnoozeRefreshAt,
                time_until_next_ad_seconds = (int?)schedule.TimeUntilNextAd?.TotalSeconds,
            }
        );

        // Reset threshold tracking when the next-ad slot changes (snooze, ad ran, etc.).
        if (_trackedNextAdAt != schedule.NextAdAt)
        {
            _trackedNextAdAt = schedule.NextAdAt;
            _firedThresholds.Clear();
            _firedChatWarn = false;
        }

        if (!schedule.HasUpcomingAd || schedule.TimeUntilNextAd is not { } timeLeft)
            return;

        foreach (TimeSpan threshold in WarnThresholds)
        {
            if (timeLeft <= threshold && !_firedThresholds.Contains(threshold))
            {
                _firedThresholds.Add(threshold);
                await _widgetEventService.PublishEventAsync(
                    "twitch.ad.upcoming",
                    new
                    {
                        seconds_until_ad = (int)timeLeft.TotalSeconds,
                        threshold_seconds = (int)threshold.TotalSeconds,
                        duration_seconds = schedule.DurationSeconds,
                        next_ad_at = schedule.NextAdAt,
                    }
                );
                _logger.LogInformation(
                    "Ad coming up in {Seconds}s (threshold {Threshold}s, duration {Duration}s)",
                    (int)timeLeft.TotalSeconds,
                    (int)threshold.TotalSeconds,
                    schedule.DurationSeconds
                );
            }
        }

        // Single chat warning per upcoming-ad slot. Fires at the ~3min mark to mirror
        // Twitch's own moderator popup, then stays silent until the next slot.
        if (!_firedChatWarn && timeLeft <= ChatWarnThreshold && _twitchChatService.IsReady)
        {
            _firedChatWarn = true;
            try
            {
                string broadcasterLogin = TwitchConfig.Service().UserName ?? string.Empty;
                if (!string.IsNullOrEmpty(broadcasterLogin))
                {
                    int minutes = (int)Math.Round(timeLeft.TotalMinutes);
                    string when =
                        minutes >= 1
                            ? $"in ~{minutes} minute{(minutes == 1 ? "" : "s")}"
                            : $"in ~{(int)timeLeft.TotalSeconds} seconds";
                    await _twitchChatService.SendMessageAsBot(
                        broadcasterLogin,
                        $"Heads up — ad break {when} ({schedule.DurationSeconds}s long). Subscribers skip ads. 💜"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ad-break chat warning");
            }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
