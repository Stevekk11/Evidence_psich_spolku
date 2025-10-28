namespace API_psi_spolky.dtos;

public record StatutesDto(string? Guidelines, DateTime? UpdatedAt);
public record SpolekCreateDto(string Name, string? Ico, string? Address, string? Email, string? Phone);
public record SpolekUpdateDto(string Name, string? Ico, string? Address, string? Email, string? Phone);