namespace ExpenseHub.Api.Domain;

/// <summary>
/// Ações registradas no histórico de um reembolso.
/// </summary>
public enum ExpenseAction
{
    /// <summary>
    /// Criação do rascunho.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Alteração de campos enquanto o reembolso estava em rascunho.
    /// </summary>
    Updated = 1,

    /// <summary>
    /// Envio do rascunho para análise.
    /// </summary>
    Submitted = 2,

    /// <summary>
    /// Aprovação por um aprovador.
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Reprovação com justificativa.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Registro do pagamento.
    /// </summary>
    Paid = 5,
}
