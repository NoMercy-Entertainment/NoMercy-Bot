﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace NoMercyBot.Database.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(ChannelId), nameof(ShoutedUserId), IsUnique = true)]
public class Shoutout: Timestamps
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [MaxLength(50)]
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("enabled")] public bool Enabled { get; set; }
    [JsonProperty("message_template")] public string MessageTemplate { get; set; } = AppDbConfig.DefaultShoutoutTemplate;
    
    [JsonProperty("last_shoutout")] public DateTime? LastShoutout { get; set; }
    
    [JsonProperty("channel_id")] public string ChannelId { get; set; } = null!;
    [JsonProperty("channel")]  public Channel Channel { get; set; } = null!;
    
    [JsonProperty("user_id")] public string ShoutedUserId { get; set; } = null!;
    [JsonProperty("user")] public User ShoutedUser { get; set; } = null!;

    public Shoutout()
    {
        //
    }
    public Shoutout(string id, string channelId)
    {
        Id = id;
        ChannelId = channelId;
        Enabled = true;
    }
    
}

public class SimpleShoutout
{
    [JsonProperty("channel_id")] public string ChannelId { get; set; }
    [JsonProperty("user_id")] public string ShoutedUserId { get; set; }
    [JsonProperty("enabled")] public bool Enabled { get; set; }
    [JsonProperty("message")] public string Message { get; set; }
    [JsonProperty("username")] public string Username { get; set; }
    [JsonProperty("display_name")] public string DisplayName { get; set; }
    
    public SimpleShoutout(Shoutout shoutout)
    {
        ChannelId = shoutout.ChannelId;
        ShoutedUserId = shoutout.ShoutedUserId;
        Enabled = shoutout.Enabled;
        Message = shoutout.MessageTemplate;
        
        Username = shoutout.ShoutedUser.Username;
        DisplayName = shoutout.ShoutedUser.DisplayName;
    }
}