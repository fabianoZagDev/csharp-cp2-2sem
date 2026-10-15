using System;
using System.Reflection;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Cria reembolsos em qualquer estado para os testes, sem depender das transições de domínio.
/// </summary>
public static class ExpenseFactory
{
    private static readonly DateTime _now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Cria um reembolso válido do proprietário informado e força o estado desejado.
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

        if (status != ExpenseStatus.Draft)
        {
            PropertyInfo property = typeof(Expense).GetProperty(nameof(Expense.Status))!;
            property.SetValue(expense, status);
        }

        return expense;
    }
}
