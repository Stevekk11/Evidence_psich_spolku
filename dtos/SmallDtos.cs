namespace API_psi_spolky.dtos;

public record StatutesDto(string? Guidelines, DateTime? UpdatedAt);
public record SpolekCreateDto(string Name, string? Ico, string? Address, string? Email, string? Phone, string? Guidelines);
public record SpolekUpdateDto(string Name, string? Ico, string? Address, string? Email, string? Phone, string? Guidelines, string? ChairmanUserName);
public record RegisterUserDto(string Email, string Password, string Name, string Surname, Role Role = Role.Public, string PhoneNumber = "");
public record LoginUserDto(string UserName, string Password);
