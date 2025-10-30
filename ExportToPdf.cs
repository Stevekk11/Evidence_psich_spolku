using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace API_psi_spolky;

/// <summary>
/// Provides methods for exporting data to a PDF format.
/// </summary>
public static class ExportToPdf
{
    public static byte[] GenerateClubPdf(ClubExportData club)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        using var ms = new MemoryStream();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text("Informace o spolku")
                    .FontSize(20)
                    .Bold()
                    .AlignCenter();

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(col =>
                    {
                        col.Spacing(5);
                        col.Item().Text($"ID: {club.Id}");
                        col.Item().Text($"Název: {club.Name}");
                        col.Item().Text($"IČO: {club.Ico ?? "N/A"}");
                        col.Item().Text($"Adresa: {club.Address ?? "N/A"}");
                        col.Item().Text($"Email: {club.Email ?? "N/A"}");
                        col.Item().Text($"Telefon: {club.Phone ?? "N/A"}");
                        col.Item().Text($"Vytvořeno: {club.CreatedAt:dd.MM.yyyy HH:mm}");
                        col.Item().Text($"Předseda: {club.ChairmanUserName ?? "N/A"}");

                        if (!string.IsNullOrEmpty(club.Guidelines))
                        {
                            col.Item().PaddingTop(10).Text("Stanovy:").Bold();
                            col.Item().Text(club.Guidelines);
                            if (club.GuidelinesUpdatedAt.HasValue)
                            {
                                col.Item().Text($"Poslední aktualizace: {club.GuidelinesUpdatedAt:dd.MM.yyyy HH:mm}");
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Stránka ");
                        x.CurrentPageNumber();
                    });
            });
        });

        document.GeneratePdf(ms);
        return ms.ToArray();
    }
}

public record ClubExportData(
    int Id,
    string Name,
    string? Ico,
    string? Address,
    string? Email,
    string? Phone,
    DateTime? CreatedAt,
    string? Guidelines,
    DateTime? GuidelinesUpdatedAt,
    string? ChairmanUserName
);