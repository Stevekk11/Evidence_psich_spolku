using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_psi_spolky.DatabaseModels;

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    public string UserId { get; set; }
    public User User { get; set; } = null!;

    [ForeignKey(nameof(Spolek))]
    public int SpolekId { get; set; }
    public Spolek Spolek { get; set; } = null!;

    [Required]
    public string Action { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? OriginalData { get; set; } // JSON
    public string? NewData { get; set; } // JSON
}