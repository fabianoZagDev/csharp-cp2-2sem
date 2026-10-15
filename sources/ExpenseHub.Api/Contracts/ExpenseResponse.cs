using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Representação de um reembolso devolvida pela API.
/// </summary>
/// <param name="Id">Identificador do reembolso.</param>
/// <param name="OwnerId">Identificador do proprietário.</param>
/// <param name="Description">Descrição da despesa.</param>
/// <param name="Amount">Valor solicitado.</param>
/// <param name="ExpenseDate">Data da despesa.</param>
/// <param name="CategoryId">Identificador da categoria.</param>
/// <param name="Status">Estado atual.</param>
/// <param name="RejectionReason">Justificativa da reprovação, quando houver.</param>
/// <param name="CreatedAtUtc">Instante de criação em UTC.</param>
/// <param name="UpdatedAtUtc">Instante da última alteração em UTC.</param>
/// <param name="PaidById">Identificador de quem registrou o pagamento, quando pago.</param>
/// <param name="PaidAtUtc">Instante do pagamento em UTC, quando pago.</param>
public sealed record ExpenseResponse(
    Guid Id,
    string OwnerId,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    int CategoryId,
    ExpenseStatus Status,
    string? RejectionReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? PaidById,
    DateTime? PaidAtUtc)
{
    /// <summary>
    /// Cria a resposta a partir da entidade.
    /// </summary>
    /// <param name="expense">Reembolso de origem.</param>
    /// <returns>A resposta correspondente.</returns>
    public static ExpenseResponse FromEntity(Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new ExpenseResponse(
            expense.Id,
            expense.OwnerId,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            expense.CategoryId,
            expense.Status,
            expense.RejectionReason,
            expense.CreatedAtUtc,
            expense.UpdatedAtUtc,
            expense.Payment?.PaidById,
            expense.Payment?.PaidAtUtc);
    }
}
