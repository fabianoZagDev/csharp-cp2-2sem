using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Errors;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Implementação das regras de negócio dos reembolsos.
/// </summary>
public sealed class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa o serviço.
    /// </summary>
    /// <param name="repository">Repositório de reembolsos.</param>
    /// <param name="timeProvider">Fonte de tempo do servidor.</param>
    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Expense> CreateAsync(
        CurrentUser user,
        CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        int categoryId = request.CategoryId.GetValueOrDefault();
        await EnsureCategoryExistsAsync(categoryId, cancellationToken);

        Expense expense = Expense.CreateDraft(
            user.Id,
            request.Description,
            request.Amount.GetValueOrDefault(),
            request.ExpenseDate.GetValueOrDefault(),
            categoryId,
            _timeProvider.GetUtcNow().UtcDateTime);

        _repository.Add(expense);
        await _repository.SaveChangesAsync(cancellationToken);

        return expense;
    }

    /// <inheritdoc />
    public async Task<Expense> UpdateAsync(
        CurrentUser user,
        Guid id,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        Expense expense = await FindOrThrowAsync(id, cancellationToken);
        ExpenseAccessPolicy.EnsureCanActAsOwner(user, expense);

        int categoryId = request.CategoryId.GetValueOrDefault();
        await EnsureCategoryExistsAsync(categoryId, cancellationToken);

        expense.UpdateDraft(
            user.Id,
            request.Description,
            request.Amount.GetValueOrDefault(),
            request.ExpenseDate.GetValueOrDefault(),
            categoryId,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _repository.SaveChangesAsync(cancellationToken);

        return expense;
    }

    /// <inheritdoc />
    public async Task<Expense> SubmitAsync(CurrentUser user, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        Expense expense = await FindOrThrowAsync(id, cancellationToken);
        ExpenseAccessPolicy.EnsureCanActAsOwner(user, expense);

        expense.Submit(user.Id, _timeProvider.GetUtcNow().UtcDateTime);
        await _repository.SaveChangesAsync(cancellationToken);

        return expense;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Expense>> ListAsync(CurrentUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        return _repository.ListAsync(ExpenseAccessPolicy.ReadableBy(user), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Expense> GetByIdAsync(CurrentUser user, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        Expense expense = await FindOrThrowAsync(id, cancellationToken);

        if (!ExpenseAccessPolicy.CanRead(user, expense))
        {
            throw new ResourceNotFoundException("Reembolso não encontrado.");
        }

        return expense;
    }

    private async Task<Expense> FindOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.FindAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Reembolso não encontrado.");
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (!await _repository.CategoryExistsAsync(categoryId, cancellationToken))
        {
            throw new DomainValidationException("A categoria informada não existe.");
        }
    }
}
