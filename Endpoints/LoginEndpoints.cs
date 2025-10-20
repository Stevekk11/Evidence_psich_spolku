using API_psi_spolky.DatabaseModels;
using Microsoft.AspNetCore.Identity;

namespace API_psi_spolky.Endpoints;

/// <summary>
/// Represents the data transfer object for registering a new user.
/// Contains essential user information required for account creation.
/// </summary>
public record RegisterUserDto(string Email, string Password, string Name, string Surname, Role Role = Role.Public, string PhoneNumber = "");

/// <summary>
/// Represents the data transfer object for logging in a user.
/// Contains user credentials required for authentication.
/// </summary>
public record LoginUserDto(string UserName, string Password);

/// <summary>
/// Provides endpoint mappings for user authentication functionality,
/// including user registration and login operations.
/// </summary>
public static class LoginEndpoints
{
    /// <summary>
    /// Configures the login and registration endpoints for the application.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance used to map the endpoints.</param>
    public static void MapLoginEndpoints(this WebApplication app)
    {
        app.MapPost("/register", async (UserManager<User> userManager, RegisterUserDto registration, RoleManager<IdentityRole> roleManager) =>
        {
            var user = new User
            {
                UserName = registration.Name,
                Email = registration.Email,
                Surname = registration.Surname,
                Role = registration.Role,
                PhoneNumber = registration.PhoneNumber
            };

            var createResult = await userManager.CreateAsync(user, registration.Password);
            if (!createResult.Succeeded)
                return Results.BadRequest(createResult.Errors);

            // Ensure Identity role exists
            var roleName = registration.Role.ToString();
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleCreate = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleCreate.Succeeded)
                    return Results.BadRequest(roleCreate.Errors);
            }

            // Assign user to an Identity role
            var addToRole = await userManager.AddToRoleAsync(user, roleName);
            if (!addToRole.Succeeded)
                return Results.BadRequest(addToRole.Errors);

            return Results.Ok("User registered successfully.");
        }).WithName("Register").WithDescription("Registers a new user.").WithDisplayName("Register");

        app.MapPost("/login", async (SignInManager<User> signInManager, LoginUserDto login) =>
        {
            var result = await signInManager.PasswordSignInAsync(login.UserName, login.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Results.Ok("Login successful. Welcome, " + login.UserName + "");
            }

            return Results.Unauthorized();
        }).WithName("Login").WithDescription("Logs in an existing user.");

        app.MapPost("/logout", async (SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization().WithName("Logout").WithDescription("Logs out the current user.").WithDisplayName("Logout");
    }
}