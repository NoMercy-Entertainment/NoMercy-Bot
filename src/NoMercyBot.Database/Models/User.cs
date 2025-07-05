using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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
}