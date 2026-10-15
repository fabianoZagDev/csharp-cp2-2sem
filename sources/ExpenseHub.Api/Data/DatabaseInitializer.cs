using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Cria ou atualiza o banco aplicando as migrations pendentes na inicialização.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Aplica as migrations pendentes.
    /// </summary>
    /// <param name="services">Provedor raiz de serviços da aplicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa da inicialização.</returns>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
