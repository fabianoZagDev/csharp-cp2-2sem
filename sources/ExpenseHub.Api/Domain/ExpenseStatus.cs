namespace ExpenseHub.Api.Domain;

/// <summary>
/// Estados do ciclo de vida de um reembolso. O estado é definido exclusivamente pelo servidor.
/// </summary>
public enum ExpenseStatus
{
    /// <summary>
    /// Rascunho editável somente pelo proprietário.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Enviado para análise de um aprovador.
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Aprovado e aguardando pagamento.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Reprovado com justificativa. Estado final.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Pago pelo financeiro. Estado final.
    /// </summary>
    Paid = 4,
}
