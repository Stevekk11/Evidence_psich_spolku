using System.Security.Claims;
using API_psi_spolky.DatabaseModels;
using API_psi_spolky.dtos;
using Microsoft.EntityFrameworkCore;

namespace API_psi_spolky.Endpoints;

public record StatutesDto(string? Guidelines, DateTime? UpdatedAt);

/// <summary>
/// Provides static methods to define and map HTTP endpoints for the application.
/// </summary>
/// <remarks>
/// Includes mappings for various API routes related to exhibitions, clubs, dogs, and audit logs.
/// Each endpoint is configured with its route, HTTP method, input parameters, and relevant logic.
/// </remarks>
public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapExhibitionEndpoints();
        app.MapClubEndpoints();

        app.MapGet("/api/audit/statutes", async (SpolkyDbContext ctx, int? clubId) =>
            {
                var q = ctx.Set<AuditLog>().AsNoTracking()
                    .Where(a => a.Action == "ClubChangeRequest" || a.Action == "StatutesUpdated");

                if (clubId is not null)
                    q = q.Where(a => a.SpolekId == clubId);

                var list = await q
                    .OrderByDescending(a => a.ChangedAt)
                    .Select(a => new
                    {
                        a.Id,
                        a.SpolekId,
                        a.UserId,
                        a.Action,
                        a.ChangedAt,
                        a.OriginalData,
                        a.NewData
                    })
                    .ToListAsync();

                return Results.Ok(list);
            })
            .WithName("ListStatutesAudit")
            .WithDescription("Vrátí auditní logy změn stanov a požadavků na změny.")
            .WithSummary("List statutes audit")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "ReadOnly"));

        app.MapGet("/api/clubs/{id:int}/export", async (SpolkyDbContext ctx, int id, string? format) =>
            {
                var club = await ctx.Set<Spolek>()
                    .AsNoTracking()
                    .Where(s => s.Id == id)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Ico,
                        s.Address,
                        s.Email,
                        s.Phone,
                        s.CreatedAt,
                        s.Guidelines,
                        s.GuidelinesUpdatedAt,
                        ChairmanUserName = s.Chairman != null ? s.Chairman.UserName : null
                    })
                    .FirstOrDefaultAsync();

                if (club is null) return Results.NotFound("Spolek nebyl nalezen.");

                var fmt = (format ?? "json").ToLowerInvariant();
                if (fmt == "csv")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Id,Name,Ico,Address,Email,Phone,CreatedAt,GuidelinesUpdatedAt,ChairmanUserName");
                    string Csv(string? s) => s is null ? "" : $"\"{s.Replace("\"", "\"\"")}\"";
                    sb.AppendLine(
                        $"{club.Id},{Csv(club.Name)},{Csv(club.Ico)},{Csv(club.Address)},{Csv(club.Email)},{Csv(club.Phone)},{club.CreatedAt:O},{club.GuidelinesUpdatedAt:O},{Csv(club.ChairmanUserName)}");
                    var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                    return Results.File(bytes, "text/csv", fileDownloadName: $"club_{id}.csv");
                }
                else
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(club);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    return Results.File(bytes, "application/json", fileDownloadName: $"club_{id}.json");
                }
            })
            .WithName("ExportClub")
            .WithDescription("Export údajů spolku v JSON nebo CSV.")
            .WithSummary("Export club")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "ReadOnly"));

        app.MapPost("/api/dogs/", async (SpolkyDbContext ctx, DogCreateDto dto) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return Results.BadRequest("Name is required.");

                var nameExists = await ctx.Set<Dog>().AnyAsync(d => d.Name == dto.Name);
                if (nameExists)
                    return Results.BadRequest("Dog with this name already exists.");

                var dog = new Dog
                {
                    Name = dto.Name,
                    Breed = dto.Breed,
                    Gender = dto.Gender,
                    DateOfBirth = dto.DateOfBirth,
                    OwnersName = dto.OwnersName,
                    OwnersPhone = dto.OwnersPhone
                };

                ctx.Set<Dog>().Add(dog);
                await ctx.SaveChangesAsync();

                return Results.Created($"/api/dogs/{dog.Id}", new
                {
                    dog.Id,
                    dog.Name,
                    dog.Breed,
                    dog.Gender,
                    dog.DateOfBirth,
                    dog.OwnersName,
                    dog.OwnersPhone
                });
            })
            .WithName("CreateDog")
            .WithDescription("Vytvoří nového psa.")
            .WithSummary("Create dog")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));

        app.MapGet("/api/dogs/{id:int}", async (SpolkyDbContext ctx, int id) =>
            {
                var dog = await ctx.Set<Dog>()
                    .AsNoTracking()
                    .Where(d => d.Id == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Breed,
                        d.Gender,
                        d.DateOfBirth,
                        d.OwnersName,
                        d.OwnersPhone
                    })
                    .FirstOrDefaultAsync();

                return dog is not null ? Results.Ok(dog) : Results.NotFound();
            })
            .WithName("GetDogById")
            .WithDescription("Vrátí detail psa dle ID.")
            .WithSummary("Get dog by ID")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"));
    }
}