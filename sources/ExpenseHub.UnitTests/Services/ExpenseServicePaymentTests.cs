using System;
using System.Collections.Generic;
using System.Linq;
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
/// Testes de pagamento e histórico em <see cref="ExpenseService"/>.
/// </summary>
[TestClass]
public sealed class ExpenseServicePaymentTests
{
    private const string OwnerId = "employee-a";

    private static readonly DateTimeOffset _now = new(2026, 10, 17, 14, 0, 0, TimeSpan.Zero);
    private static readonly CurrentUser _finance = new("finance-1", [Roles.Finance]);
    private static readonly CurrentUser _auditor = new("auditor-1", [Roles.Auditor]);

    /// <summary>
    /// Finance que não é dono paga; o pagamento guarda pagador, horário do servidor e valor, com uma única gravação.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayAsync_FinanceNotOwner_CreatesPaymentWithServerActorAndTime()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Approved);

        Expense paid = await service.PayAsync(_finance, expense.Id, CancellationToken.None);

        Assert.AreEqual(ExpenseStatus.Paid, paid.Status);
        Assert.AreEqual(_finance.Id, paid.Payment!.PaidById);
        Assert.AreEqual(_now.UtcDateTime, paid.Payment.PaidAtUtc);
        Assert.AreEqual(paid.Amount, paid.Payment.Amount);
        Assert.AreEqual(ExpenseAction.Paid, paid.History.Last().Action);
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Submitted, Rejected e Paid não podem ser pagos: 409 sem novo pagamento, histórico ou gravação.
    /// </summary>
    /// <param name="status">Estado inicial.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public async Task PayAsync_NotApproved_ThrowsConflictWithoutSaving(ExpenseStatus status)
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(status);
        int historyBefore = expense.History.Count;
        PaymentRecord? paymentBefore = expense.Payment;

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.PayAsync(_finance, expense.Id, CancellationToken.None));

        Assert.AreEqual(status, expense.Status);
        Assert.AreSame(paymentBefore, expense.Payment);
        Assert.HasCount(historyBefore, expense.History);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// O dono não paga o próprio reembolso, mesmo tendo a role Finance: 403 sem pagamento.
    /// </summary>
    /// <param name="roles">Roles do dono, separadas por vírgula.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(Roles.Finance)]
    [DataRow(Roles.Employee + "," + Roles.Finance)]
    public async Task PayAsync_OwnerWithFinanceRole_ThrowsForbiddenWithoutPayment(string roles)
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Approved);

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => service.PayAsync(new CurrentUser(OwnerId, roles.Split(',')), expense.Id, CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.IsNull(expense.Payment);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Rascunho de outro usuário não existe para o Finance: 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayAsync_OthersDraft_ThrowsNotFound()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Draft);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.PayAsync(_finance, expense.Id, CancellationToken.None));

        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Pagar duas vezes gera 409 na segunda chamada sem duplicar pagamento nem histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayAsync_Twice_ThrowsConflictWithoutDuplicating()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Approved);
        await service.PayAsync(_finance, expense.Id, CancellationToken.None);
        PaymentRecord first = expense.Payment!;

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.PayAsync(new CurrentUser("finance-2", [Roles.Finance]), expense.Id, CancellationToken.None));

        Assert.AreSame(first, expense.Payment);
        Assert.AreEqual(1, expense.History.Count(entry => entry.Action == ExpenseAction.Paid));
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Falha de persistência (ex.: concorrência) chega ao chamador como 409, depois de uma única tentativa de gravação.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayAsync_SaveFails_PropagatesConflictAfterSingleSave()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Approved);
        repository.SaveFailure = new StateConflictException("O reembolso foi alterado por outra operação.");

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.PayAsync(_finance, expense.Id, CancellationToken.None));

        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Outro Employee não vê o histórico de um reembolso alheio: 404 sem consultar o histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task GetHistoryAsync_OtherEmployee_ThrowsNotFound()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Draft);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.GetHistoryAsync(new CurrentUser("employee-b", [Roles.Employee]), expense.Id, CancellationToken.None));

        Assert.AreEqual(0, repository.ListHistoryCalls);
    }

    /// <summary>
    /// Approver não vê o histórico de um reembolso fora do seu escopo (já aprovado): 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task GetHistoryAsync_ApproverOnApproved_ThrowsNotFound()
    {
        (ExpenseService service, _, Expense expense) = Arrange(ExpenseStatus.Approved);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.GetHistoryAsync(new CurrentUser("approver-2", [Roles.Approver]), expense.Id, CancellationToken.None));
    }

    /// <summary>
    /// Auditor lê o histórico de qualquer reembolso, inclusive rascunho alheio.
    /// </summary>
    /// <param name="status">Estado do reembolso.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public async Task GetHistoryAsync_Auditor_ReadsAnyHistory(ExpenseStatus status)
    {
        (ExpenseService service, _, Expense expense) = Arrange(status);

        IReadOnlyList<ExpenseHistory> history = await service.GetHistoryAsync(_auditor, expense.Id, CancellationToken.None);

        Assert.HasCount(expense.History.Count, history);
        Assert.AreEqual(status, history[^1].ToStatus);
    }

    /// <summary>
    /// O fluxo completo gera Created → Updated → Submitted → Approved → Paid, cada operação com uma gravação.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task FullFlow_CreateToPaid_RecordsOrderedHistoryWithOneSavePerOperation()
    {
        var repository = new InMemoryExpenseRepository();
        var service = new ExpenseService(repository, new FixedTimeProvider(_now));
        var employee = new CurrentUser(OwnerId, [Roles.Employee]);
        var approver = new CurrentUser("approver-1", [Roles.Approver]);
        DateOnly expenseDate = DateOnly.FromDateTime(_now.UtcDateTime);

        Expense expense = await service.CreateAsync(
            employee,
            new CreateExpenseRequest { Description = "Almoço com cliente", Amount = 80m, ExpenseDate = expenseDate, CategoryId = 1 },
            CancellationToken.None);
        int[] savesPerStep = new int[5];
        savesPerStep[0] = repository.SaveChangesCalls;
        await service.UpdateAsync(
            employee,
            expense.Id,
            new UpdateExpenseRequest { Description = "Almoço com cliente (ajuste)", Amount = 85m, ExpenseDate = expenseDate, CategoryId = 1 },
            CancellationToken.None);
        savesPerStep[1] = repository.SaveChangesCalls;
        await service.SubmitAsync(employee, expense.Id, CancellationToken.None);
        savesPerStep[2] = repository.SaveChangesCalls;
        await service.ApproveAsync(approver, expense.Id, CancellationToken.None);
        savesPerStep[3] = repository.SaveChangesCalls;
        await service.PayAsync(_finance, expense.Id, CancellationToken.None);
        savesPerStep[4] = repository.SaveChangesCalls;

        IReadOnlyList<ExpenseHistory> history = await service.GetHistoryAsync(_auditor, expense.Id, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { ExpenseAction.Created, ExpenseAction.Updated, ExpenseAction.Submitted, ExpenseAction.Approved, ExpenseAction.Paid },
            history.Select(entry => entry.Action).ToArray());
        CollectionAssert.AreEqual(
            new[] { OwnerId, OwnerId, OwnerId, approver.Id, _finance.Id },
            history.Select(entry => entry.ActorId).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 5).ToArray(), savesPerStep);
        Assert.AreEqual(85m, expense.Payment!.Amount);
    }

    private static (ExpenseService Service, InMemoryExpenseRepository Repository, Expense Expense) Arrange(ExpenseStatus status)
    {
        var repository = new InMemoryExpenseRepository();
        Expense expense = ExpenseFactory.Create(OwnerId, status);
        repository.Add(expense);

        return (new ExpenseService(repository, new FixedTimeProvider(_now)), repository, expense);
    }
}
