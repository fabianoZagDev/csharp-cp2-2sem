using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados de entrada para reprovar um reembolso enviado.
/// </summary>
public sealed class RejectExpenseRequest
{
    /// <summary>
    /// Obtém ou define a justificativa da reprovação.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Justification { get; set; } = string.Empty;
}
