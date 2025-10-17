using System.ComponentModel.DataAnnotations;

namespace API_psi_spolky.DatabaseModels;

public class Dog
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string? Breed { get; set; }
    [Required]
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? OwnersName { get; set; }
    [Phone]
    public string? OwnersPhone { get; set; }

    public ICollection<ExhibitionResult> ExhibitionResults { get; set; } = new List<ExhibitionResult>();
}