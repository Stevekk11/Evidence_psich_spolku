using API_psi_spolky.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Debugging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


SelfLog.Enable(Console.Error);
Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo
    .File("Logs/application_log.log", rollingInterval: RollingInterval.Day).WriteTo.Console().CreateLogger();

// C#
builder.Services.AddDbContext<SpolkyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Configure the HTTP request pipeline.
builder.Host.UseSerilog();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SpolkyDbContext>();
    db.Database.EnsureCreated();
}


app.MapGet("/", () => "Nothing here! Go to /login or /register to start!");
app.MapGet("/favicon.ico", async context =>
{
    context.Response.ContentType = "image/x-icon";
    await context.Response.SendFileAsync("wwwroot/favicon.ico");
});


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.Run();
