using System;

namespace ExpenseHub.Api.Errors;

/// <summary>
/// Base das exceções de negócio, convertidas em ProblemDetails pelo handler global.
/// </summary>
public abstract class ExpenseHubException : Exception
{
    /// <summary>
    /// Inicializa a exceção.
    /// </summary>
    /// <param name="statusCode">Código HTTP correspondente.</param>
    /// <param name="title">Título do ProblemDetails.</param>
    /// <param name="message">Mensagem de erro.</param>
    /// <param name="innerException">Exceção que causou o erro.</param>
    protected ExpenseHubException(int statusCode, string title, string? message = null, Exception? innerException = null)
        : base(message ?? title, innerException)
    {
        StatusCode = statusCode;
        Title = title;
    }

    /// <summary>
    /// Obtém o código HTTP correspondente.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Obtém o título do ProblemDetails.
    /// </summary>
    public string Title { get; }
}
