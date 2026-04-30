using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Client;

namespace NoMercyBot.Services.Twitch;

/// <summary>
/// Reflection-based shim around TwitchLib's internals to capture the HTTP response
/// details on a failed EventSub WebSocket upgrade — specifically the 429
/// Retry-After / Ratelimit-Reset headers that TwitchLib's ClientWebSocket usage
/// silently discards.
///
/// Strategy: before each connect attempt, replace the inner ClientWebSocket on
/// TwitchLib's WebsocketClient with one that has CollectHttpResponseDetails=true.
/// After the attempt, read HttpStatusCode + HttpResponseHeaders from that socket.
///
/// This is a temporary patch until we replace the library entirely. It's brittle
/// to library updates (private field renames will silently break it) — every
/// reflection step has a defensive fallback so the bot keeps working with the
/// existing heuristic backoff if anything goes wrong.
/// </summary>
public class RateLimitProbe
{
    private readonly ILogger<RateLimitProbe> _logger;
    private readonly EventSubWebsocketClient _eventSubClient;

    private readonly FieldInfo? _websocketClientField;
    private readonly FieldInfo? _webSocketField;
    private readonly bool _reflectionOk;

    public HttpStatusCode? LastStatusCode { get; private set; }
    public IReadOnlyDictionary<string, IEnumerable<string>>? LastResponseHeaders
    {
        get;
        private set;
    }

    public RateLimitProbe(EventSubWebsocketClient eventSubClient, ILogger<RateLimitProbe> logger)
    {
        _eventSubClient = eventSubClient;
        _logger = logger;

        _websocketClientField = typeof(EventSubWebsocketClient).GetField(
            "_websocketClient",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        _webSocketField = typeof(WebsocketClient).GetField(
            "_webSocket",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        _reflectionOk = _websocketClientField != null && _webSocketField != null;
        if (!_reflectionOk)
        {
            _logger.LogWarning(
                "RateLimitProbe: TwitchLib internals not as expected "
                    + "(_websocketClient or _webSocket field missing). "
                    + "Bot will fall back to heuristic backoff for WS 429s."
            );
        }
    }

    /// <summary>
    /// Replaces the inner _webSocket on the current WebsocketClient with a fresh
    /// ClientWebSocket that has CollectHttpResponseDetails=true. Call BEFORE each
    /// connect attempt — including retries — because TwitchLib's WebsocketClient
    /// internally does `_webSocket = new()` whenever its previous one is in Closed
    /// state, wiping any flag we set.
    /// </summary>
    public bool ArmForNextConnect()
    {
        if (!_reflectionOk)
            return false;

        try
        {
            object? inner = _websocketClientField!.GetValue(_eventSubClient);
            if (inner == null)
                return false;

            // Dispose the previous _webSocket (which may have an open or
            // half-open TCP socket from a prior failed attempt) before
            // swapping in the freshly armed one.
            ClientWebSocket? previous = _webSocketField!.GetValue(inner) as ClientWebSocket;

            ClientWebSocket fresh = new();
            fresh.Options.CollectHttpResponseDetails = true;
            _webSocketField!.SetValue(inner, fresh);

            try
            {
                previous?.Dispose();
            }
            catch
            { /* best-effort */
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RateLimitProbe.ArmForNextConnect failed");
            return false;
        }
    }

    /// <summary>
    /// Snapshots the response status + headers from whichever ClientWebSocket is
    /// currently inside EventSubWebsocketClient.WebsocketClient. The DI factory
    /// pre-arms the first instance; ArmForNextConnect re-arms before retries.
    ///
    /// Call AFTER a connect attempt (success or failure).
    /// </summary>
    public void Capture()
    {
        LastStatusCode = null;
        LastResponseHeaders = null;

        if (!_reflectionOk)
            return;

        try
        {
            object? inner = _websocketClientField!.GetValue(_eventSubClient);
            if (inner == null)
            {
                _logger.LogDebug("RateLimitProbe.Capture: _websocketClient was null");
                return;
            }

            object? socketObj = _webSocketField!.GetValue(inner);
            if (socketObj is not ClientWebSocket socket)
            {
                _logger.LogDebug("RateLimitProbe.Capture: _webSocket was not a ClientWebSocket");
                return;
            }

            LastStatusCode = socket.HttpStatusCode;
            LastResponseHeaders = socket.HttpResponseHeaders;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RateLimitProbe.Capture failed");
        }
    }

    /// <summary>
    /// Parses RFC 7231 Retry-After: either a positive integer of seconds or an HTTP-date.
    /// Returns null if the header is missing or unparseable.
    /// </summary>
    public TimeSpan? ParseRetryAfter()
    {
        string? raw = GetHeader("Retry-After");
        if (string.IsNullOrEmpty(raw))
            return null;

        if (int.TryParse(raw.Trim(), out int seconds) && seconds >= 0)
            return TimeSpan.FromSeconds(seconds);

        if (DateTimeOffset.TryParse(raw, out DateTimeOffset when))
        {
            TimeSpan delta = when - DateTimeOffset.UtcNow;
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        return null;
    }

    /// <summary>
    /// Parses Twitch's Ratelimit-Reset header (Unix epoch seconds).
    /// Returns null if missing or unparseable.
    /// </summary>
    public TimeSpan? ParseRatelimitReset()
    {
        string? raw = GetHeader("Ratelimit-Reset");
        if (!long.TryParse(raw, out long epoch))
            return null;

        DateTimeOffset reset = DateTimeOffset.FromUnixTimeSeconds(epoch);
        TimeSpan delta = reset - DateTimeOffset.UtcNow;
        return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
    }

    /// <summary>
    /// Returns the most actionable retry hint from the captured response — Retry-After
    /// preferred (it's the standard 429 field), Ratelimit-Reset as fallback.
    /// </summary>
    public TimeSpan? GetRetryHint() => ParseRetryAfter() ?? ParseRatelimitReset();

    /// <summary>
    /// Returns a log-friendly summary of what we captured. Always returns something
    /// (status code at minimum) so a missing log line never silently means "probe broke".
    /// </summary>
    public string CurrentHeadersForLog()
    {
        if (LastStatusCode == null && LastResponseHeaders == null)
            return "no response details captured (CollectHttpResponseDetails not honored)";

        List<string> bits = [];
        if (LastStatusCode != null)
            bits.Add($"status={(int)LastStatusCode}");

        // Rate-limit-relevant first.
        foreach (
            string name in new[]
            {
                "Retry-After",
                "Ratelimit-Reset",
                "Ratelimit-Remaining",
                "Ratelimit-Limit",
                "X-Ratelimit-Reset",
                "Cf-Ray",
                "Date",
                "Server",
            }
        )
        {
            string? v = GetHeader(name);
            if (!string.IsNullOrEmpty(v))
                bits.Add($"{name}={v}");
        }

        if (bits.Count == 0)
            return "captured but headers dictionary was empty";

        return string.Join(", ", bits);
    }

    private string? GetHeader(string name)
    {
        if (LastResponseHeaders == null)
            return null;
        // ClientWebSocket headers dictionary is case-insensitive in practice,
        // but use case-insensitive lookup to be safe across header sources.
        foreach (var kvp in LastResponseHeaders)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                return kvp.Value.FirstOrDefault();
        }
        return null;
    }
}
