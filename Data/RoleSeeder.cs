using Amanat.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amanat.Data;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in AmanatRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to create role '{role}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var seedUsers = configuration.GetSection("SeedUsers").Get<List<SeedUserOptions>>() ?? [];

        foreach (var seed in seedUsers)
        {
            if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password) || string.IsNullOrWhiteSpace(seed.FullName))
            {
                throw new InvalidOperationException("SeedUsers entries require Email, Password and FullName.");
            }

            if (seed.Role is not (AmanatRoles.Organizer or AmanatRoles.Treasurer))
            {
                throw new InvalidOperationException($"SeedUsers role '{seed.Role}' is not an administrative role.");
            }

            var user = await userManager.FindByEmailAsync(seed.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    FullName = seed.FullName,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, seed.Password);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to create seed user '{seed.Email}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, seed.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, seed.Role);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to assign role '{seed.Role}' to '{seed.Email}': {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}

public sealed class SeedUserOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
