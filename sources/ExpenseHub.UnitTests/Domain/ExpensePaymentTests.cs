using System;
using System.Linq;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Testes do pagamento simulado em <see cref="Expense"/>.
/// </summary>
[TestClass]
public sealed class ExpensePaymentTests
{
    private const string Owner = "owner";
    private const string Finance = "finance-1";

    private static readonly DateTime _paidAt = new(2026, 10, 17, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Pagar um reembolso aprovado cria o registro de pagamento com pagador, horário e valor.
    /// </summary>
    [TestMethod]
    public void MarkAsPaid_Approved_CreatesPaymentRecordWithActorTimeAndAmount()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Approved);

        expense.MarkAsPaid(Finance, _paidAt);

        Assert.AreEqual(ExpenseStatus.Paid, expense.Status);
        Assert.IsNotNull(expense.Payment);
        Assert.AreEqual(expense.Id, expense.Payment.ExpenseId);
        Assert.AreEqual(Finance, expense.Payment.PaidById);
        Assert.AreEqual(_paidAt, expense.Payment.PaidAtUtc);
        Assert.AreEqual(expense.Amount, expense.Payment.Amount);
        Assert.AreEqual(_paidAt, expense.UpdatedAtUtc);
    }

    /// <summary>
    /// Pagar registra a transição Approved → Paid no histórico com o ator e o horário.
    /// </summary>
    [TestMethod]
    public void MarkAsPaid_Approved_RecordsPaidHistory()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Approved);

        expense.MarkAsPaid(Finance, _paidAt);

        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Paid, entry.Action);
        Assert.AreEqual(Finance, entry.ActorId);
        Assert.AreEqual(_paidAt, entry.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Approved, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Paid, entry.ToStatus);
    }

    /// <summary>
    /// Só reembolsos aprovados podem ser pagos; os demais geram conflito sem pagamento nem histórico novo.
    /// </summary>
    /// <param name="status">Estado inicial.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Rejected)]
    public void MarkAsPaid_NotApproved_ThrowsStateConflictWithoutPayment(ExpenseStatus status)
    {
        Expense expense = ExpenseFactory.Create(Owner, status);
        int historyBefore = expense.History.Count;

        Assert.ThrowsExactly<StateConflictException>(() => expense.MarkAsPaid(Finance, _paidAt));

        Assert.AreEqual(status, expense.Status);
        Assert.IsNull(expense.Payment);
        Assert.HasCount(historyBefore, expense.History);
    }

    /// <summary>
    /// Pagar duas vezes gera conflito e preserva o primeiro pagamento e uma única entrada Paid.
    /// </summary>
    [TestMethod]
    public void MarkAsPaid_Twice_ThrowsStateConflictKeepingFirstPayment()
    {
        Expense expense = ExpenseFactory.Create(Owner, ExpenseStatus.Approved);
        expense.MarkAsPaid(Finance, _paidAt);
        PaymentRecord first = expense.Payment!;

        Assert.ThrowsExactly<StateConflictException>(() => expense.MarkAsPaid("finance-2", _paidAt.AddHours(1)));

        Assert.AreSame(first, expense.Payment);
        Assert.AreEqual(Finance, expense.Payment!.PaidById);
        Assert.AreEqual(1, expense.History.Count(entry => entry.Action == ExpenseAction.Paid));
    }
}
