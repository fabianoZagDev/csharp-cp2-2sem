using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>
/// Testes de visibilidade e ownership de <see cref="ExpenseAccessPolicy"/>.
/// </summary>
[TestClass]
public sealed class ExpenseAccessPolicyTests
{
    private const string Owner = "owner";
    private const string Other = "other";

    /// <summary>
    /// Employee vê apenas os próprios reembolsos, em qualquer estado.
    /// </summary>
    [TestMethod]
    public void ReadableBy_Employee_SeesOnlyOwnExpenses()
    {
        var user = new CurrentUser(Owner, [Roles.Employee]);
        List<Expense> all = AllStatusesFor(Owner).Concat(AllStatusesFor(Other)).ToList();

        List<Expense> visible = all.Where(ExpenseAccessPolicy.ReadableBy(user).Compile()).ToList();

        Assert.HasCount(5, visible);
        Assert.IsTrue(visible.All(expense => expense.OwnerId == Owner));
    }

    /// <summary>
    /// Approver vê somente os reembolsos enviados, inclusive de outros usuários.
    /// </summary>
    [TestMethod]
    public void ReadableBy_Approver_SeesOnlySubmitted()
    {
        var user = new CurrentUser("approver", [Roles.Approver]);

        List<Expense> visible = AllStatusesFor(Other).Where(ExpenseAccessPolicy.ReadableBy(user).Compile()).ToList();

        Assert.HasCount(1, visible);
        Assert.AreEqual(ExpenseStatus.Submitted, visible[0].Status);
    }

    /// <summary>
    /// Finance vê somente aprovados e pagos.
    /// </summary>
    [TestMethod]
    public void ReadableBy_Finance_SeesOnlyApprovedAndPaid()
    {
        var user = new CurrentUser("finance", [Roles.Finance]);

        List<ExpenseStatus> statuses = AllStatusesFor(Other)
            .Where(ExpenseAccessPolicy.ReadableBy(user).Compile())
            .Select(expense => expense.Status)
            .ToList();

        CollectionAssert.AreEquivalent(new[] { ExpenseStatus.Approved, ExpenseStatus.Paid }, statuses);
    }

    /// <summary>
    /// Auditor vê todos os reembolsos.
    /// </summary>
    [TestMethod]
    public void ReadableBy_Auditor_SeesEverything()
    {
        var user = new CurrentUser("auditor", [Roles.Auditor]);

        List<Expense> visible = AllStatusesFor(Other).Where(ExpenseAccessPolicy.ReadableBy(user).Compile()).ToList();

        Assert.HasCount(5, visible);
    }

    /// <summary>
    /// Usuário sem role, ou apenas Admin, não vê nada.
    /// </summary>
    /// <param name="role">Role do usuário, vazia para nenhuma.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(Roles.Admin)]
    public void ReadableBy_NoFunctionalRole_SeesNothing(string role)
    {
        string[] roles = role.Length == 0 ? [] : [role];
        var user = new CurrentUser(Owner, roles);

        List<Expense> visible = AllStatusesFor(Owner).Where(ExpenseAccessPolicy.ReadableBy(user).Compile()).ToList();

        Assert.IsEmpty(visible);
    }

    /// <summary>
    /// Quem acumula roles recebe a união das visibilidades.
    /// </summary>
    [TestMethod]
    public void ReadableBy_EmployeeAndApprover_SeesUnion()
    {
        var user = new CurrentUser(Owner, [Roles.Employee, Roles.Approver]);
        List<Expense> all = AllStatusesFor(Owner).Concat(AllStatusesFor(Other)).ToList();

        List<Expense> visible = all.Where(ExpenseAccessPolicy.ReadableBy(user).Compile()).ToList();

        Assert.HasCount(6, visible);
    }

    /// <summary>
    /// Agir como dono de um reembolso fora do escopo gera 404.
    /// </summary>
    [TestMethod]
    public void EnsureCanActAsOwner_OutOfScope_ThrowsNotFound()
    {
        var user = new CurrentUser(Other, [Roles.Employee]);
        Expense expense = ExpenseFactory.Create(Owner);

        Assert.ThrowsExactly<ResourceNotFoundException>(() => ExpenseAccessPolicy.EnsureCanActAsOwner(user, expense));
    }

    /// <summary>
    /// Quem pode ler mas não é o dono (Auditor) recebe 403.
    /// </summary>
    [TestMethod]
    public void EnsureCanActAsOwner_ReaderThatIsNotOwner_ThrowsForbidden()
    {
        var user = new CurrentUser("auditor", [Roles.Auditor]);
        Expense expense = ExpenseFactory.Create(Owner);

        Assert.ThrowsExactly<ForbiddenOperationException>(() => ExpenseAccessPolicy.EnsureCanActAsOwner(user, expense));
    }

    /// <summary>
    /// O dono passa na checagem.
    /// </summary>
    [TestMethod]
    public void EnsureCanActAsOwner_Owner_DoesNotThrow()
    {
        var user = new CurrentUser(Owner, [Roles.Employee]);

        ExpenseAccessPolicy.EnsureCanActAsOwner(user, ExpenseFactory.Create(Owner));
    }

    /// <summary>
    /// Approver que também é Employee não decide o próprio reembolso enviado.
    /// </summary>
    [TestMethod]
    public void EnsureCanDecide_OwnerWithEmployeeAndApprover_ThrowsForbidden()
    {
        var user = new CurrentUser(Owner, [Roles.Employee, Roles.Approver]);
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);

        Assert.ThrowsExactly<ForbiddenOperationException>(() => ExpenseAccessPolicy.EnsureCanDecide(user, expense));
    }

    /// <summary>
    /// Finance que também é Employee não paga o próprio reembolso aprovado.
    /// </summary>
    [TestMethod]
    public void EnsureCanDecide_OwnerWithEmployeeAndFinance_ThrowsForbidden()
    {
        var user = new CurrentUser(Owner, [Roles.Employee, Roles.Finance]);
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Approved);

        Assert.ThrowsExactly<ForbiddenOperationException>(() => ExpenseAccessPolicy.EnsureCanDecide(user, expense));
    }

    /// <summary>
    /// Rascunho de outro usuário não é visível ao Approver: 404.
    /// </summary>
    [TestMethod]
    public void EnsureCanDecide_OthersDraft_ThrowsNotFound()
    {
        var user = new CurrentUser("approver", [Roles.Approver]);
        Expense expense = ExpenseFactory.Create(Owner);

        Assert.ThrowsExactly<ResourceNotFoundException>(() => ExpenseAccessPolicy.EnsureCanDecide(user, expense));
    }

    /// <summary>
    /// Approver que não é dono passa; o estado é decidido depois pela entidade.
    /// </summary>
    [TestMethod]
    public void EnsureCanDecide_ApproverThatIsNotOwner_DoesNotThrow()
    {
        var user = new CurrentUser("approver", [Roles.Approver]);

        ExpenseAccessPolicy.EnsureCanDecide(user, ExpenseFactory.Create(Owner, ExpenseStatus.Submitted));
    }

    private static IEnumerable<Expense> AllStatusesFor(string ownerId)
    {
        return Enum.GetValues<ExpenseStatus>().Select(status => ExpenseFactory.Create(ownerId, status));
    }
}
