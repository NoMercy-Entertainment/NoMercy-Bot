using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace NoMercyBot.Database.Models.ChatMessage;

[NotMapped]
public class ChatBadge
{
    [JsonProperty("set_id", NullValueHandling = NullValueHandling.Ignore)] public string? SetId { get; set; }
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)] public string? Id { get; set; }
    [JsonProperty("info", NullValueHandling = NullValueHandling.Ignore)] public string? Info { get; set; }
}
