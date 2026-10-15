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

        return Task.CompletedTask;
    }
}
