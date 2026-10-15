using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Conjunto de roles que o usuário deve passar a ter.
/// </summary>
public sealed class UpdateUserRolesRequest
{
    /// <summary>
    /// Obtém ou define os nomes das roles desejadas.
    /// </summary>
    [Required]
    [MaxLength(10)]
    public List<string> Roles { get; set; } = [];
}
