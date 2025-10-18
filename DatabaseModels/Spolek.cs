using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_psi_spolky.DatabaseModels;

public class Spolek
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string? Ico { get; set; }
    [Required]
    public string? Address { get; set; } = string.Empty;
    [EmailAddress]
    public string? Email { get; set; }
    [Phone, Required]
    public string? Phone { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Guidelines { get; set; }
    public DateTime? GuidelinesUpdatedAt { get; set; }
    public string? ChairmanId { get; set; }
    [ForeignKey(nameof(ChairmanId))]
    public User? Chairman { get; set; }

    public ICollection<Exhibition> Exhibitions { get; set; } = new List<Exhibition>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

}