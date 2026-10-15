using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Representação de uma entrada do histórico de um reembolso devolvida pela API.
/// </summary>
/// <param name="Id">Identificador sequencial da entrada.</param>
/// <param name="ExpenseId">Identificador do reembolso.</param>
/// <param name="Action">Ação executada.</param>
/// <param name="ActorId">Identificador do usuário que executou a ação.</param>
/// <param name="OccurredAtUtc">Instante da ação em UTC.</param>
/// <param name="FromStatus">Estado anterior, nulo na criação.</param>
/// <param name="ToStatus">Estado posterior.</param>
/// <param name="Justification">Justificativa da reprovação, quando houver.</param>
/// <param name="Changes">Descrição das alterações feitas em rascunho, quando houver.</param>
public sealed record ExpenseHistoryResponse(
    long Id,
    Guid ExpenseId,
    ExpenseAction Action,
    string ActorId,
    DateTime OccurredAtUtc,
    ExpenseStatus? FromStatus,
    ExpenseStatus ToStatus,
    string? Justification,
    string? Changes)
{
    /// <summary>
    /// Cria a resposta a partir da entidade.
    /// </summary>
    /// <param name="history">Entrada de histórico de origem.</param>
    /// <returns>A resposta correspondente.</returns>
    public static ExpenseHistoryResponse FromEntity(ExpenseHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return new ExpenseHistoryResponse(
            history.Id,
            history.ExpenseId,
            history.Action,
            history.ActorId,
            history.OccurredAtUtc,
            history.FromStatus,
            history.ToStatus,
            history.Justification,
            history.Changes);
    }
}
