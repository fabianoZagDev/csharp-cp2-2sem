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
/// Negações de ownership e a ordem das checagens (escopo → dono → estado) em <see cref="ExpenseService"/>.
/// </summary>
[TestClass]
public sealed class ExpenseServiceOwnershipTests
{
    private const string OwnerId = "employee-a";

    private static readonly DateTimeOffset _now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Quem consegue ler o reembolso, mas não é o dono, recebe 403 ao editar, e o 403 vem antes do 409 de estado.
    /// </summary>
    /// <param name="roles">Roles do usuário, separadas por vírgula.</param>
    /// <param name="status">Estado do reembolso, dentro do escopo de leitura do usuário.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(Roles.Auditor, ExpenseStatus.Draft)]
    [DataRow(Roles.Employee + "," + Roles.Approver, ExpenseStatus.Submitted)]
    [DataRow(Roles.Employee + "," + Roles.Finance, ExpenseStatus.Approved)]
    public async Task UpdateAsync_ReaderNotOwner_ThrowsForbiddenBeforeStateCheck(string roles, ExpenseStatus status)
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(status);

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => service.UpdateAsync(User(roles), expense.Id, NewUpdateRequest(), CancellationToken.None));

        Assert.AreEqual(status, expense.Status);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Auditor lê o rascunho alheio, mas não envia: 403 sem histórico novo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task SubmitAsync_AuditorOnOthersDraft_ThrowsForbidden()
    {
        (ExpenseService service, InMemoryExpenseRepository repository, Expense expense) = Arrange(ExpenseStatus.Draft);

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => service.SubmitAsync(User(Roles.Auditor), expense.Id, CancellationToken.None));

        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.HasCount(1, expense.History);
        Assert.AreEqual(0, repository.SaveChangesCalls);
    }

    /// <summary>
    /// Fora do escopo de leitura, a resposta é 404 mesmo quando o estado também seria inválido.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateAsync_OtherEmployeeOnSubmitted_ThrowsNotFoundBeforeStateCheck()
    {
        (ExpenseService service, _, Expense expense) = Arrange(ExpenseStatus.Submitted);

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => service.UpdateAsync(User(Roles.Employee, "employee-b"), expense.Id, NewUpdateRequest(), CancellationToken.None));
    }

    /// <summary>
    /// O dono que também é Auditor (lê tudo) continua sem poder decidir o próprio reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveAsync_OwnerWithApproverAndAuditor_ThrowsForbidden()
    {
        (ExpenseService service, _, Expense expense) = Arrange(ExpenseStatus.Submitted);

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => service.ApproveAsync(User(Roles.Approver + "," + Roles.Auditor, OwnerId), expense.Id, CancellationToken.None));
    }

    /// <summary>
    /// Detalhe fora do escopo do Finance (ainda enviado) gera 404; dentro do escopo (aprovado) devolve o reembolso.
    /// </summary>
    /// <param name="status">Estado do reembolso.</param>
    /// <param name="visible">Se o Finance deve enxergar o reembolso.</param>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft, false)]
    [DataRow(ExpenseStatus.Submitted, false)]
    [DataRow(ExpenseStatus.Rejected, false)]
    [DataRow(ExpenseStatus.Approved, true)]
    [DataRow(ExpenseStatus.Paid, true)]
    public async Task GetByIdAsync_Finance_SeesOnlyApprovedAndPaid(ExpenseStatus status, bool visible)
    {
        (ExpenseService service, _, Expense expense) = Arrange(status);
        CurrentUser finance = User(Roles.Finance);

        if (visible)
        {
            Expense found = await service.GetByIdAsync(finance, expense.Id, CancellationToken.None);
            Assert.AreEqual(expense.Id, found.Id);
        }
        else
        {
            await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
                () => service.GetByIdAsync(finance, expense.Id, CancellationToken.None));
        }
    }

    private static (ExpenseService Service, InMemoryExpenseRepository Repository, Expense Expense) Arrange(ExpenseStatus status)
    {
        var repository = new InMemoryExpenseRepository();
        Expense expense = ExpenseFactory.Create(OwnerId, status);
        repository.Add(expense);

        return (new ExpenseService(repository, new FixedTimeProvider(_now)), repository, expense);
    }

    private static CurrentUser User(string roles, string id = "someone-else")
    {
        return new CurrentUser(id, roles.Split(','));
    }

    private static UpdateExpenseRequest NewUpdateRequest()
    {
        return new UpdateExpenseRequest
        {
            Description = "Almoço com cliente revisado",
            Amount = 30m,
            ExpenseDate = DateOnly.FromDateTime(_now.UtcDateTime),
            CategoryId = 2,
        };
    }
}
