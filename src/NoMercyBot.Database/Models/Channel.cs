using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NoMercyBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class Channel
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(255)]
    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    
    public virtual ICollection<ChatPresence> UsersInChat { get; set; } = [];
}