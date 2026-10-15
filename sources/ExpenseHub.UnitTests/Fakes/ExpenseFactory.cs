using System;
using System.Reflection;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Cria reembolsos em qualquer estado para os testes, passando pelas transições de domínio.
/// </summary>
public static class ExpenseFactory
{
    /// <summary>
    /// Identificador do aprovador usado nas transições montadas pela fábrica.
    /// </summary>
    public const string ApproverId = "factory-approver";

    private static readonly DateTime _now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Cria um reembolso válido do proprietário informado e o leva até o estado desejado.
    /// </summary>
    /// <param name="ownerId">Identificador do proprietário.</param>
    /// <param name="status">Estado desejado.</param>
    /// <returns>O reembolso criado.</returns>
    public static Expense Create(string ownerId, ExpenseStatus status = ExpenseStatus.Draft)
    {
        Expense expense = Expense.CreateDraft(
            ownerId,
            "Almoço com cliente",
            20m,
            DateOnly.FromDateTime(_now),
            1,
            _now);

        if (status == ExpenseStatus.Draft)
        {
            return expense;
        }

        expense.Submit(ownerId, _now);

        switch (status)
        {
            case ExpenseStatus.Approved:
                expense.Approve(ApproverId, _now);
                break;
            case ExpenseStatus.Rejected:
                expense.Reject(ApproverId, "Falta o comprovante fiscal.", _now);
                break;
            case ExpenseStatus.Paid:
                expense.Approve(ApproverId, _now);
                PropertyInfo property = typeof(Expense).GetProperty(nameof(Expense.Status))!;
                property.SetValue(expense, status);
                break;
            default:
                break;
        }

        return expense;
    }
}
