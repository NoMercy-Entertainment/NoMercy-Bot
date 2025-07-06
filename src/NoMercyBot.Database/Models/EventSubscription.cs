using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace NoMercyBot.Database.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(Provider), nameof(EventType), IsUnique = true)]
public class EventSubscription
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonProperty("id")] public string Id { get; set; } = null!;
    
    [JsonProperty("provider")] public string Provider { get; set; } = null!;
    [JsonProperty("event_type")] public string EventType { get; set; } = null!;
    [JsonProperty("enabled")] public bool Enabled { get; set; } = true;
    
    [JsonProperty("subscription_id")] public string? SubscriptionId { get; set; }
    [JsonProperty("callback_url")] public string? CallbackUrl { get; set; }
    [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [JsonProperty("expires_at")] public DateTime? ExpiresAt { get; set; }
    
    [JsonProperty("metadata_json")]
    public string? MetadataJson { get; set; }

    [NotMapped]
    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata
    {
        get => !string.IsNullOrEmpty(MetadataJson) 
            ? JsonConvert.DeserializeObject<Dictionary<string, string>>(MetadataJson) ?? new()
            : new();
        set => MetadataJson = JsonConvert.SerializeObject(value);
    }
    
    // Create a new subscription entry
    public EventSubscription() {}

    public EventSubscription(string provider, string eventType, bool enabled = true)
    {
        Id = Ulid.NewUlid().ToString();
        Provider = provider;
        EventType = eventType;
        Enabled = enabled;
    }
}
