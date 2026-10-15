namespace ExpenseHub.Api.Auth;

/// <summary>
/// Configuração do Admin inicial, lida da seção <c>Seed:Admin</c>.
/// </summary>
public sealed class AdminSeedOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "Seed:Admin";

    /// <summary>
    /// Obtém ou define o e-mail do Admin inicial.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a senha do Admin inicial, fornecida por user-secrets ou variável de ambiente.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
