using System;

namespace ExpenseHub.Api.Errors;

/// <summary>
/// Dado de entrada que viola uma regra de domínio (HTTP 400).
/// </summary>
public sealed class DomainValidationException : ExpenseHubException
{
    /// <summary>
    /// Inicializa a exceção com a mensagem padrão.
    /// </summary>
    public DomainValidationException()
        : base(400, "Dados inválidos")
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    public DomainValidationException(string message)
        : base(400, "Dados inválidos", message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a exceção interna.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    /// <param name="innerException">Exceção que causou o erro.</param>
    public DomainValidationException(string message, Exception innerException)
        : base(400, "Dados inválidos", message, innerException)
    {
    }
}
