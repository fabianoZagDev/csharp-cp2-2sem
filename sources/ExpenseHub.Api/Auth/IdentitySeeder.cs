using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Cria as roles do sistema e a única conta Admin inicial, de forma idempotente.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Executa o seed.
    /// </summary>
    /// <param name="services">Provedor raiz de serviços da aplicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa do seed.</returns>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        UserManager<IdentityUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        AdminSeedOptions options = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        foreach (string role in Roles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)));
            }
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        IdentityUser? admin = await userManager.FindByEmailAsync(options.Email);

        if (admin is null)
        {
            admin = new IdentityUser { UserName = options.Email, Email = options.Email, EmailConfirmed = true };
            EnsureSucceeded(await userManager.CreateAsync(admin, options.Password));
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, Roles.Admin));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha no seed do Identity: {errors}");
        }
    }
}
