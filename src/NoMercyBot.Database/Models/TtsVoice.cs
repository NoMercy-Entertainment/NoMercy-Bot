using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace NoMercyBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class TtsVoice
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(50)]
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("speaker_id")] public string SpeakerId { get; set; } = string.Empty;
    [JsonProperty("name")] public string Name { get; set; } = string.Empty;
    [JsonProperty("gender")] public string Gender { get; set; } = string.Empty;
    [JsonProperty("age")] public int Age { get; set; }
    [JsonProperty("accent")] public string Accent { get; set; } = string.Empty;
    [JsonProperty("description")] public string Region { get; set; } = string.Empty;
    
    [JsonProperty("user_tts_voices")] public ICollection<UserTtsVoice> UserTtsVoices { get; set; } = [];
}

