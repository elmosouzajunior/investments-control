using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Omega.Invest.Infrastructure.Identity;

namespace Omega.Invest.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsurePlanProjectionColumnsAsync(context);
        await EnsureInvestmentResponsibleColumnsAsync(context);

        foreach (var roleName in new[] { "Master", "CompanyUser" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var email = configuration["BootstrapAdmin:Email"] ?? "master@omegainvest.com";
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = configuration["BootstrapAdmin:Name"] ?? "Administrador Master",
            IsActive = true
        };

        var password = configuration["BootstrapAdmin:Password"] ?? "Master@123";
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Falha ao criar usuário master: {errors}");
        }

        await userManager.AddToRoleAsync(user, "Master");
    }

    private static async Task EnsurePlanProjectionColumnsAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('investment_plans', 'TargetTermYears') IS NULL
                ALTER TABLE investment_plans ADD TargetTermYears decimal(5,2) NULL;
            """);
    }

    private static async Task EnsureInvestmentResponsibleColumnsAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('investments', 'ResponsibleUserId') IS NULL
                ALTER TABLE investments ADD ResponsibleUserId uniqueidentifier NULL;
            """);
    }
}
