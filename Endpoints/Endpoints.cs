using System.Security.Claims;
using API_psi_spolky.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace API_psi_spolky.Endpoints;

public class ExhibitionResultDto
{
    public int DogId { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? Score { get; set; }
}
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
        app.MapPost("/api/exhibitions", async (SpolkyDbContext ctx, ClaimsPrincipal user, Exhibition dto) =>
            {
                // basic validation
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 200)
                    return Results.BadRequest("Name is required and must be <= 200 characters.");

                // set audit fields
                dto.CreatedById ??= user.FindFirstValue(ClaimTypes.Name);

                // ensure related Spolek exists
                var spolekExists = await ctx.Set<Spolek>().AnyAsync(s => s.Id == dto.SpolekId);
                if (!spolekExists)
                    return Results.BadRequest("Invalid SpolekId.");

                ctx.Set<Exhibition>().Add(dto);
                await ctx.SaveChangesAsync();
                return Results.Created($"/api/exhibitions/{dto.Id}", dto);
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
            .WithSummary("Get all exhibitions").RequireAuthorization("ReadOnly");

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
            .WithSummary("Get Exhibition By ID").RequireAuthorization("ReadOnly");
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
            .WithDescription("Přidá výsledek ke konkrétní výstavě.").WithSummary("Add result to an exhibition.");
        app.MapGet("/api/exhibitions/{id:int}/results", async (SpolkyDbContext ctx, int id) =>
        {
            
        });
        app.MapGet("/api/clubs", async (SpolkyDbContext ctx) =>
        {
            
        });
        app.MapGet("/api/clubs/{id:int}", async (SpolkyDbContext ctx, int id) => { });
        app.MapPut("/api/clubs/{id:int}", async (SpolkyDbContext ctx, int id) => { });
        app.MapPost("/api/clubs/", async (SpolkyDbContext ctx) => { });
        app.MapPost("/api/clubs/{id:int}/change-request", async (SpolkyDbContext ctx, int id) => { });
        app.MapGet("/api/clubs/{id:int}/export", async (SpolkyDbContext ctx, int id, string? format) => { });
        app.MapPost("/api/clubs/{id:int}/statutes", async (SpolkyDbContext ctx, int id) => { });
        app.MapGet("/api/clubs/{id:int}/statutes", async (SpolkyDbContext ctx, int id) => { });
        app.MapGet("/api/audit/statutes", async (SpolkyDbContext ctx) => { });
        app.MapPost("/api/dogs/", async (SpolkyDbContext ctx) => { });
        app.MapGet("/api/dogs/{id:int}", async (SpolkyDbContext ctx, int id) => { });
    }
}