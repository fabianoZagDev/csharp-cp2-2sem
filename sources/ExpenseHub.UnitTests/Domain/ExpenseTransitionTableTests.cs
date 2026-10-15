using System;
using System.Linq;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Tabela completa de transições de <see cref="Expense"/>: todos os estados contra todas as ações.
/// </summary>
[TestClass]
public sealed class ExpenseTransitionTableTests
{
    private const string Owner = "owner";
    private const string Actor = "actor";

    private static readonly DateTime _now = new(2026, 10, 18, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Transições permitidas mudam para o estado esperado e registram uma entrada de histórico com a ação,
    /// o ator, o horário e os estados anterior e posterior.
    /// </summary>
    /// <param name="from">Estado inicial.</param>
    /// <param name="action">Ação executada.</param>
    /// <param name="to">Estado esperado.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Updated, ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Submitted, ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Approved, ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Rejected, ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Paid, ExpenseStatus.Paid)]
    public void Transition_Allowed_ChangesStatusAndRecordsHistory(ExpenseStatus from, ExpenseAction action, ExpenseStatus to)
    {
        Expense expense = ExpenseFactory.Create(Owner, from);
        int historyBefore = expense.History.Count;

        Execute(expense, action);

        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(to, expense.Status);
        Assert.HasCount(historyBefore + 1, expense.History);
        Assert.AreEqual(action, entry.Action);
        Assert.AreEqual(Actor, entry.ActorId);
        Assert.AreEqual(_now, entry.OccurredAtUtc);
        Assert.AreEqual(from, entry.FromStatus);
        Assert.AreEqual(to, entry.ToStatus);
    }

    /// <summary>
    /// Todas as demais combinações de estado e ação geram 409 sem mudar o estado nem o histórico.
    /// </summary>
    /// <param name="from">Estado inicial.</param>
    /// <param name="action">Ação executada.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Paid)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Updated)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Paid)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Updated)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Updated)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Paid)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Updated)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Paid)]
    public void Transition_NotAllowed_ThrowsStateConflictWithoutChanges(ExpenseStatus from, ExpenseAction action)
    {
        Expense expense = ExpenseFactory.Create(Owner, from);
        int historyBefore = expense.History.Count;
        DateTime updatedBefore = expense.UpdatedAtUtc;

        Assert.ThrowsExactly<StateConflictException>(() => Execute(expense, action));

        Assert.AreEqual(from, expense.Status);
        Assert.HasCount(historyBefore, expense.History);
        Assert.AreEqual(updatedBefore, expense.UpdatedAtUtc);
    }

    /// <summary>
    /// Os estados finais (Rejected e Paid) não aceitam nenhuma ação.
    /// </summary>
    /// <param name="final">Estado final.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void Transition_FinalState_RejectsEveryAction(ExpenseStatus final)
    {
        foreach (ExpenseAction action in Enum.GetValues<ExpenseAction>().Where(action => action != ExpenseAction.Created))
        {
            Expense expense = ExpenseFactory.Create(Owner, final);

            Assert.ThrowsExactly<StateConflictException>(() => Execute(expense, action), action.ToString());
        }
    }

    private static void Execute(Expense expense, ExpenseAction action)
    {
        switch (action)
        {
            case ExpenseAction.Updated:
                expense.UpdateDraft(Actor, "Almoço com cliente revisado", 30m, DateOnly.FromDateTime(_now), 2, _now);
                break;
            case ExpenseAction.Submitted:
                expense.Submit(Actor, _now);
                break;
            case ExpenseAction.Approved:
                expense.Approve(Actor, _now);
                break;
            case ExpenseAction.Rejected:
                expense.Reject(Actor, "Falta o comprovante fiscal.", _now);
                break;
            case ExpenseAction.Paid:
                expense.MarkAsPaid(Actor, _now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Ação sem transição.");
        }
    }
}
