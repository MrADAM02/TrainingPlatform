using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrainingPlatform.Domain.Users;
using TrainingPlatform.Infrastructure.Identity;

namespace TrainingPlatform.Infrastructure.Database;

/// <summary>
/// Seeds the three fixed roles and, in local development only, a default Administrator account.
/// There is no self-registration in this platform (REQ-ADM-01: accounts are admin-provisioned),
/// so a seeded admin is required to bootstrap the very first login.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var configuration = services.GetRequiredService<IConfiguration>();
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Default Administrator",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Administrator);
        }
        else
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));
            logger.LogWarning(
                "Failed to seed default administrator account: {Errors}",
                string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
