using System.Security.Claims;
using System.Text.Json;
using API_psi_spolky.DatabaseModels;
using API_psi_spolky.dtos;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace API_psi_spolky.Endpoints;

/// <summary>
/// Provides endpoint mappings for managing and retrieving information about clubs (spolky).
/// This static class defines HTTP endpoints for actions such as listing, retrieving, creating, updating,
/// and managing change requests and statutes for clubs. Each endpoint has predefined security policies
/// requiring specific user roles for access.
/// </summary>
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
            .WithTags("Clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"))
            .Produces<List<Spolek>>().Produces(401);
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
            .WithTags("Clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"))
            .Produces<Spolek>().Produces(401);
        app.MapPut("/api/clubs/{id:int}",
                async (SpolkyDbContext ctx, ClaimsPrincipal user, int id, SpolekUpdateDto dto) =>
                {
                    var club = await ctx.Set<Spolek>().Include(s => s.Chairman)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (club is null)
                        return Results.NotFound();

                    // Capture original data for audit log
                    var originalJson = JsonSerializer.Serialize(new
                    {
                        club.Name,
                        club.Ico,
                        club.Address,
                        club.Email,
                        club.Phone,
                        club.Guidelines,
                        club.GuidelinesUpdatedAt,
                        club.Chairman.UserName
                    });

                    // Update club data
                    club.Name = dto.Name;
                    club.Ico = dto.Ico;
                    club.Address = dto.Address;
                    club.Email = dto.Email;
                    club.Phone = dto.Phone;
                    club.Guidelines = dto.Guidelines;
                    if (!string.IsNullOrEmpty(dto.ChairmanUserName))
                    {
                        var newChairman = await ctx.Set<User>()
                            .FirstOrDefaultAsync(u => u.UserName == dto.ChairmanUserName);

                        if (newChairman == null)
                        {
                            return Results.BadRequest("New chairman with the provided username not found.");
                        }

                        // Reassign the chairman by updating the ChairmanId
                        club.ChairmanId = newChairman.Id;
                    }


                    // Capture new data for audit log
                    var newJson = JsonSerializer.Serialize(new
                    {
                        dto.Name,
                        dto.Ico,
                        dto.Address,
                        dto.Email,
                        dto.Phone,
                        dto.Guidelines,
                        dto.ChairmanUserName
                    });

                    // Create audit log entry
                    var auditLog = new AuditLog
                    {
                        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? user.FindFirstValue(ClaimTypes.Name),
                        SpolekId = id,
                        Action = "ClubUpdated",
                        ChangedAt = DateTime.UtcNow,
                        OriginalData = originalJson,
                        NewData = newJson
                    };
                    try
                    {
                        ctx.Set<AuditLog>().Add(auditLog);
                        await ctx.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "Error saving audit log entry");
                    }


                    return Results.Ok();
                })
            .WithName("UpdateClub")
            .WithDescription("Aktualizuje spolek (klub) dle ID")
            .WithSummary("Update Club")
            .WithTags("Clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman"));
        app.MapPost("/api/clubs/", async (SpolkyDbContext ctx, ClaimsPrincipal user, SpolekCreateDto dto) =>
            {
                // basic validation
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 200)
                    return Results.BadRequest("Name is required and must be <= 200 characters.");


                var nameExists = await ctx.Set<Spolek>().AnyAsync(s => s.Name == dto.Name);
                if (nameExists)
                    return Results.BadRequest("Club with this name already exists.");

                var newClub = new Spolek
                {
                    Name = dto.Name,
                    Ico = dto.Ico,
                    Address = dto.Address,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    CreatedAt = DateTime.UtcNow,
                    ChairmanId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? user.FindFirstValue(ClaimTypes.Name),
                    Guidelines = dto.Guidelines
                };

                ctx.Set<Spolek>().Add(newClub);
                await ctx.SaveChangesAsync();

                return Results.Created($"/api/clubs/{newClub.Id}", new
                {
                    newClub.Id,
                    newClub.Name,
                    newClub.Ico,
                    newClub.Address,
                    newClub.CreatedAt,
                    newClub.Phone,
                    newClub.Email,
                    newClub.Guidelines,
                    newClub.GuidelinesUpdatedAt,
                    newClub.ChairmanId
                });
            })
            .WithName("CreateClub")
            .WithDescription("Vytvoří nový klub (spolek).")
            .WithSummary("Create club")
            .WithTags("Clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman")).Produces<Spolek>().Produces(401);
        app.MapPost("/api/clubs/{id:int}/change-request",
                async (SpolkyDbContext ctx, ClaimsPrincipal user, int id, SpolekUpdateDto proposed) =>
                {
                    var club = await ctx.Set<Spolek>().AsNoTracking().Include(s => s.Chairman)
                        .FirstOrDefaultAsync(s => s.Id == id);
                    if (club is null) return Results.NotFound("Spolek nebyl nalezen.");

                    // Serialize old/new states
                    var originalJson = JsonSerializer.Serialize(new
                    {
                        club.Name,
                        club.Ico,
                        club.Email,
                        club.Phone,
                        club.Guidelines,
                        club.GuidelinesUpdatedAt,
                        club.Chairman.UserName,
                    });

                    var proposedJson = JsonSerializer.Serialize(new
                    {
                        proposed.Name,
                        proposed.Ico,
                        proposed.Address,
                        proposed.Email,
                        proposed.Phone,
                        proposed.Guidelines,
                        proposed.ChairmanUserName
                    });

                    var log = new AuditLog
                    {
                        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? user.FindFirstValue(ClaimTypes.Name),
                        SpolekId = id,
                        Action = "ClubChangeRequest",
                        ChangedAt = DateTime.UtcNow,
                        OriginalData = originalJson,
                        NewData = proposedJson
                    };

                    ctx.Set<AuditLog>().Add(log);
                    await ctx.SaveChangesAsync();

                    return Results.Created($"/api/audit/statutes?clubId={id}",
                        new { log.Id, log.OriginalData, log.NewData, log.ChangedAt });
                })
            .WithName("CreateClubChangeRequest")
            .WithDescription("Vytvoří požadavek na změnu údajů spolku do audit logu.")
            .WithSummary("Create club change request")
            .WithTags("Clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman")).Produces<AuditLog>().Produces(401);

        app.MapPost("/api/clubs/{id:int}/statutes",
                async (SpolkyDbContext ctx, ClaimsPrincipal user, int id, StatutesDto dto) =>
                {
                    var club = await ctx.Set<Spolek>().FirstOrDefaultAsync(s => s.Id == id);
                    if (club is null) return Results.NotFound("Spolek nebyl nalezen.");

                    // Capture original statutes for audit log
                    var originalJson = JsonSerializer.Serialize(new
                    {
                        club.Guidelines,
                        club.GuidelinesUpdatedAt
                    });

                    // Update statutes
                    club.Guidelines = dto.Guidelines;
                    club.GuidelinesUpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow;

                    // Capture new statutes for audit log
                    var newJson = JsonSerializer.Serialize(new
                    {
                        Guidelines = dto.Guidelines,
                        GuidelinesUpdatedAt = club.GuidelinesUpdatedAt
                    });


                    var auditLog = new AuditLog
                    {
                        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? user.FindFirstValue(ClaimTypes.Name),
                        SpolekId = id,
                        Action = "StatutesUpdated",
                        ChangedAt = DateTime.UtcNow,
                        OriginalData = originalJson,
                        NewData = newJson
                    };

                    ctx.Set<AuditLog>().Add(auditLog);
                    await ctx.SaveChangesAsync();

                    return Results.NoContent();
                })
            .WithName("UploadClubStatutes")
            .WithDescription("Aktualizuje stanovy (Guidelines) spolku.")
            .WithSummary("Upload club statutes")
            .WithTags("Clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman")).Produces<Spolek>().Produces(401);

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
            .WithTags("Clubs")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Chairman", "Public", "ReadOnly"))
            .Produces<Spolek>().Produces(401);
    }
}