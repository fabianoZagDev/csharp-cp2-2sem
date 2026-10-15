using System;

namespace ExpenseHub.Api.Errors;

/// <summary>
/// Usuário sem permissão para a operação (HTTP 403).
/// </summary>
public sealed class ForbiddenOperationException : ExpenseHubException
{
    /// <summary>
    /// Inicializa a exceção com a mensagem padrão.
    /// </summary>
    public ForbiddenOperationException()
        : base(403, "Operação não permitida")
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    public ForbiddenOperationException(string message)
        : base(403, "Operação não permitida", message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a exceção interna.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    /// <param name="innerException">Exceção que causou o erro.</param>
    public ForbiddenOperationException(string message, Exception innerException)
        : base(403, "Operação não permitida", message, innerException)
    {
    }
}
