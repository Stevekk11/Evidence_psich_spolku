using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_psi_spolky.DatabaseModels;

public class ExhibitionResult
{
    [Key]
    public int Id { get; set; }
    [ForeignKey(nameof(Exhibition))]
    public int ExhibitionId { get; set; }

    public Exhibition Exhibition { get; set; } = null!;
    [ForeignKey(nameof(Dog))]
    public int DogId { get; set; }

    public Dog Dog { get; set; } = null!;

    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? Score { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public string? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
}