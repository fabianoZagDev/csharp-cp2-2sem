using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Implementação Entity Framework Core de <see cref="IExpenseRepository"/>.
/// </summary>
public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Inicializa o repositório.
    /// </summary>
    /// <param name="dbContext">Contexto do banco.</param>
    public ExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Expenses
            .Include(expense => expense.Payment)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Expense>> ListAsync(
        Expression<Func<Expense, bool>> filter,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Expenses
            .AsNoTracking()
            .Include(expense => expense.Payment)
            .Where(filter)
            .OrderByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpenseHistory>> ListHistoryAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        return await _dbContext.ExpenseHistories
            .AsNoTracking()
            .Where(history => history.ExpenseId == expenseId)
            .OrderBy(history => history.OccurredAtUtc)
            .ThenBy(history => history.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return _dbContext.ExpenseCategories.AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Expense expense)
    {
        _dbContext.Expenses.Add(expense);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new StateConflictException(
                "O reembolso foi alterado por outra operação. Tente novamente.",
                exception);
        }
    }
}
