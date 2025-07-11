using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace NoMercyBot.Database;

public class Timestamps
{
    [DefaultValue("CURRENT_TIMESTAMP")]
    [JsonProperty("created_at")]
    [TypeConverter("TIMESTAMP")]
    [Timestamp]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedAt { get; set; }

    [DefaultValue("CURRENT_TIMESTAMP")]
    [JsonProperty("updated_at")]
    [TypeConverter("TIMESTAMP")]
    [Timestamp]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}