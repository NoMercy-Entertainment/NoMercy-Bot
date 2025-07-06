using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace NoMercyBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Timezone { get; set; }
    
    [NotMapped]
    public TimeZoneInfo? TimeZoneInfo => !string.IsNullOrEmpty(Timezone) 
        ? TimeZoneInfo.FindSystemTimeZoneById(Timezone) 
        : null;
    
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(2048)]
    public string ProfileImageUrl { get; set; } = string.Empty;
    
    
    [MaxLength(2048)]
    public string OfflineImageUrl { get; set; } = string.Empty;
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    [MaxLength(50)]
    public string BroadcasterType { get; set; } = string.Empty;
    
    public bool Enabled { get; set; }

    public bool IsLive { get; set; }

    public virtual Channel Channel { get; set; } = new();
    
    [JsonIgnore]
    public string? PronounData { get; set; }

    [NotMapped]
    [JsonProperty("pronoun")] public Pronoun? Pronoun
    {
        get => !string.IsNullOrEmpty(PronounData) 
            ? JsonConvert.DeserializeObject<Pronoun>(PronounData) 
            : null;
        init => PronounData = value != null 
            ? JsonConvert.SerializeObject(value) 
            : null;
    }
}

public class SimpleUser
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("user_name")] public string Username { get; set; }
    [JsonProperty("display_name")] public string DisplayName { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("profile_image_url")] public string ProfileImageUrl { get; set; }
    [JsonProperty("offline_image_url")]  public string OfflineImageUrl { get; set; }
    [JsonProperty("color")] public string? Color { get; set; }
    [JsonProperty("broadcaster_type")] public string BroadcasterType { get; set; }
    [JsonProperty("enabled")] public bool Enabled { get; set; }
    [JsonProperty("is_live")] public bool IsLive { get; set; }
    [JsonProperty("pronoun")] public Pronoun? Pronoun { get; set; }
    
    public SimpleUser(User user)
    {
        Id = user.Id;
        Username = user.Username;
        DisplayName = user.DisplayName;
        Description = user.Description;
        ProfileImageUrl = user.ProfileImageUrl;
        OfflineImageUrl = user.OfflineImageUrl;
        Color = user.Color;
        BroadcasterType = user.BroadcasterType;
        Enabled = user.Enabled;
        IsLive = user.IsLive;
        Pronoun = user.Pronoun;
    }
}