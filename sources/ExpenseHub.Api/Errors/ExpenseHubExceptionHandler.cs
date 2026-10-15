using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Errors;

/// <summary>
/// Converte as exceções de negócio em respostas ProblemDetails.
/// </summary>
public sealed class ExpenseHubExceptionHandler : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not ExpenseHubException businessException)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = businessException.StatusCode,
            Title = businessException.Title,
            Detail = businessException.Message,
        };

        httpContext.Response.StatusCode = businessException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
