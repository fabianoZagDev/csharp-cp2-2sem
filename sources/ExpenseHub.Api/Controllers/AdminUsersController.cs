using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Administração de usuários e roles, exclusiva do Admin.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Inicializa o controller.
    /// </summary>
    /// <param name="userService">Serviço de usuários.</param>
    public AdminUsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Lista os usuários com suas roles.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os usuários.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _userService.ListAsync(cancellationToken));
    }

    /// <summary>
    /// Substitui as roles de um usuário.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="request">Roles desejadas.</param>
    /// <returns>O usuário atualizado.</returns>
    [HttpPut("{id}/roles")]
    public async Task<ActionResult<UserResponse>> UpdateRoles(string id, UpdateUserRolesRequest request)
    {
        return await _userService.UpdateRolesAsync(CurrentUser.FromPrincipal(User), id, request.Roles);
    }
}
