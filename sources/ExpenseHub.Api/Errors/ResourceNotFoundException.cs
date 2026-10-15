using System;

namespace ExpenseHub.Api.Errors;

/// <summary>
/// Recurso inexistente ou fora do escopo do usuário (HTTP 404).
/// </summary>
public sealed class ResourceNotFoundException : ExpenseHubException
{
    /// <summary>
    /// Inicializa a exceção com a mensagem padrão.
    /// </summary>
    public ResourceNotFoundException()
        : base(404, "Recurso não encontrado")
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    public ResourceNotFoundException(string message)
        : base(404, "Recurso não encontrado", message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a exceção interna.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    /// <param name="innerException">Exceção que causou o erro.</param>
    public ResourceNotFoundException(string message, Exception innerException)
        : base(404, "Recurso não encontrado", message, innerException)
    {
    }
}
