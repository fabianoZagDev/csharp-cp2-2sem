using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Credenciais de login.
/// </summary>
public sealed class LoginRequest
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
    [StringLength(100, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
