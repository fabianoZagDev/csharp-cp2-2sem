using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Acesso a dados dos reembolsos.
/// </summary>
public interface IExpenseRepository
{
    /// <summary>
    /// Busca um reembolso rastreado pelo contexto, com o registro de pagamento.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso, ou <see langword="null"/> quando não existe.</returns>
    Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os reembolsos que atendem ao filtro, aplicado na consulta ao banco.
    /// </summary>
    /// <param name="filter">Filtro traduzível pelo provedor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos encontrados, sem rastreamento.</returns>
    Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken);

    /// <summary>
    /// Lista o histórico de um reembolso em ordem cronológica, com o filtro aplicado na consulta ao banco.
    /// </summary>
    /// <param name="expenseId">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>As entradas do histórico, sem rastreamento.</returns>
    Task<IReadOnlyList<ExpenseHistory>> ListHistoryAsync(Guid expenseId, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se a categoria existe.
    /// </summary>
    /// <param name="categoryId">Identificador da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> quando a categoria existe.</returns>
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Marca um novo reembolso para inclusão.
    /// </summary>
    /// <param name="expense">Reembolso a incluir.</param>
    void Add(Expense expense);

    /// <summary>
    /// Grava as alterações pendentes em uma única transação.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa da gravação.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
