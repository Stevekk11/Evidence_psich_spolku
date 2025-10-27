namespace API_psi_spolky.dtos;

public record ExhibitionCreateDto(
    string Name,
    DateTime Date,
    string? Place,
    int SpolekId,
    string? Description
);
