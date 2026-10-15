using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro do pagamento simulado de um reembolso aprovado.
/// </summary>
public class PaymentRecord
{
    /// <summary>
    /// Inicializa um novo registro de pagamento.
    /// </summary>
    /// <param name="expenseId">Reembolso pago.</param>
    /// <param name="paidById">Usuário do financeiro que registrou o pagamento.</param>
    /// <param name="paidAtUtc">Instante do pagamento em UTC, obtido do servidor.</param>
    /// <param name="amount">Valor pago.</param>
    public PaymentRecord(Guid expenseId, string paidById, DateTime paidAtUtc, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paidById);

        Id = Guid.NewGuid();
        ExpenseId = expenseId;
        PaidById = paidById;
        PaidAtUtc = paidAtUtc;
        Amount = amount;
    }

    /// <summary>
    /// Construtor usado pelo Entity Framework Core.
    /// </summary>
    private PaymentRecord()
    {
    }

    /// <summary>
    /// Obtém o identificador do pagamento.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Obtém o identificador do reembolso pago.
    /// </summary>
    public Guid ExpenseId { get; private set; }

    /// <summary>
    /// Obtém o identificador do usuário que registrou o pagamento.
    /// </summary>
    public string PaidById { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém o instante do pagamento em UTC.
    /// </summary>
    public DateTime PaidAtUtc { get; private set; }

    /// <summary>
    /// Obtém o valor pago.
    /// </summary>
    public decimal Amount { get; private set; }
}
