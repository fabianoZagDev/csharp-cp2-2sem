using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Errors;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>
/// Testes de criação, edição e consulta de rascunho em <see cref="ExpenseService"/>.
/// </summary>
[TestClass]
public sealed class ExpenseServiceDraftTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly CurrentUser _employeeA = new("employee-a", [Roles.Employee]);
    private static readonly CurrentUser _employeeB = new("employee-b", [Roles.Employee]);

    /// <summary>
    /// O dono do reembolso vem do usuário autenticado e o estado inicial é Draft.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task CreateAsync_ValidRequest_UsesAuthenticatedOwnerAndDraftStatus()
    {
        var repository = new InMemoryExpenseRepository();
        ExpenseService service = CreateService(repository);

        Expense expense = await service.CreateAsync(_employeeA, NewCreateRequest(), CancellationToken.None);

        Assert.AreEqual("employee-a", expense.OwnerId);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual(_now.UtcDateTime, expense.CreatedAtUtc);
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Categoria inexistente gera erro de validação e nada é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task CreateAsync_UnknownCategory_ThrowsDomainValidation()
    {
        var repository = new InMemoryExpenseRepository();
        ExpenseService service = CreateService(repository);
        CreateExpenseRequest request = NewCreateRequest();
        request.CategoryId = 99;

        await Assert.ThrowsExactlyAsync<DomainValidationException>(
            () => service.CreateAsync(_employeeA, request, CancellationToken.None));
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// A edição pelo dono grava uma única vez e registra o histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateAsync_Owner_UpdatesDraftWithSingleSave()
    {
        var repository = new InMemoryExpenseRepository();
        ExpenseService service = CreateService(repository);
        Expense expense = await service.CreateAsync(_employeeA, NewCreateRequest(), CancellationToken.None);

        Expense updated = await service.UpdateAsync(_employeeA, expense.Id, NewUpdateRequest(), CancellationToken.None);

        Assert.AreEqual(25m, updated.Amount);
        Assert.HasCount(2, updated.History);
        Assert.AreEqual(2, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Outro Employee não enxerga o rascunho: a edição devolve 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateAsync_OtherEmployee_ThrowsNotFound()
    {
        var repository = new InMemoryExpenseRepository();
        ExpenseService service = CreateService(repository);
        Expense expense = await service.CreateAsync(_employeeA, NewCreateRequest(), CancellationToken.None);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.UpdateAsync(_employeeB, expense.Id, NewUpdateRequest(), CancellationToken.None));
        Assert.HasCount(1, expense.History);
    }

    /// <summary>
    /// Reembolso inexistente gera 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateAsync_UnknownId_ThrowsNotFound()
    {
        ExpenseService service = CreateService(new InMemoryExpenseRepository());

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.UpdateAsync(_employeeA, Guid.NewGuid(), NewUpdateRequest(), CancellationToken.None));
    }

    /// <summary>
    /// Editar fora do estado Draft gera 409 e não grava.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateAsync_NotDraft_ThrowsStateConflict()
    {
        var repository = new InMemoryExpenseRepository();
        ExpenseService service = CreateService(repository);
        Expense expense = await service.CreateAsync(_employeeA, NewCreateRequest(), CancellationToken.None);
        expense.Submit(_employeeA.Id, _now.UtcDateTime);

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.UpdateAsync(_employeeA, expense.Id, NewUpdateRequest(), CancellationToken.None));
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Editar com categoria inexistente gera erro de validação.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateAsync_UnknownCategory_ThrowsDomainValidation()
    {
        ExpenseService service = CreateService(new InMemoryExpenseRepository());
        Expense expense = await service.CreateAsync(_employeeA, NewCreateRequest(), CancellationToken.None);
        UpdateExpenseRequest request = NewUpdateRequest();
        request.CategoryId = 99;

        await Assert.ThrowsExactlyAsync<DomainValidationException>(
            () => service.UpdateAsync(_employeeA, expense.Id, request, CancellationToken.None));
    }

    /// <summary>
    /// Consultar o reembolso de outro Employee gera 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task GetByIdAsync_OtherEmployee_ThrowsNotFound()
    {
        ExpenseService service = CreateService(new InMemoryExpenseRepository());
        Expense expense = await service.CreateAsync(_employeeA, NewCreateRequest(), CancellationToken.None);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.GetByIdAsync(_employeeB, expense.Id, CancellationToken.None));
    }

    private static ExpenseService CreateService(InMemoryExpenseRepository repository)
    {
        return new ExpenseService(repository, new FixedTimeProvider(_now));
    }

    private static CreateExpenseRequest NewCreateRequest()
    {
        return new CreateExpenseRequest
        {
            Description = "Almoço com cliente",
            Amount = 20m,
            ExpenseDate = new DateOnly(2026, 10, 14),
            CategoryId = 1,
        };
    }

    private static UpdateExpenseRequest NewUpdateRequest()
    {
        return new UpdateExpenseRequest
        {
            Description = "Almoço com cliente X",
            Amount = 25m,
            ExpenseDate = new DateOnly(2026, 10, 14),
            CategoryId = 1,
        };
    }
}
