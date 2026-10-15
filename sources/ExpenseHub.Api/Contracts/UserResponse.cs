using System.Collections.Generic;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Usuário e suas roles, devolvido nas rotas administrativas.
/// </summary>
/// <param name="Id">Identificador do usuário.</param>
/// <param name="Email">E-mail do usuário.</param>
/// <param name="Roles">Roles atribuídas.</param>
public sealed record UserResponse(string Id, string? Email, IReadOnlyList<string> Roles);
