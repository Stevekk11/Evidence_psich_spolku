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
        
        app.MapPost("/api/exhibitions", async (SpolkyDbContext ctx, ClaimsPrincipal user, ExhibitionCreateDto dto) =>
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 200)
                    return Results.BadRequest("Name is required and must be <= 200 characters.");

                // Uniqueness check due to unique index on Exhibition.Name
                var nameExists = await ctx.Set<Exhibition>().AnyAsync(e => e.Name == dto.Name);
                if (nameExists)
                    return Results.BadRequest("Exhibition with this name already exists.");

                // Ensure related Spolek exists
                var spolekExists = await ctx.Set<Spolek>().AnyAsync(s => s.Id == dto.SpolekId);
                if (!spolekExists)
                    return Results.BadRequest($"Invalid SpolekId: {dto.SpolekId}");

                // Resolve user id (Identity uses string keys)
                var createdById = user.FindFirstValue(ClaimTypes.NameIdentifier) 
                                  ?? user.FindFirstValue(ClaimTypes.Name);

                var entity = new Exhibition
                {
                    Name = dto.Name,
                    Date = dto.Date,
                    Place = dto.Place,
                    SpolekId = dto.SpolekId,
                    Description = dto.Description,
                    CreatedById = createdById
                };

                ctx.Set<Exhibition>().Add(entity);
                await ctx.SaveChangesAsync();

                // Return a safe projection (avoid returning tracked entity with navs)
                var response = new
                {
                    entity.Id,
                    entity.Name,
                    entity.Date,
                    entity.Place,
                    entity.SpolekId,
                    entity.Description
                };

                return Results.Created($"/api/exhibitions/{entity.Id}", response);
            })
            .WithName("CreateExhibition")
            .WithDescription("Vytvoří novou výstavu.")
            .WithSummary("Create Exhibition")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));

        app.MapGet("/api/exhibitions", async (SpolkyDbContext ctx) =>
            {
                var list = await ctx.Set<Exhibition>()
                    .AsNoTracking()
                    .Select(e => new
                    {
                        e.Id,
                        e.Name,
                        e.Date,
                        e.Place,
                        e.SpolekId,
                        e.Description
                    })
                    .ToListAsync();

                return Results.Ok(list);
            }).WithName("GetExhibitions")
            .WithDescription("Vrátí seznam výstav.")
            .WithSummary("Get all exhibitions")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"));

        app.MapGet("/api/exhibitions/{id:int}", async (SpolkyDbContext ctx, int id) =>
            {
                var exhibition = await ctx.Set<Exhibition>()
                    .AsNoTracking()
                    .Where(e => e.Id == id)
                    .Select(e => new
                    {
                        e.Id,
                        e.Name,
                        e.Date,
                        e.Place,
                        e.SpolekId,
                        e.Description
                    })
                    .FirstOrDefaultAsync();

                return exhibition is not null
                    ? Results.Ok(exhibition)
                    : Results.NotFound();
            })
            .WithName("GetExhibitionById")
            .WithDescription("Vrátí detail výstavy dle ID")
            .WithSummary("Get Exhibition By ID")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"));
        app.MapPost("/api/exhibitions/{id:int}/results", async (SpolkyDbContext ctx, int id, ExhibitionResultDto dto) =>
            {
                // Ověření, že výstava existuje
                var exhibitionExists = await ctx.Set<Exhibition>().AnyAsync(e => e.Id == id);
                if (!exhibitionExists)
                    return Results.NotFound("Výstava nebyla nalezena.");

                // Ověření, že pes existuje (volitelné, ale doporučeno)
                var dogExists = await ctx.Set<Dog>().AnyAsync(d => d.Id == dto.DogId);
                if (!dogExists)
                    return Results.BadRequest("Zvolený pes neexistuje.");

                // Vytvoření entity výsledku
                var result = new ExhibitionResult
                {
                    ExhibitionId = id,
                    DogId = dto.DogId,
                    Location = dto.Location,
                    Description = dto.Description,
                    Score = dto.Score
                };

                ctx.Set<ExhibitionResult>().Add(result);
                await ctx.SaveChangesAsync();

                return Results.Created($"/api/exhibitions/{id}/results/{result.Id}", new
                {
                    result.Id,
                    result.ExhibitionId,
                    result.DogId,
                    result.Location,
                    result.Description,
                    result.Score
                });
            })
            .WithName("AddExhibitionResult")
            .WithDescription("Přidá výsledek ke konkrétní výstavě.").WithSummary("Add result to an exhibition.")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));
        app.MapGet("/api/exhibitions/{id:int}/results", async (SpolkyDbContext ctx, int id) =>
            {
                var exists = await ctx.Set<Exhibition>().AnyAsync(e => e.Id == id);
                if (!exists)
                    return Results.NotFound("Výstava nebyla nalezena.");

                var results = await ctx.Set<ExhibitionResult>()
                    .AsNoTracking()
                    .Where(r => r.ExhibitionId == id)
                    .Select(r => new
                    {
                        r.Id,
                        r.ExhibitionId,
                        r.DogId,
                        r.Location,
                        r.Description,
                        r.Score
                    })
                    .ToListAsync();

                return Results.Ok(results);
            }).WithName("GetExhibitionResults")
            .WithDescription("Vrátí seznam výsledků pro konkrétní výstavu.")
            .WithSummary("Get results for an exhibition")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"));
        app.MapGet("/api/clubs", async (SpolkyDbContext ctx) =>
            {
                var clubs = await ctx.Set<Spolek>()
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Ico,
                        s.Address,
                        s.CreatedAt,
                        s.GuidelinesUpdatedAt,
                        s.Chairman.UserName,
                        s.Email,
                        s.Phone
                    })
                    .ToListAsync();

                return Results.Ok(clubs);
            })
            .WithName("GetClubs")
            .WithDescription("Vrátí seznam všech spolků (klubů).")
            .WithSummary("Get all clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"));
        app.MapGet("/api/clubs/{id:int}", async (SpolkyDbContext ctx, int id) =>
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
                        s.CreatedAt,
                        s.GuidelinesUpdatedAt,
                        s.Chairman.UserName,
                        s.Email,
                        s.Phone
                    })
                    .FirstOrDefaultAsync();

                return club is not null
                    ? Results.Ok(club)
                    : Results.NotFound();
            })
            .WithName("GetClubById")
            .WithDescription("Vrátí detail spolku (klubu) dle ID")
            .WithSummary("Get Club By ID")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"));
        app.MapPut("/api/clubs/{id:int}", async (SpolkyDbContext ctx, int id, Spolek dto) =>
            {
                var club = await ctx.Set<Spolek>()
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (club is null)
                    return Results.NotFound();

                club.Name = dto.Name;
                club.Ico = dto.Ico;
                club.Address = dto.Address;
                club.Email = dto.Email;
                club.Phone = dto.Phone;
                club.CreatedAt = DateTime.UtcNow;
                club.Guidelines = dto.Guidelines;
                club.GuidelinesUpdatedAt = dto.GuidelinesUpdatedAt;

                await ctx.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("UpdateClub")
            .WithDescription("Aktualizuje spolek (klub) dle ID")
            .WithSummary("Update Club")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));
        app.MapPost("/api/clubs/", async (SpolkyDbContext ctx, ClaimsPrincipal user, Spolek dto) =>
            {
                // basic validation
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 200)
                    return Results.BadRequest("Name is required and must be <= 200 characters.");


                var nameExists = await ctx.Set<Spolek>().AnyAsync(s => s.Name == dto.Name);
                if (nameExists)
                    return Results.BadRequest("Club with this name already exists.");

                dto.CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt;
                dto.ChairmanId = user.FindFirstValue(ClaimTypes.NameIdentifier) 
                                                   ?? user.FindFirstValue(ClaimTypes.Name);
                ctx.Set<Spolek>().Add(dto);
                await ctx.SaveChangesAsync();

                return Results.Created($"/api/clubs/{dto.Id}", new
                {
                    dto.Id,
                    dto.Name,
                    dto.Ico,
                    dto.Address,
                    dto.CreatedAt,
                    dto.Phone,
                    dto.Email,
                    dto.Guidelines,
                    dto.GuidelinesUpdatedAt,
                    dto.ChairmanId
                });
            })
            .WithName("CreateClub")
            .WithDescription("Vytvoří nový klub (spolek).")
            .WithSummary("Create club")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));
        app.MapPost("/api/clubs/{id:int}/change-request",
                async (SpolkyDbContext ctx, ClaimsPrincipal user, int id, Spolek proposed) =>
                {
                    var club = await ctx.Set<Spolek>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                    if (club is null) return Results.NotFound("Spolek nebyl nalezen.");

                    // Serialize old/new states (simple approach; replace with your serializer if needed)
                    var originalJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        club.Id,
                        club.Name,
                        club.Ico,
                        club.Address,
                        club.Email,
                        club.Phone,
                        club.Guidelines,
                        club.GuidelinesUpdatedAt
                    });

                    var proposedJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        proposed.Name,
                        proposed.Ico,
                        proposed.Address,
                        proposed.Email,
                        proposed.Phone,
                        proposed.Guidelines,
                        proposed.GuidelinesUpdatedAt
                    });

                    var log = new AuditLog
                    {
                        UserId = user.FindFirstValue(ClaimTypes.Name),
                        SpolekId = id,
                        Action = "ClubChangeRequest",
                        ChangedAt = DateTime.UtcNow,
                        OriginalData = originalJson,
                        NewData = proposedJson
                    };

                    ctx.Set<AuditLog>().Add(log);
                    await ctx.SaveChangesAsync();

                    return Results.Created($"/api/audit/statutes?clubId={id}", new { log.Id });
                })
            .WithName("CreateClubChangeRequest")
            .WithDescription("Vytvoří požadavek na změnu údajů spolku (audit log).")
            .WithSummary("Create club change request")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));

        app.MapPost("/api/clubs/{id:int}/statutes", async (SpolkyDbContext ctx, int id, StatutesDto dto) =>
            {
                var club = await ctx.Set<Spolek>().FirstOrDefaultAsync(s => s.Id == id);
                if (club is null) return Results.NotFound("Spolek nebyl nalezen.");

                club.Guidelines = dto.Guidelines;
                club.GuidelinesUpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow;

                await ctx.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("UploadClubStatutes")
            .WithDescription("Aktualizuje stanovy (Guidelines) spolku.")
            .WithSummary("Upload club statutes")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));

        app.MapGet("/api/clubs/{id:int}/statutes", async (SpolkyDbContext ctx, int id) =>
            {
                var data = await ctx.Set<Spolek>()
                    .AsNoTracking()
                    .Where(s => s.Id == id)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Guidelines,
                        s.GuidelinesUpdatedAt
                    })
                    .FirstOrDefaultAsync();

                return data is not null ? Results.Ok(data) : Results.NotFound();
            })
            .WithName("GetClubStatutes")
            .WithDescription("Vrátí stanovy (Guidelines) a datum poslední aktualizace.")
            .WithSummary("Get club statutes")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"));

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