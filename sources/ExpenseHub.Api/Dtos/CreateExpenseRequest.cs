using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados de entrada para criar um reembolso em rascunho.
/// </summary>
public sealed class CreateExpenseRequest
{
    /// <summary>
    /// Obtém ou define a descrição da despesa.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o valor solicitado.
    /// </summary>
    [Required]
    [Range(typeof(decimal), "0.01", "2147483647", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Obtém ou define a data em que a despesa ocorreu.
    /// </summary>
    [Required]
    public DateOnly? ExpenseDate { get; set; }

    /// <summary>
    /// Obtém ou define o identificador da categoria.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }
}
