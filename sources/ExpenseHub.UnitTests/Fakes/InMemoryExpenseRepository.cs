using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Repositório em memória para os testes, sem banco nem EF.
/// </summary>
public sealed class InMemoryExpenseRepository : IExpenseRepository
{
    private readonly List<Expense> _expenses = [];
    private readonly HashSet<int> _categoryIds = [1, 2, 3, 4, 5];

    /// <summary>
    /// Obtém quantas vezes <see cref="SaveChangesAsync"/> foi chamado.
    /// </summary>
    public int SaveChangesCalls { get; private set; }

    /// <summary>
    /// Obtém ou define a exceção lançada por <see cref="SaveChangesAsync"/>, simulando falha de persistência.
    /// </summary>
    public Exception? SaveFailure { get; set; }

    /// <summary>
    /// Obtém quantas vezes <see cref="ListHistoryAsync"/> foi chamado.
    /// </summary>
    public int ListHistoryCalls { get; private set; }

    /// <summary>
    /// Obtém os reembolsos armazenados.
    /// </summary>
    public IReadOnlyList<Expense> Expenses => _expenses;

    /// <inheritdoc />
    public Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_expenses.FirstOrDefault(expense => expense.Id == id));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Expense>> ListAsync(
        Expression<Func<Expense, bool>> filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IReadOnlyList<Expense> result = _expenses.Where(filter.Compile()).ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ExpenseHistory>> ListHistoryAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        ListHistoryCalls++;

        IReadOnlyList<ExpenseHistory> result = _expenses
            .Where(expense => expense.Id == expenseId)
            .SelectMany(expense => expense.History)
            .OrderBy(history => history.OccurredAtUtc)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_categoryIds.Contains(categoryId));
    }

    /// <inheritdoc />
    public void Add(Expense expense)
    {
        _expenses.Add(expense);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalls++;

        return SaveFailure is null ? Task.CompletedTask : Task.FromException(SaveFailure);
    }
}
