using System.Collections.Generic;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Identidade do usuário autenticado, conforme o token.
/// </summary>
/// <param name="Id">Identificador do usuário.</param>
/// <param name="Roles">Roles presentes no token.</param>
public sealed record MeResponse(string Id, IReadOnlyCollection<string> Roles);
