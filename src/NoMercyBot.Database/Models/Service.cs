using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace NoMercyBot.Database.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(Name), IsUnique = true)]
public class Service : Timestamps
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Ulid Id { get; init; } = Ulid.NewUlid();

    public string Name { get; set; } = null!;

    public bool Enabled { get; set; }
    
    public string? ClientId { get; set; }
    
    public string? ClientSecret { get; set; }
    
    public string[] Scopes { get; set; } = [];
    
    [JsonIgnore]
    public string? AccessToken { get; set; }

    [JsonIgnore]
    public string? RefreshToken { get; set; }
    
    [JsonIgnore]
    public DateTime? TokenExpiry { get; set; }

}