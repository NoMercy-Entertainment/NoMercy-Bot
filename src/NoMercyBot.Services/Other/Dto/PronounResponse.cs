﻿using Newtonsoft.Json;
using NoMercyBot.Database.Models;

namespace NoMercyBot.Services.Other.Dto;

public class PronounResponse : Dictionary<string, Pronoun>
{
}

public class UserPronounResponse
{
    [JsonProperty("channel_id")] public string ChannelId { get; set; } = string.Empty;
    [JsonProperty("channel_login")] public string ChannelLogin { get; set; } = string.Empty;
    [JsonProperty("pronoun_id")] public string PronounId { get; set; } = string.Empty;
    [JsonProperty("alt_pronoun_id")] public string? AltPronounId { get; set; }
}