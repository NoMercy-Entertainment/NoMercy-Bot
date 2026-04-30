// Watch streak handler.
// Receives data from channel.chat.notification (notice_type: "watch_streak")
// dispatched by ChatEventHandler. The IRC client previously used to capture
// USERNOTICE viewermilestone events has been removed — Twitch now exposes
// watch streaks through EventSub directly.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.NewtonSoftConverters;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Widgets;

namespace NoMercyBot.Services.Twitch;

public class WatchStreakRecord
{
    [JsonProperty("streak")]
    public int Streak { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("channel_points_reward")]
    public int ChannelPointsReward { get; set; }
}

public class WatchStreakService
{
    private readonly ILogger<WatchStreakService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TwitchChatService _twitchChatService;
    private readonly TtsService _ttsService;

    private const string RECORD_TYPE = "WatchStreak";
    private const string BOT_VOICE = "en-US-GuyNeural";

    // Snarky streak celebration messages - bot voice
    private static readonly string[] _streakMessages =
    {
        "{name} has watched {streak} streams in a row! That's either dedication or a cry for help. Either way, we appreciate it.",
        "{streak} stream watch streak for {name}! At this point, {name} is basically furniture in this channel.",
        "Alert! {name} has been here for {streak} consecutive streams. Someone check if they're okay.",
        "{name} just hit a {streak} stream watch streak. That's more commitment than most people put into their jobs.",
        "Congratulations {name}! {streak} streams in a row! Your touch grass counter has been reset to zero.",
        "{name} has achieved a {streak} stream watch streak! Their monitor has permanent burn-in of this channel.",
        "Breaking news! {name} has watched {streak} streams straight. The Big Bird is honored. And slightly concerned.",
        "{streak} consecutive streams for {name}! At this point they should be on the payroll.",
        "{name} has now watched {streak} streams in a row. HR has been notified.",
        "{streak} streams deep, {name}. We can stop. We have to stop. We won't.",
        "{name} just hit {streak} streams. The Twitch algorithm is calling it stalking.",
        "Local viewer {name} now has a {streak} stream streak. Doctors hate this one trick.",
        "{name} clocked in for the {streak}th consecutive stream. Time-and-a-half kicks in next week.",
        "{streak} streams without a miss. {name}, your commitment is unmatched, your weekends concerning.",
        "{name} hit {streak} in a row. Even the IRS thinks you're a dependent of this channel.",
        "Streak update: {name} is at {streak}. The Big Bird has filed a noise complaint with reality.",
        "{name} has been here {streak} streams running. Spotify Wrapped is going to be embarrassing.",
        "{streak} streams for {name}. We are now legally each other's emergency contact.",
        "{name} just renewed for {streak} streams. The lease is now month to month, which is to say, every stream.",
        "{streak} streams in a row from {name}. The chair has formed an indent. Take that as you will.",
        "{name} is on a {streak} stream streak. Twitch sent flowers.",
        "Day {streak} of {name} not missing a stream. Day 1 of us pretending this is normal.",
        "{name} hit {streak} consecutive streams. Their browser bookmarked this channel out of habit.",
        "{streak} streams. {name} could have learned a language by now. Instead, they learned my chat commands.",
    };

    // Messages when someone shares a streak with a custom message
    private static readonly string[] _streakWithMessageTemplates =
    {
        "{name} hit a {streak} stream streak and had this to say:",
        "{name} reached {streak} consecutive streams and dropped this message:",
        "After {streak} streams in a row, {name} felt compelled to share:",
        "{streak} stream streak! {name} has a statement for the chat:",
        "{name} took the mic at {streak} consecutive streams to say:",
        "Live, from {streak} streams deep, {name} delivers a personal message:",
        "{name} hit {streak} streams and immediately gave a press conference:",
        "Following the {streak} stream milestone, {name} graced us with the following:",
        "{name} is {streak} streams in and apparently has notes:",
        "{streak} streams! {name} prepared remarks. They are as follows:",
    };

    // Messages when someone's streak is lower than their record (they lost it and rebuilt)
    private static readonly string[] _rebuiltStreakMessages =
    {
        "{name} is back with a {streak} stream streak! But we remember when it was {record}. The comeback arc has begun.",
        "Look who's rebuilding! {name} had a {record} streak, lost it, and is crawling back at {streak}. Respect the grind.",
        "{name} used to have a legendary {record} stream streak. Now it's {streak}. We don't talk about what happened in between.",
        "The phoenix rises! {name} had a {record} stream record but is back from the ashes with {streak}. Never forget.",
        "{name} is back at {streak}, but the ghost of that {record} streak still haunts this chat.",
        "{streak} streaks for {name}. We all remember the glorious {record}-streak era. Better days. Sadder times.",
        "{name} grinding back from the abyss. {record} once. {streak} now. Recovery is non-linear.",
        "{streak}? Cute, {name}. We saw you peak at {record}. We saw it all.",
        "{name} has been demoted from {record} streams to {streak}. The stockholders are nervous.",
        "Rebuild watch: {name} is {streak} streams in, climbing back toward the legendary {record} mark. Pace yourself.",
    };

