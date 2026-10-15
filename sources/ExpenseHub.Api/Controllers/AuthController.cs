using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Cadastro, login e identidade do usuário autenticado.
/// </summary>
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Inicializa o controller.
    /// </summary>
    /// <param name="userService">Serviço de usuários.</param>
    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Cadastra um usuário sem roles.
    /// </summary>
    /// <param name="request">E-mail e senha.</param>
    /// <returns>O usuário criado.</returns>
    [HttpPost("/register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        UserResponse user = await _userService.RegisterAsync(request);

        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>
    /// Autentica o usuário e devolve o token bearer.
    /// </summary>
    /// <param name="request">Credenciais.</param>
    /// <returns>O token, ou 401 quando as credenciais são inválidas.</returns>
    [HttpPost("/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        ClaimsPrincipal? principal = await _userService.AuthenticateAsync(request);

        if (principal is null)
        {
            return Unauthorized();
        }

        return SignIn(principal, IdentityConstants.BearerScheme);
    }

    /// <summary>
    /// Mostra o identificador e as roles presentes no token.
    /// </summary>
    /// <returns>A identidade do token.</returns>
    [HttpGet("/api/me")]
    [Authorize]
    public ActionResult<MeResponse> Me()
    {
        CurrentUser user = CurrentUser.FromPrincipal(User);

        return new MeResponse(user.Id, user.RoleNames);
    }
}
