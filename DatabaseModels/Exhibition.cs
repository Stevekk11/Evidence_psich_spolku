using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_psi_spolky.DatabaseModels;

public class Exhibition
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }
    public string? Place { get; set; }

    [ForeignKey(nameof(Spolek))]
    public int SpolekId { get; set; }
    public Spolek Spolek { get; set; } = null!;

    public string? Description { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public string? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<ExhibitionResult> ExhibitionResults { get; set; } = new List<ExhibitionResult>();
}