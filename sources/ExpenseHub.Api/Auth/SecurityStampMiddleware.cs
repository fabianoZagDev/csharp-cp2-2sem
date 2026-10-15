using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Rejeita com 401 tokens emitidos antes de uma troca de roles (security stamp diferente do atual).
/// </summary>
public sealed class SecurityStampMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Inicializa o middleware.
    /// </summary>
    /// <param name="next">Próximo passo do pipeline.</param>
    public SecurityStampMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Valida o security stamp do token da requisição.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="userManager">Gerenciador de usuários.</param>
    /// <param name="identityOptions">Opções do Identity.</param>
    /// <returns>Tarefa do middleware.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        UserManager<IdentityUser> userManager,
        IOptions<IdentityOptions> identityOptions)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(identityOptions);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            string? userId = userManager.GetUserId(context.User);
            string? tokenStamp = context.User.FindFirst(identityOptions.Value.ClaimsIdentity.SecurityStampClaimType)?.Value;
            IdentityUser? user = userId is null ? null : await userManager.FindByIdAsync(userId);

            if (user is null || tokenStamp is null || tokenStamp != user.SecurityStamp)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }
}
