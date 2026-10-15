using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>
/// Testes de envio, listagem e isolamento entre usuários em <see cref="ExpenseService"/>.
/// </summary>
[TestClass]
public sealed class ExpenseServiceSubmitTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly CurrentUser _employeeA = new("employee-a", [Roles.Employee]);
    private static readonly CurrentUser _employeeB = new("employee-b", [Roles.Employee]);

    /// <summary>
    /// O envio válido muda o estado para Submitted, registra o histórico e grava uma única vez.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task SubmitAsync_OwnerDraft_BecomesSubmittedWithHistory()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(_employeeA);

        Expense submitted = await service.SubmitAsync(_employeeA, expense.Id, CancellationToken.None);

        ExpenseHistory entry = submitted.History.Last();
        Assert.AreEqual(ExpenseStatus.Submitted, submitted.Status);
        Assert.AreEqual(ExpenseAction.Submitted, entry.Action);
        Assert.AreEqual(ExpenseStatus.Draft, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, entry.ToStatus);
        Assert.AreEqual("employee-a", entry.ActorId);
        Assert.AreEqual(_now.UtcDateTime, entry.OccurredAtUtc);
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Enviar duas vezes gera 409 e mantém uma única entrada Submitted.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task SubmitAsync_SubmittedTwice_ThrowsConflictWithSingleHistoryEntry()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(_employeeA);
        await service.SubmitAsync(_employeeA, expense.Id, CancellationToken.None);

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.SubmitAsync(_employeeA, expense.Id, CancellationToken.None));

        Assert.AreEqual(1, expense.History.Count(entry => entry.Action == ExpenseAction.Submitted));
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Outro Employee tentando enviar recebe 404 e nada muda.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task SubmitAsync_OtherEmployee_ThrowsNotFound()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(_employeeA);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.SubmitAsync(_employeeB, expense.Id, CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Listar devolve só o que o usuário pode ler: Employee B não vê o reembolso de A.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ListAsync_TwoEmployees_EachSeesOnlyOwn()
    {
        var repository = new InMemoryExpenseRepository();
        repository.Add(ExpenseFactory.Create("employee-a"));
        repository.Add(ExpenseFactory.Create("employee-b"));
        repository.Add(ExpenseFactory.Create("employee-b"));
        var service = new ExpenseService(repository, new FixedTimeProvider(_now));

        IReadOnlyList<Expense> forA = await service.ListAsync(_employeeA, CancellationToken.None);
        IReadOnlyList<Expense> forB = await service.ListAsync(_employeeB, CancellationToken.None);

        Assert.HasCount(1, forA);
        Assert.HasCount(2, forB);
    }

    /// <summary>
    /// Auditor lista tudo; usuário sem role não lista nada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ListAsync_AuditorAndNoRole_SeesAllAndNothing()
    {
        var repository = new InMemoryExpenseRepository();
        repository.Add(ExpenseFactory.Create("employee-a"));
        repository.Add(ExpenseFactory.Create("employee-b", ExpenseStatus.Paid));
        var service = new ExpenseService(repository, new FixedTimeProvider(_now));

        IReadOnlyList<Expense> auditor = await service.ListAsync(new CurrentUser("x", [Roles.Auditor]), CancellationToken.None);
        IReadOnlyList<Expense> none = await service.ListAsync(new CurrentUser("y", []), CancellationToken.None);

        Assert.HasCount(2, auditor);
        Assert.IsEmpty(none);
    }

    /// <summary>
    /// Detalhe fora do escopo do Approver (rascunho) gera 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task GetByIdAsync_ApproverOnDraft_ThrowsNotFound()
    {
        (ExpenseService service, _, Expense expense) = Arrange(_employeeA);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.GetByIdAsync(new CurrentUser("approver", [Roles.Approver]), expense.Id, CancellationToken.None));
    }

    private static (ExpenseService Service, InMemoryExpenseRepository Repository, Expense Expense) Arrange(CurrentUser owner)
    {
        var repository = new InMemoryExpenseRepository();
        Expense expense = ExpenseFactory.Create(owner.Id);
        repository.Add(expense);

        return (new ExpenseService(repository, new FixedTimeProvider(_now)), repository, expense);
    }
}
