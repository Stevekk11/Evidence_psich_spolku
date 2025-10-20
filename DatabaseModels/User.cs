using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace API_psi_spolky.DatabaseModels;

public class User : IdentityUser
{
    [Required, MaxLength(100)]
    public string Surname { get; set; } = string.Empty;
    [Required]
    public Role Role { get; set; }
    [Required]
    public bool IsActive { get; set; } = true;
}