    // Messages when someone beats their own record
    private static readonly string[] _newRecordMessages =
    {
        "{name} just set a NEW personal record! {streak} consecutive streams! The previous record of {record} has been demolished!",
        "HISTORY HAS BEEN MADE! {name} broke their own record of {record} streams with {streak}! Someone give this person a trophy!",
        "{name} is on a RAMPAGE! {streak} streams straight, smashing their old record of {record}! The Big Bird salutes you!",
        "New high score! {name} went from {record} to {streak} consecutive streams! The dedication is frankly alarming!",
        "{name} just shattered their {record} ceiling and landed on {streak}. Speedrunners are taking notes.",
        "New PR for {name}: {streak} streams. The previous record of {record} has been retired with honors.",
        "{name} unlocked the {streak}-streak achievement. Reward: more streams. Punishment: also more streams.",
        "{record} was the floor, {streak} is the new ceiling. {name} keeps moving the goalposts on themselves.",
        "RECORD BROKEN! {name} blew past {record} and is now at {streak}. Their other hobbies have filed for divorce.",
        "{name} just promoted from {record} streams to {streak}. Salary stays the same. The grind continues.",
    };

    public WatchStreakService(
        ILogger<WatchStreakService> logger,
        IServiceScopeFactory scopeFactory,
        TwitchChatService twitchChatService,
        TtsService ttsService
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _twitchChatService = twitchChatService;
        _ttsService = ttsService;
    }

    public async Task HandleWatchStreak(
        string userId,
        string displayName,
        string channel,
        int streak,
        int channelPointsReward,
        string? customMessage
    )
    {
        _logger.LogInformation(
            "Watch streak: {User} has {Streak} consecutive streams (reward: {Points}cp, message: {Message})",
            displayName,
            streak,
            channelPointsReward,
            customMessage ?? "(none)"
        );

        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get their highest recorded streak
        List<Record> existingRecords = await dbContext
            .Records.AsNoTracking()
            .Where(r => r.UserId == userId && r.RecordType == RECORD_TYPE)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        int highestStreak = 0;
        foreach (Record record in existingRecords)
        {
            WatchStreakRecord? data = record.Data.FromJson<WatchStreakRecord>();
            if (data != null && data.Streak > highestStreak)
                highestStreak = data.Streak;
        }

        // Store this streak
        WatchStreakRecord newRecord = new()
        {
            Streak = streak,
            Message = customMessage,
            ChannelPointsReward = channelPointsReward,
        };

        dbContext.Records.Add(
            new Record
            {
                UserId = userId,
                RecordType = RECORD_TYPE,
                Data = newRecord.ToJson(),
            }
        );
        await dbContext.SaveChangesAsync();

        // Pick the right response template
        string template;
        if (streak > highestStreak && highestStreak > 0)
        {
            // New personal record!
            template = _newRecordMessages[Random.Shared.Next(_newRecordMessages.Length)];
        }
        else if (highestStreak > streak)
        {
            // They had a higher streak before — they lost it and are rebuilding
            template = _rebuiltStreakMessages[Random.Shared.Next(_rebuiltStreakMessages.Length)];
        }
        else if (!string.IsNullOrWhiteSpace(customMessage))
        {
            // Has a custom message
            template = _streakWithMessageTemplates[
                Random.Shared.Next(_streakWithMessageTemplates.Length)
            ];
        }
        else
        {
            // Standard streak celebration
            template = _streakMessages[Random.Shared.Next(_streakMessages.Length)];
        }

        string text = template
            .Replace("{name}", displayName)
            .Replace("{streak}", streak.ToString())
            .Replace("{record}", highestStreak.ToString());

        // If they had a custom message, append it
        if (
            !string.IsNullOrWhiteSpace(customMessage)
            && !template.Contains("had this to say")
            && !template.Contains("dropped this message")
            && !template.Contains("felt compelled")
            && !template.Contains("has a statement")
        )
        {
            text += $" They also said: \"{customMessage}\"";
        }

        // Send chat message
        await _twitchChatService.SendMessageAsBot(channel, text);

        // Dual-voice TTS: bot announces, then reads their custom message if present
        string processedText = await _ttsService.ApplyUsernamePronunciationsAsync(text);

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            string processedMessage = await _ttsService.ApplyUsernamePronunciationsAsync(
                customMessage
            );
            string? userVoice =
                await _ttsService.GetSpeakerIdForUserAsync(userId, CancellationToken.None)
                ?? "en-US-EmmaMultilingualNeural";

            string botPart = processedText.Replace($"\"{customMessage}\"", "");

            var segments = new List<(string text, string voiceId)>
            {
                TtsService.Segment(BOT_VOICE, botPart),
                TtsService.Segment(userVoice, processedMessage),
            };

            (string? audioBase64, int durationMs) = await _ttsService.SynthesizeMultiVoiceSsmlAsync(
                segments,
                CancellationToken.None
            );

            if (audioBase64 != null)
            {
                IWidgetEventService widgetEventService =
                    scope.ServiceProvider.GetRequiredService<IWidgetEventService>();
                await widgetEventService.PublishEventAsync(
                    "channel.chat.message.tts",
                    new
                    {
                        text,
                        user = new { id = userId },
                        audioBase64,
                        provider = "Edge",
                        cost = 0m,
                        characterCount = text.Length,
                        cached = false,
                    }
                );
            }
        }
        else
        {
            // Single voice - just bot announcing
            await _ttsService.SendCachedTts(processedText, userId, CancellationToken.None);
        }
    }
}
