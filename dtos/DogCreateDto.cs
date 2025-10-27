namespace API_psi_spolky.dtos;

public record DogCreateDto(
    string Name,
    string? Breed,
    string? Gender,
    DateTime? DateOfBirth,
    string? OwnersName,
    string? OwnersPhone
);