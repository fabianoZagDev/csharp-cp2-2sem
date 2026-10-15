using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Entrada imutável do histórico de um reembolso.
/// </summary>
public class ExpenseHistory
{
    /// <summary>
    /// Quantidade máxima de caracteres da descrição das alterações.
    /// </summary>
    public const int ChangesMaxLength = 2000;

    /// <summary>
    /// Inicializa uma nova entrada de histórico.
    /// </summary>
    /// <param name="expenseId">Reembolso afetado.</param>
    /// <param name="action">Ação executada.</param>
    /// <param name="actorId">Usuário autenticado que executou a ação.</param>
    /// <param name="occurredAtUtc">Instante da ação em UTC, obtido do servidor.</param>
    /// <param name="fromStatus">Estado anterior, nulo na criação.</param>
    /// <param name="toStatus">Estado posterior.</param>
    /// <param name="justification">Justificativa da reprovação, quando houver.</param>
    /// <param name="changes">Descrição das alterações feitas em rascunho, quando houver.</param>
    public ExpenseHistory(
        Guid expenseId,
        ExpenseAction action,
        string actorId,
        DateTime occurredAtUtc,
        ExpenseStatus? fromStatus,
        ExpenseStatus toStatus,
        string? justification = null,
        string? changes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        ExpenseId = expenseId;
        Action = action;
        ActorId = actorId;
        OccurredAtUtc = occurredAtUtc;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Justification = justification;
        Changes = changes;
    }

    /// <summary>
    /// Construtor usado pelo Entity Framework Core.
    /// </summary>
    private ExpenseHistory()
    {
    }

    /// <summary>
    /// Obtém o identificador sequencial gerado pelo banco.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Obtém o identificador do reembolso.
    /// </summary>
    public Guid ExpenseId { get; private set; }

    /// <summary>
    /// Obtém a ação executada.
    /// </summary>
    public ExpenseAction Action { get; private set; }

    /// <summary>
    /// Obtém o identificador do usuário que executou a ação.
    /// </summary>
    public string ActorId { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém o instante da ação em UTC.
    /// </summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>
    /// Obtém o estado anterior à ação.
    /// </summary>
    public ExpenseStatus? FromStatus { get; private set; }

    /// <summary>
    /// Obtém o estado posterior à ação.
    /// </summary>
    public ExpenseStatus ToStatus { get; private set; }

    /// <summary>
    /// Obtém a justificativa da reprovação.
    /// </summary>
    public string? Justification { get; private set; }

    /// <summary>
    /// Obtém a descrição das alterações feitas em rascunho.
    /// </summary>
    public string? Changes { get; private set; }
}
