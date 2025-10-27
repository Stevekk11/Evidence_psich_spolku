using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using API_psi_spolky.DatabaseModels;
using API_psi_spolky.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Debugging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new()
        {
            Title = "Evidence psích spolků a chovatelů", Version = "v1",
            Description = "Api pro psí spolky a jejich evidenci včetně výstav",
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://mit-license.org/")
            }
        });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

SelfLog.Enable(Console.Error);
Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo
    .File("Logs/application_log.log", rollingInterval: RollingInterval.Day).WriteTo.Console().CreateLogger();

// C#
builder.Services.AddDbContext<SpolkyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<SpolkyDbContext>();
builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/"; // redirect target when unauthenticated
    options.AccessDeniedPath = "/";
});
// Configure the HTTP request pipeline.
builder.Host.UseSerilog();
builder.Services.AddAuthorization();
// C#

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SpolkyDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () => { return "Nothing here! Go to /login or /register to start!"; })
    .WithDescription("The root page - nothing here!").WithName("root").WithDisplayName("Root");

app.MapGet("/favicon.ico", async context =>
{
    context.Response.ContentType = "image/x-icon";
    await context.Response.SendFileAsync("wwwroot/favicon.ico");
}).WithDescription("Returns the favicon").WithName("favicon").WithDisplayName("Favicon");

app.MapGet("/me", (ClaimsPrincipal user) =>
    {
        var email = user.FindFirstValue(ClaimTypes.Email);
        var name = user.FindFirstValue(ClaimTypes.Name);
        var surname = user.FindFirstValue(ClaimTypes.Surname);
        return Results.Ok(new
        {
            email, name, surname
        });
    }).RequireAuthorization().WithName("Me").WithDescription("Returns the user's email, name and surname")
    .WithDisplayName("Me").WithSummary("Get current account details");

app.MapLoginEndpoints();
app.MapEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//add middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseHttpsRedirection();
//run the app
app.Run();