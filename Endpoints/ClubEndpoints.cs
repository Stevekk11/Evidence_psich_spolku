using System.Security.Claims;
using API_psi_spolky.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace API_psi_spolky.Endpoints;

public static class ClubEndpoints
{
    public static void MapClubEndpoints(this WebApplication app)
    {
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
    }
}