
using Newtonsoft.Json;

namespace NoMercyBot.Services.Emotes.Dto;

public class SevenTvGlobalResponse
{
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("name")] public string Name { get; set; } = string.Empty;
    [JsonProperty("flags")] public int Flags { get; set; }
    [JsonProperty("tags")] public object[] Tags { get; set; } = [];
    [JsonProperty("immutable")] public bool Immutable { get; set; }
    [JsonProperty("privileged")] public bool Privileged { get; set; }
    [JsonProperty("emotes")] public SevenTvEmote[] Emotes { get; set; } = [];
    [JsonProperty("emote_count")] public int EmoteCount { get; set; }
    [JsonProperty("capacity")] public int Capacity { get; set; }
    [JsonProperty("owner")] public SevenTvOwner Owner { get; set; } = new();
}

public class SevenTvEmote
{
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("name")] public string Name { get; set; } = string.Empty;
    [JsonProperty("flags")] public int Flags { get; set; }
    [JsonProperty("timestamp")] public long Timestamp { get; set; }
    [JsonProperty("actor_id")] public string ActorId { get; set; } = string.Empty;
    [JsonProperty("data")] public SevenTvData Data { get; set; } = new();
    [JsonProperty("origin_id")] public object? OriginId { get; set; }
}

public class SevenTvData
{
    [JsonProperty("id")] public string Id { get; set; }  = string.Empty;
    [JsonProperty("name")] public string Name { get; set; } = string.Empty;
    [JsonProperty("flags")] public int Flags { get; set; }
    [JsonProperty("lifecycle")] public int Lifecycle { get; set; }
    [JsonProperty("state")] public string[] State { get; set; } = [];
    [JsonProperty("listed")] public bool Listed { get; set; }
    [JsonProperty("animated")] public bool Animated { get; set; }
    [JsonProperty("owner", NullValueHandling = NullValueHandling.Ignore)] public SevenTvOwner Owner { get; set; } = new();
    [JsonProperty("host")] public SevenTvHost Host { get; set; }
    [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)] public string[] Tags { get; set; } = [];
}

public class SevenTvHost
{
    [JsonProperty("url")] public Uri Url { get; set; } = null!;
    [JsonProperty("files")] public SevenTvFile[] Files { get; set; } = [];
}

public class SevenTvFile
{
    [JsonProperty("name")] public string Name { get; set; } = string.Empty;
    [JsonProperty("static_name")] public string StaticName { get; set; } = string.Empty;
    [JsonProperty("width")] public int Width { get; set; }
    [JsonProperty("height")] public int Height { get; set; }
    [JsonProperty("frame_count")] public int FrameCount { get; set; }
    [JsonProperty("size")] public int Size { get; set; }
    [JsonProperty("format")] public string Format { get; set; } = string.Empty;
}

public class SevenTvOwner
{
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("username")] public string Username { get; set; } = string.Empty;
    [JsonProperty("display_name")] public string DisplayName { get; set; } = string.Empty;
    [JsonProperty("avatar_url")] public Uri AvatarUrl { get; set; } = null!;
    [JsonProperty("style")] public SevenTvStyle Style { get; set; } = new();
    [JsonProperty("role_ids")] public string[] RoleIds { get; set; } = [];
    [JsonProperty("connections")] public SevenTvConnection[] Connections { get; set; } = [];
}

public class SevenTvConnection
{
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("platform")] public string Platform { get; set; }  = string.Empty;
    [JsonProperty("username")] public string Username { get; set; } = string.Empty;
    [JsonProperty("display_name")] public string DisplayName { get; set; } = string.Empty;
    [JsonProperty("linked_at")] public long LinkedAt { get; set; }
    [JsonProperty("emote_capacity")] public int EmoteCapacity { get; set; } = 0;
    [JsonProperty("emote_set_id")] public string EmoteSetId { get; set; } = string.Empty;
}

public class SevenTvStyle
{
    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)] public int? Color { get; set; }
    [JsonProperty("paint_id", NullValueHandling = NullValueHandling.Ignore)] public string PaintId { get; set; } = string.Empty;
    [JsonProperty("badge_id", NullValueHandling = NullValueHandling.Ignore)] public string BadgeId { get; set; } = string.Empty;
}
