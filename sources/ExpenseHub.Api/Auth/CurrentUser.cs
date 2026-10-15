using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Usuário autenticado da requisição: identificador e roles extraídos do token.
/// </summary>
public sealed class CurrentUser
{
    /// <summary>
    /// Inicializa o usuário atual.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="roleNames">Roles do usuário.</param>
    public CurrentUser(string id, IEnumerable<string> roleNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(roleNames);

        Id = id;
        RoleNames = roleNames.ToArray();
    }

    /// <summary>
    /// Obtém o identificador do usuário.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Obtém os nomes das roles do usuário.
    /// </summary>
    public IReadOnlyCollection<string> RoleNames { get; }

    /// <summary>
    /// Extrai o usuário atual das claims do token.
    /// </summary>
    /// <param name="principal">Usuário autenticado da requisição.</param>
    /// <returns>O usuário atual.</returns>
    public static CurrentUser FromPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("O token não possui o identificador do usuário.");
        IEnumerable<string> roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value);

        return new CurrentUser(id, roles);
    }

    /// <summary>
    /// Indica se o usuário possui a role informada.
    /// </summary>
    /// <param name="roleName">Nome da role.</param>
    /// <returns><see langword="true"/> quando o usuário possui a role.</returns>
    public bool IsInRole(string roleName)
    {
        return RoleNames.Contains(roleName, StringComparer.Ordinal);
    }
}
