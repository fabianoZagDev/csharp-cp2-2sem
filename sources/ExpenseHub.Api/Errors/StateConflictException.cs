using System;

namespace ExpenseHub.Api.Errors;

/// <summary>
/// Operação incompatível com o estado atual do recurso (HTTP 409).
/// </summary>
public sealed class StateConflictException : ExpenseHubException
{
    /// <summary>
    /// Inicializa a exceção com a mensagem padrão.
    /// </summary>
    public StateConflictException()
        : base(409, "Conflito de estado")
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    public StateConflictException(string message)
        : base(409, "Conflito de estado", message)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem e a exceção interna.
    /// </summary>
    /// <param name="message">Mensagem de erro.</param>
    /// <param name="innerException">Exceção que causou o erro.</param>
    public StateConflictException(string message, Exception innerException)
        : base(409, "Conflito de estado", message, innerException)
    {
    }
}
