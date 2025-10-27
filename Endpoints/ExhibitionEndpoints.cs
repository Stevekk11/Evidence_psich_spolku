using System.Security.Claims;
using API_psi_spolky.DatabaseModels;
using API_psi_spolky.dtos;
using Microsoft.EntityFrameworkCore;

namespace API_psi_spolky.Endpoints;

public static class ExhibitionEndpoints
{
    public static void MapExhibitionEndpoints(this WebApplication app)
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
    }
}