using System;
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
/// Testes de aprovação e reprovação em <see cref="ExpenseService"/>.
/// </summary>
[TestClass]
public sealed class ExpenseServiceDecisionTests
{
    private const string OwnerId = "employee-a";
    private const string ValidJustification = "Falta o comprovante fiscal.";

    private static readonly DateTimeOffset _now = new(2026, 10, 16, 9, 30, 0, TimeSpan.Zero);
    private static readonly CurrentUser _approver = new("approver-1", [Roles.Approver]);

    /// <summary>
    /// Approver que não é dono aprova; ator e horário vêm do servidor e há uma única gravação.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveAsync_ApproverNotOwner_ApprovesWithServerActorAndTime()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Submitted);

        Expense approved = await service.ApproveAsync(_approver, expense.Id, CancellationToken.None);

        ExpenseHistory entry = approved.History.Last();
        Assert.AreEqual(ExpenseStatus.Approved, approved.Status);
        Assert.AreEqual(ExpenseAction.Approved, entry.Action);
        Assert.AreEqual(_approver.Id, entry.ActorId);
        Assert.AreEqual(_now.UtcDateTime, entry.OccurredAtUtc);
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Approver que não é dono reprova com justificativa; ator e horário vêm do servidor.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task RejectAsync_ApproverNotOwner_RejectsWithJustification()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Submitted);

        Expense rejected = await service.RejectAsync(_approver, expense.Id, Justify(ValidJustification), CancellationToken.None);

        ExpenseHistory entry = rejected.History.Last();
        Assert.AreEqual(ExpenseStatus.Rejected, rejected.Status);
        Assert.AreEqual(ValidJustification, rejected.RejectionReason);
        Assert.AreEqual(ExpenseAction.Rejected, entry.Action);
        Assert.AreEqual(ValidJustification, entry.Justification);
        Assert.AreEqual(_approver.Id, entry.ActorId);
        Assert.AreEqual(_now.UtcDateTime, entry.OccurredAtUtc);
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// O dono não aprova o próprio reembolso, mesmo tendo a role Approver: 403 sem histórico novo.
    /// </summary>
    /// <param name="roles">Roles do dono, separadas por vírgula.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(Roles.Approver)]
    [DataRow(Roles.Employee + "," + Roles.Approver)]
    public async Task ApproveAsync_OwnerWithApproverRole_ThrowsForbiddenWithoutHistory(string roles)
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Submitted);
        int historyBefore = expense.History.Count;

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => service.ApproveAsync(Owner(roles), expense.Id, CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.HasCount(historyBefore, expense.History);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// O dono não reprova o próprio reembolso, mesmo tendo a role Approver: 403 sem histórico novo.
    /// </summary>
    /// <param name="roles">Roles do dono, separadas por vírgula.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(Roles.Approver)]
    [DataRow(Roles.Employee + "," + Roles.Approver)]
    public async Task RejectAsync_OwnerWithApproverRole_ThrowsForbiddenWithoutHistory(string roles)
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Submitted);
        int historyBefore = expense.History.Count;

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => service.RejectAsync(Owner(roles), expense.Id, Justify(ValidJustification), CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.IsNull(expense.RejectionReason);
        Assert.HasCount(historyBefore, expense.History);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Rascunho de outro usuário não existe para o Approver: 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveAsync_OthersDraft_ThrowsNotFound()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Draft);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.ApproveAsync(_approver, expense.Id, CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Rascunho de outro usuário não existe para o Approver na reprovação: 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task RejectAsync_OthersDraft_ThrowsNotFound()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Draft);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.RejectAsync(_approver, expense.Id, Justify(ValidJustification), CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Identificador inexistente gera 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveAsync_UnknownId_ThrowsNotFound()
    {
        (ExpenseService service, _, _) = Arrange(ExpenseStatus.Submitted);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.ApproveAsync(_approver, Guid.NewGuid(), CancellationToken.None));
    }

    /// <summary>
    /// Reembolsos já decididos não recebem nova aprovação: 409 sem duplicar histórico.
    /// </summary>
    /// <param name="status">Estado inicial.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public async Task ApproveAsync_AlreadyDecided_ThrowsConflictWithoutHistory(ExpenseStatus status)
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(status);
        int historyBefore = expense.History.Count;

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.ApproveAsync(_approver, expense.Id, CancellationToken.None));

        Assert.AreEqual(status, expense.Status);
        Assert.HasCount(historyBefore, expense.History);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Reembolsos já decididos não recebem reprovação: 409 sem duplicar histórico.
    /// </summary>
    /// <param name="status">Estado inicial.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public async Task RejectAsync_AlreadyDecided_ThrowsConflictWithoutHistory(ExpenseStatus status)
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(status);
        int historyBefore = expense.History.Count;

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.RejectAsync(_approver, expense.Id, Justify(ValidJustification), CancellationToken.None));

        Assert.AreEqual(status, expense.Status);
        Assert.HasCount(historyBefore, expense.History);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Aprovar duas vezes gera 409 na segunda chamada e mantém uma única entrada Approved.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveAsync_Twice_ThrowsConflictWithSingleApprovedEntry()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Submitted);
        await service.ApproveAsync(_approver, expense.Id, CancellationToken.None);

        await Assert.ThrowsExactlyAsync<StateConflictException>(
            () => service.ApproveAsync(_approver, expense.Id, CancellationToken.None));

        Assert.AreEqual(1, expense.History.Count(entry => entry.Action == ExpenseAction.Approved));
        Assert.AreEqual(1, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Justificativa inválida gera 400 e não grava nada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task RejectAsync_ShortJustification_ThrowsDomainValidationWithoutSaving()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Submitted);

        await Assert.ThrowsExactlyAsync<DomainValidationException>(
            () => service.RejectAsync(_approver, expense.Id, Justify("curta"), CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    private static (ExpenseService Service, InMemoryExpenseRepository Repository, Expense Expense) Arrange(ExpenseStatus status)
    {
        var repository = new InMemoryExpenseRepository();
        Expense expense = ExpenseFactory.Create(OwnerId, status);
        repository.Add(expense);

        return (new ExpenseService(repository, new FixedTimeProvider(_now)), repository, expense);
    }

    private static CurrentUser Owner(string roles)
    {
        return new CurrentUser(OwnerId, roles.Split(','));
    }

    private static RejectExpenseRequest Justify(string justification)
    {
        return new RejectExpenseRequest { Justification = justification };
    }
}
