using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados de cadastro: somente e-mail e senha, nunca roles.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// Obtém ou define o e-mail do usuário.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a senha do usuário.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
