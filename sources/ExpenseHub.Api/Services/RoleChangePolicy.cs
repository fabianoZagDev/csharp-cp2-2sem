using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Errors;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras para alteração de roles de um usuário.
/// </summary>
public static class RoleChangePolicy
{
    /// <summary>
    /// Valida e normaliza o conjunto de roles solicitado.
    /// </summary>
    /// <param name="actorId">Identificador do Admin que altera.</param>
    /// <param name="targetUserId">Identificador do usuário alterado.</param>
    /// <param name="currentRoles">Roles atuais do usuário alterado.</param>
    /// <param name="requestedRoles">Roles desejadas.</param>
    /// <returns>As roles desejadas, sem repetição.</returns>
    /// <exception cref="DomainValidationException">Quando há role desconhecida.</exception>
    /// <exception cref="ForbiddenOperationException">Quando o Admin tenta remover a própria role Admin.</exception>
    public static IReadOnlyList<string> Resolve(
        string actorId,
        string targetUserId,
        IEnumerable<string> currentRoles,
        IEnumerable<string> requestedRoles)
    {
        ArgumentNullException.ThrowIfNull(currentRoles);
        ArgumentNullException.ThrowIfNull(requestedRoles);

        string[] requested = requestedRoles
            .Select(role => role?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        string? unknown = requested.FirstOrDefault(role => !Roles.All.Contains(role, StringComparer.Ordinal));

        if (unknown is not null)
        {
            throw new DomainValidationException($"Role desconhecida: {unknown}.");
        }

        bool isSelf = string.Equals(actorId, targetUserId, StringComparison.Ordinal);
        bool hadAdmin = currentRoles.Contains(Roles.Admin, StringComparer.Ordinal);

        if (isSelf && hadAdmin && !requested.Contains(Roles.Admin, StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException("O Admin não pode remover a própria role Admin.");
        }

        return requested;
    }
}
