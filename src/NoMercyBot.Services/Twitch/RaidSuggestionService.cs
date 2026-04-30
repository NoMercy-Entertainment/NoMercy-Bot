using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Services.Twitch.Dto;

namespace NoMercyBot.Services.Twitch;

/// <summary>
/// Per-user raid history: how many times this party has been on the receiving end
/// of a raid (Count) and when the most recent one happened (LastAt). Used in both
/// directions (us→them and them→us) for ratio-aware reciprocity scoring.
/// </summary>
public record RaidStats(int Count, DateTime LastAt);

public record RaidCandidate(
    string UserId,
    string UserLogin,
    string UserName,
    string GameName,
    int ViewerCount,
    DateTime StartedAt,
    List<string> Tags,
    bool IsFollowed,
    DateTime? LastRaidedAt,
    DateTime? LastRaidedByAt,
    int Score,
    string Reason
);

/// <summary>
/// Builds a ranked list of raid targets from the broadcaster's followed channels
/// and the live "Software and Game Development" English category. Scoring favors
/// real-software streams over games, smaller channels over bigger ones, and
/// channels we haven't raided recently.
/// </summary>
public class RaidSuggestionService
{
    private readonly TwitchApiService _twitchApiService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RaidSuggestionService> _logger;

    private const string SoftwareCategoryName = "Software and Game Development";
    private const string ScienceCategoryName = "Science & Technology";

    private static readonly TimeSpan FollowedCacheLifetime = TimeSpan.FromHours(6);

    private List<FollowedChannelDto>? _cachedFollowed;
    private DateTime _followedCachedAt = DateTime.MinValue;
    private string? _cachedSoftwareGameId;
    private readonly SemaphoreSlim _followedLock = new(1, 1);

