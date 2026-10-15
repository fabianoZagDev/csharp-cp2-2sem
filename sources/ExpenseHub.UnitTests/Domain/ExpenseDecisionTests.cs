using System;
using System.Linq;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Testes das decisões de aprovação e reprovação em <see cref="Expense"/>.
/// </summary>
[TestClass]
public sealed class ExpenseDecisionTests
{
    private const string Owner = "owner";
    private const string Approver = "approver";
    private const string ValidJustification = "Falta o comprovante fiscal.";

    private static readonly DateTime _decidedAt = new(2026, 10, 16, 9, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// Aprovar um reembolso enviado muda o estado e registra ator, horário e transição no histórico.
    /// </summary>
    [TestMethod]
    public void Approve_Submitted_BecomesApprovedWithHistory()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);

        expense.Approve(Approver, _decidedAt);

        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.AreEqual(_decidedAt, expense.UpdatedAtUtc);
        Assert.AreEqual(ExpenseAction.Approved, entry.Action);
        Assert.AreEqual(Approver, entry.ActorId);
        Assert.AreEqual(_decidedAt, entry.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Submitted, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Approved, entry.ToStatus);
        Assert.IsNull(expense.RejectionReason);
    }

    /// <summary>
    /// Só reembolsos enviados podem ser aprovados; os demais geram conflito sem histórico novo.
    /// </summary>
    /// <param name="status">Estado inicial.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void Approve_NotSubmitted_ThrowsStateConflictWithoutHistory(ExpenseStatus status)
    {
        Expense expense = ExpenseFactory.Create(Owner, status);
        int historyBefore = expense.History.Count;

        Assert.ThrowsExactly<StateConflictException>(() => expense.Approve(Approver, _decidedAt));

        Assert.AreEqual(status, expense.Status);
        Assert.HasCount(historyBefore, expense.History);
    }

    /// <summary>
    /// Aprovar duas vezes gera conflito e mantém uma única entrada Approved.
    /// </summary>
    [TestMethod]
    public void Approve_Twice_ThrowsStateConflictWithSingleApprovedEntry()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);
        expense.Approve(Approver, _decidedAt);

        Assert.ThrowsExactly<StateConflictException>(() => expense.Approve(Approver, _decidedAt));

        Assert.AreEqual(1, expense.History.Count(entry => entry.Action == ExpenseAction.Approved));
    }

    /// <summary>
    /// Reprovar um reembolso enviado grava a justificativa no reembolso e no histórico.
    /// </summary>
    [TestMethod]
    public void Reject_Submitted_BecomesRejectedWithReasonAndHistory()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);

        expense.Reject(Approver, ValidJustification, _decidedAt);

        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
        Assert.AreEqual(ValidJustification, expense.RejectionReason);
        Assert.AreEqual(ExpenseAction.Rejected, entry.Action);
        Assert.AreEqual(Approver, entry.ActorId);
        Assert.AreEqual(_decidedAt, entry.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Submitted, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Rejected, entry.ToStatus);
        Assert.AreEqual(ValidJustification, entry.Justification);
    }

    /// <summary>
    /// Justificativa vazia, só com espaços, com 9 ou com 501 caracteres é rejeitada e nada muda.
    /// </summary>
    /// <param name="justification">Justificativa testada.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("          ")]
    [DataRow("123456789")]
    [DataRow("   123456789   ")]
    public void Reject_JustificationTooShort_ThrowsDomainValidationWithoutChanges(string justification)
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);

        Assert.ThrowsExactly<DomainValidationException>(() => expense.Reject(Approver, justification, _decidedAt));

        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.IsNull(expense.RejectionReason);
        Assert.AreEqual(0, expense.History.Count(entry => entry.Action == ExpenseAction.Rejected));
    }

    /// <summary>
    /// Justificativa com 501 caracteres é rejeitada.
    /// </summary>
    [TestMethod]
    public void Reject_JustificationTooLong_ThrowsDomainValidation()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);

        Assert.ThrowsExactly<DomainValidationException>(
            () => expense.Reject(Approver, new string('a', 501), _decidedAt));

        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
    }

    /// <summary>
    /// Os limites de 10 e 500 caracteres são aceitos.
    /// </summary>
    /// <param name="length">Tamanho da justificativa.</param>
    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public void Reject_JustificationAtLimit_Succeeds(int length)
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);

        expense.Reject(Approver, new string('a', length), _decidedAt);

        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
        Assert.AreEqual(length, expense.RejectionReason!.Length);
    }

    /// <summary>
    /// A justificativa é gravada sem espaços nas pontas.
    /// </summary>
    [TestMethod]
    public void Reject_JustificationWithPadding_IsTrimmed()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Submitted);

        expense.Reject(Approver, "  " + ValidJustification + "  ", _decidedAt);

        Assert.AreEqual(ValidJustification, expense.RejectionReason);
        Assert.AreEqual(ValidJustification, expense.History.Last().Justification);
    }

    /// <summary>
    /// Só reembolsos enviados podem ser reprovados; os demais geram conflito sem histórico novo.
    /// </summary>
    /// <param name="status">Estado inicial.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void Reject_NotSubmitted_ThrowsStateConflictWithoutHistory(ExpenseStatus status)
    {
        Expense expense = ExpenseFactory.Create(Owner, status);
        int historyBefore = expense.History.Count;

        Assert.ThrowsExactly<StateConflictException>(
            () => expense.Reject(Approver, ValidJustification, _decidedAt));

        Assert.AreEqual(status, expense.Status);
        Assert.HasCount(historyBefore, expense.History);
    }
}
