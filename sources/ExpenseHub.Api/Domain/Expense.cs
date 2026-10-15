using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitação de reembolso de uma despesa corporativa. Cada reembolso possui um único valor.
/// </summary>
public class Expense
{
    /// <summary>
    /// Quantidade mínima de caracteres da descrição.
    /// </summary>
    public const int DescriptionMinLength = 10;

    /// <summary>
    /// Quantidade máxima de caracteres da descrição.
    /// </summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>
    /// Quantidade mínima de caracteres da justificativa de reprovação.
    /// </summary>
    public const int RejectionReasonMinLength = 10;

    /// <summary>
    /// Quantidade máxima de caracteres da justificativa de reprovação.
    /// </summary>
    public const int RejectionReasonMaxLength = 500;

    /// <summary>
    /// Menor valor aceito para um reembolso (R$ 0,01).
    /// </summary>
    public const decimal MinAmount = 0.01m;

    /// <summary>
    /// Maior valor aceito para um reembolso (R$ 2.147.483.647,00, equivalente a <see cref="int.MaxValue"/>).
    /// </summary>
    public const decimal MaxAmount = int.MaxValue;

    private readonly List<ExpenseHistory> _history = [];

    /// <summary>
    /// Construtor usado pelo Entity Framework Core.
    /// </summary>
    private Expense()
    {
    }

    /// <summary>
    /// Obtém o identificador gerado pelo servidor.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Obtém o identificador do usuário proprietário, obtido do token autenticado.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém a descrição da despesa.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém o valor solicitado em reais.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Obtém a data em que a despesa ocorreu.
    /// </summary>
    public DateOnly ExpenseDate { get; private set; }

    /// <summary>
    /// Obtém o identificador da categoria da despesa.
    /// </summary>
    public int CategoryId { get; private set; }

    /// <summary>
    /// Obtém a categoria da despesa, quando carregada.
    /// </summary>
    public ExpenseCategory? Category { get; private set; }

    /// <summary>
    /// Obtém o estado atual, controlado exclusivamente pelo servidor.
    /// </summary>
    public ExpenseStatus Status { get; private set; }

    /// <summary>
    /// Obtém a justificativa informada na reprovação.
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Obtém o instante de criação em UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o instante da última alteração em UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o token de concorrência, renovado a cada gravação pelo contexto.
    /// </summary>
    public Guid Version { get; private set; }

    /// <summary>
    /// Obtém as entradas de histórico do reembolso.
    /// </summary>
    public IReadOnlyCollection<ExpenseHistory> History => _history;

    /// <summary>
    /// Obtém o registro de pagamento, quando existir.
    /// </summary>
    public PaymentRecord? Payment { get; private set; }
}