    public RaidSuggestionService(
        TwitchApiService twitchApiService,
        IServiceScopeFactory scopeFactory,
        ILogger<RaidSuggestionService> logger
    )
    {
        _twitchApiService = twitchApiService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    public async Task<List<RaidCandidate>> GetSuggestionsAsync(int max = 5)
    {
        string? broadcasterId = _twitchApiService.Service?.UserId;
        if (string.IsNullOrEmpty(broadcasterId))
            return [];

        Dictionary<string, (StreamInfo stream, bool followed)> live = await GetLiveCandidatesAsync(
            broadcasterId
        );
        if (live.Count == 0)
            return [];

        (var outgoing, var incoming) = await GetRaidHistoryAsync(broadcasterId);

        List<RaidCandidate> ranked = live
            .Values.Select(v => Score(v.stream, v.followed, outgoing, incoming))
            .OrderByDescending(c => c.Score)
            .ToList();

        LogRanked(ranked);
        return ranked.Take(max).ToList();
    }

    // ─── Live candidate set: followed channels + software-EN discovery ────────

    private async Task<
        Dictionary<string, (StreamInfo stream, bool followed)>
    > GetLiveCandidatesAsync(string broadcasterId)
    {
        List<FollowedChannelDto> followed = await GetFollowedAsync(broadcasterId);
        HashSet<string> followedIds = followed.Select(f => f.BroadcasterId).ToHashSet();

        List<StreamInfo> followedLive =
            followedIds.Count > 0 ? await _twitchApiService.GetStreamsByUserIds(followedIds) : [];

        List<StreamInfo> discovered = await GetDiscoveredAsync(broadcasterId, followedIds);

        Dictionary<string, (StreamInfo, bool)> byId = new();
        foreach (StreamInfo s in followedLive)
            byId[s.UserId] = (s, true);
        foreach (StreamInfo s in discovered)
            byId.TryAdd(s.UserId, (s, false));
        byId.Remove(broadcasterId);
        return byId;
    }

    private async Task<List<StreamInfo>> GetDiscoveredAsync(
        string broadcasterId,
        HashSet<string> followedIds
    )
    {
        string? softwareId = await GetSoftwareGameIdAsync();
        if (string.IsNullOrEmpty(softwareId))
            return [];

        List<StreamInfo> all = await _twitchApiService.GetStreamsByCategory(
            softwareId,
            language: "en",
            first: 100
        );
        return all.Where(s => !followedIds.Contains(s.UserId) && s.UserId != broadcasterId)
            .ToList();
    }

    // ─── Scoring ──────────────────────────────────────────────────────────────

    private static RaidCandidate Score(
        StreamInfo stream,
        bool isFollowed,
        Dictionary<string, RaidStats> outgoing,
        Dictionary<string, RaidStats> incoming
    )
    {
        List<(int delta, string label)> reasons = [];

        Add(reasons, ScoreCategory(stream));
        Add(reasons, ScoreContentSignal(stream));
        Add(reasons, ScoreViewerCount(stream.ViewerCount));

        outgoing.TryGetValue(stream.UserId, out RaidStats? outStats);
        DateTime? lastRaid = outStats?.LastAt;
        int outCount = outStats?.Count ?? 0;
        Add(reasons, ScoreRaidCooldown(lastRaid));

        if (isFollowed)
            Add(reasons, (50, "followed"));

        incoming.TryGetValue(stream.UserId, out RaidStats? inStats);
        DateTime? lastRaidedBy = inStats?.LastAt;
        int inCount = inStats?.Count ?? 0;
        Add(reasons, ScoreReciprocity(inCount, outCount));

        return new RaidCandidate(
            UserId: stream.UserId,
            UserLogin: stream.UserLogin,
            UserName: stream.UserName,
            GameName: stream.GameName,
            ViewerCount: stream.ViewerCount,
            StartedAt: stream.StartedAt,
            Tags: stream.Tags,
            IsFollowed: isFollowed,
            LastRaidedAt: lastRaid,
            LastRaidedByAt: lastRaidedBy,
            Score: reasons.Sum(t => t.delta),
            Reason: FormatReasons(reasons)
        );
    }

    private static void Add(List<(int, string)> reasons, (int delta, string label)? r)
    {
        if (r is { delta: not 0 } v)
            reasons.Add((v.delta, v.label));
    }

    private static string FormatReasons(List<(int delta, string label)> reasons) =>
        string.Join(", ", reasons.Select(t => $"{(t.delta >= 0 ? "+" : "")}{t.delta} {t.label}"));

    private static (int Delta, string Label) ScoreCategory(StreamInfo stream)
    {
        if (stream.GameName.Equals(SoftwareCategoryName, StringComparison.OrdinalIgnoreCase))
            return (60, "Software cat");
        if (stream.GameName.Equals(ScienceCategoryName, StringComparison.OrdinalIgnoreCase))
            return (40, "Sci&Tech cat");
        return (-20, $"non-dev cat ({stream.GameName})");
    }

    /// <summary>
    /// Gamedev signal wins over software signal — a streamer with both
    /// "programming" and "deckbuilder" is making a game, not a CRUD app.
    /// </summary>
    private static (int Delta, string Label)? ScoreContentSignal(StreamInfo stream)
    {
        if (HasMatch(stream, GameDevTagRegex))
            return (-40, "gamedev signal");
        if (HasMatch(stream, SoftwareTagRegex))
            return (35, "software signal");
        return null;
    }

    private static (int Delta, string Label) ScoreViewerCount(int v) =>
        v switch
        {
            >= 1 and <= 50 => (25, $"small ({v}v)"),
            <= 200 => (10, $"mid ({v}v)"),
            <= 500 => (-5, $"medium ({v}v)"),
            <= 1000 => (-15, $"big ({v}v)"),
            <= 5000 => (-30, $"large ({v}v)"),
            _ => (-50, $"giant ({v}v)"),
        };

    /// <summary>
    /// Bonus/penalty for outgoing raid recency. Recent raids get a hard cooldown
    /// penalty; old/never-raided gets a bonus.
    /// </summary>
    private static (int Delta, string Label) ScoreRaidCooldown(DateTime? lastRaid)
    {
        if (lastRaid is null)
            return (30, "never raided");

        TimeSpan ago = DateTime.UtcNow - lastRaid.Value;
        string when = ShortAgo(lastRaid);
        // <7d hard cooldown · 7-14d still recent · 14-30d neutral · 30d+ stale = due
        if (ago < TimeSpan.FromDays(7))
            return (-40, $"raided {when} ago (cooldown)");
        if (ago < TimeSpan.FromDays(14))
            return (-15, $"raided {when} ago (recent)");
        if (ago < TimeSpan.FromDays(30))
            return (5, $"raided {when} ago");
        return (25, $"raided {when} ago (stale)");
    }

    /// <summary>
    /// Ratio-aware reciprocity. If they've raided us more than we've raided them
    /// we owe them; the bigger the imbalance, the bigger the bump. This prevents
    /// a frequent inbound-raider from being permanently skipped because they're
    /// tagged gamedev — owing them N raids overrides.
    /// </summary>
    private static (int Delta, string Label)? ScoreReciprocity(int inCount, int outCount)
    {
        if (inCount == 0)
            return null;

        int imbalance = inCount - outCount;
        if (imbalance >= 1)
        {
            // Base big enough to clear the gamedev penalty (-40); +12 per extra owed.
            int bump = 55 + (imbalance - 1) * 12;
            string raidWord = imbalance == 1 ? "raid" : "raids";
            return (bump, $"owed {imbalance} {raidWord} (in:{inCount}/out:{outCount})");
        }
        if (imbalance == 0)
        {
            return (10, $"reciprocal (in:{inCount}/out:{outCount})");
        }
        // imbalance < 0: we've raided them more than they've raided us — no bump.
        return null;
    }

    // ─── Tag/title pattern matching ───────────────────────────────────────────

    /// <summary>
    /// Software-dev signals — tags / title tokens that indicate real software/web/dev
    /// work (not game development). Languages commonly used for game dev (csharp,
    /// cpp, java, lua, javascript, rust) are deliberately excluded as ambiguous.
    /// </summary>
    private static readonly Regex SoftwareTagRegex = new(
        @"^(programming|softwaredev(elopment)?|softwareengineer(ing)?|webdev(elopment)?|"
            + @"coding|code|developer|backend|frontend|fullstack(dev)?|devops|sre|"
            + @"systemsprogramming|datascience|machinelearning|"
            // Languages with low game-dev overlap
            + @"python|typescript|ruby|php|golang|elixir|erlang|haskell|scala|clojure|"
            + @"kotlin|swift|dart|flutter|elm|fsharp|ocaml|julia|perl|crystal|nim|zig|"
            + @"r(language)?|"
            // Web frameworks
            + @"react(js)?|vue(js)?|angular|svelte(kit)?|next(js)?|nuxt(js)?|astro|remix|"
            + @"django|flask|fastapi|rails|laravel|symfony|spring(boot)?|aspnetcore|"
            + @"nodejs|node|deno|bun|express(js)?|"
            // Markup, styling, frontend tooling
            + @"html5?|css3?|scss|sass|less|tailwind(css)?|bootstrap|jquery|webpack|vite|"
            // Databases
            + @"postgres(ql)?|mysql|mariadb|mongodb|redis|elasticsearch|sqlite|cassandra|dynamodb|"
            // Infra / cloud
            + @"kubernetes|k8s|docker|terraform|ansible|helm|nginx|apache|"
            + @"aws|azure|gcp|cloudflare|digitalocean|heroku|vercel|netlify|"
            + @"prometheus|grafana|kafka|rabbitmq|"
            // Shells / editors / tools
            + @"bash|zsh|fish|powershell|emacs|neovim|vim|git|"
            // Architecture / API
            + @"microservices|graphql|restapi|api|grpc|websockets)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// Gamedev signals — explicit tags + game engines + genre tags. Genre presence
    /// wins over "programming" (a streamer with deckbuilder + godot is making a game).
    /// </summary>
    private static readonly Regex GameDevTagRegex = new(
        @"^(gamedev(elopment)?|indie(game)?dev|gamejam|"
            // Engines / game-specific tooling
            + @"godot|unity|unrealengine|unreal|gamemaker(studio)?|construct(2|3)?|defold|lovd2|pygame|phaser|raylib|monogame|pixijs|playcanvas|"
            // Genres
            + @"roguelike|roguelite|deckbuilder|cardgame|metroidvania|platformer|shmup|bullethell|fightinggame|"
            + @"jrpg|arpg|crpg|wrpg|rpgmaker|"
            + @"survivalgame|horrorgame|puzzlegame|racinggame|simulationgame|tycoon|sandboxgame|"
            + @"fps|rts|tps|moba|mmo(rpg)?|battleroyale)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex TitleTokenSplit = new(@"[^a-zA-Z0-9+#]+", RegexOptions.Compiled);

    /// <summary>
    /// Tests whether any tag OR any token from the title matches the pattern.
    /// "Python REST API" tokenizes to [python, rest, api] and each is tested.
    /// </summary>
    private static bool HasMatch(StreamInfo stream, Regex pattern)
    {
        foreach (string t in stream.Tags)
            if (pattern.IsMatch(t))
                return true;

        foreach (string token in TokenizeTitle(stream.Title))
            if (pattern.IsMatch(token))
                return true;

        return false;
    }

    private static IEnumerable<string> TokenizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            yield break;
        foreach (string token in TitleTokenSplit.Split(title))
            if (!string.IsNullOrEmpty(token))
                yield return token;
    }

    // ─── Caches ───────────────────────────────────────────────────────────────

    private async Task<List<FollowedChannelDto>> GetFollowedAsync(string broadcasterId)
    {
        if (FollowedCacheValid())
            return _cachedFollowed!;

        await _followedLock.WaitAsync();
        try
        {
            if (FollowedCacheValid())
                return _cachedFollowed!;

            _cachedFollowed = await _twitchApiService.GetFollowedChannels(broadcasterId);
            _followedCachedAt = DateTime.UtcNow;
            _logger.LogInformation(
                "Refreshed followed-channels cache: {Count} channels",
                _cachedFollowed.Count
            );
            return _cachedFollowed;
        }
        finally
        {
            _followedLock.Release();
        }
    }

    private bool FollowedCacheValid() =>
        _cachedFollowed != null && DateTime.UtcNow - _followedCachedAt < FollowedCacheLifetime;

    private async Task<string?> GetSoftwareGameIdAsync()
    {
        if (!string.IsNullOrEmpty(_cachedSoftwareGameId))
            return _cachedSoftwareGameId;

        GameData? game = await _twitchApiService.GetGameByName(SoftwareCategoryName);
        if (game == null)
        {
            _logger.LogWarning("Could not resolve game id for '{Category}'", SoftwareCategoryName);
            return null;
        }

        _cachedSoftwareGameId = game.Id;
        _logger.LogInformation(
            "Resolved '{Category}' to game_id {Id}",
            SoftwareCategoryName,
            game.Id
        );
        return _cachedSoftwareGameId;
    }

    // ─── Raid history (DB) ────────────────────────────────────────────────────

    /// <summary>
    /// Reads raid history from ChannelEvents in both directions, with counts and
    /// latest timestamps per remote user. Outgoing (us → them): UserId=broadcaster.
    /// Incoming (them → us): ChannelId=broadcaster, UserId=raider.
    /// No date cutoff — count-based scoring needs the full picture.
    /// </summary>
    private async Task<(
        Dictionary<string, RaidStats> outgoing,
        Dictionary<string, RaidStats> incoming
    )> GetRaidHistoryAsync(string broadcasterId)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Dictionary<string, RaidStats> outgoing = await GroupRaidStats(
            db,
            e => e.Type == "channel.raid" && e.UserId == broadcasterId && e.ChannelId != null,
            e => e.ChannelId!
        );

        Dictionary<string, RaidStats> incoming = await GroupRaidStats(
            db,
            e =>
                e.Type == "channel.raid"
                && e.ChannelId == broadcasterId
                && e.UserId != null
                && e.UserId != broadcasterId,
            e => e.UserId!
        );

        return (outgoing, incoming);
    }

    private static async Task<Dictionary<string, RaidStats>> GroupRaidStats(
        AppDbContext db,
        System.Linq.Expressions.Expression<Func<Database.Models.ChannelEvent, bool>> predicate,
        System.Linq.Expressions.Expression<Func<Database.Models.ChannelEvent, string>> keySelector
    )
    {
        var rows = await db
            .ChannelEvents.AsNoTracking()
            .Where(predicate)
            .GroupBy(keySelector)
            .Select(g => new
            {
                Key = g.Key,
                Count = g.Count(),
                LastRaid = g.Max(e => e.CreatedAt),
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.Key, r => new RaidStats(r.Count, r.LastRaid));
    }

    // ─── Log table ────────────────────────────────────────────────────────────

    private void LogRanked(List<RaidCandidate> ranked)
    {
        string header =
            $"{Ansi.Bold}   #   score  src  user                            v   out        in         why{Ansi.Reset}";

        string rows = string.Join("\n", ranked.Select(FormatRow));

        _logger.LogInformation(
            "Raid suggestions ({TotalLive} live candidates):\n{Header}\n{Ranked}",
            ranked.Count,
            header,
            rows
        );
    }

    private static string FormatRow(RaidCandidate c, int i) =>
        $"  {Ansi.Gray}{i + 1, 2}.{Ansi.Reset} {Ansi.Score(c.Score)}   "
        + $"{Ansi.Src(c.IsFollowed)}   "
        + $"{Ansi.Bold}{Truncate(c.UserLogin, 30), -30}{Ansi.Reset}  "
        + $"{Ansi.Viewers(c.ViewerCount)}  "
        + $"{Ansi.OutAge(c.LastRaidedAt)}  "
        + $"{Ansi.InAge(c.LastRaidedByAt)}  "
        + $"{Ansi.Dim}{c.Reason}{Ansi.Reset}";

    // ─── Formatters ───────────────────────────────────────────────────────────

    /// <summary>Compact "ago" formatter: "3mo", "2w", "5d", "today", "never".</summary>
    private static string ShortAgo(DateTime? dt)
    {
        if (dt is null)
            return "never";
        TimeSpan ago = DateTime.UtcNow - dt.Value;
        if (ago.TotalDays >= 30)
            return $"{(int)(ago.TotalDays / 30)}mo";
        if (ago.TotalDays >= 7)
            return $"{(int)(ago.TotalDays / 7)}w";
        if (ago.TotalDays >= 1)
            return $"{(int)ago.TotalDays}d";
        return "today";
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    // ─── ANSI colours for the debug log table ─────────────────────────────────

    private static class Ansi
    {
        public const string Reset = "\x1b[0m";
        public const string Bold = "\x1b[1m";
        public const string Dim = "\x1b[2m";
        public const string Red = "\x1b[31m";
        public const string Green = "\x1b[32m";
        public const string Yellow = "\x1b[33m";
        public const string Cyan = "\x1b[36m";
        public const string Gray = "\x1b[90m";
        public const string BrightGreen = "\x1b[92m";

        public static string Score(int score)
        {
            string text = $"{score, 5}";
            if (score >= 180)
                return $"{Bold}{BrightGreen}{text}{Reset}";
            if (score >= 130)
                return $"{Green}{text}{Reset}";
            if (score >= 80)
                return $"{Yellow}{text}{Reset}";
            if (score >= 0)
                return $"{Gray}{text}{Reset}";
            return $"{Red}{text}{Reset}";
        }

        public static string Src(bool followed) =>
            followed ? $"{Bold}{Cyan}F{Reset}" : $"{Gray}N{Reset}";

        /// <summary>Outgoing raid age. Red if recent (cooldown), gray if old/never.</summary>
        public static string OutAge(DateTime? dt)
        {
            string text = ShortAgo(dt);
            if (dt is null)
                return $"{Gray}{text, -9}{Reset}";
            TimeSpan ago = DateTime.UtcNow - dt.Value;
            if (ago.TotalDays < 7)
                return $"{Red}{text, -9}{Reset}";
            if (ago.TotalDays < 30)
                return $"{Yellow}{text, -9}{Reset}";
            return $"{Gray}{text, -9}{Reset}";
        }

        /// <summary>Incoming raid age. Cyan when set (reciprocity matters), dim when not.</summary>
        public static string InAge(DateTime? dt)
        {
            string text = ShortAgo(dt);
            return dt is null ? $"{Gray}{text, -9}{Reset}" : $"{Bold}{Cyan}{text, -9}{Reset}";
        }

        public static string Viewers(int v)
        {
            string text = $"{v, 4}";
            if (v >= 1 && v <= 50)
                return $"{Green}{text}{Reset}";
            if (v <= 200)
                return text;
            if (v <= 1000)
                return $"{Yellow}{text}{Reset}";
            return $"{Red}{text}{Reset}";
        }
    }
}
