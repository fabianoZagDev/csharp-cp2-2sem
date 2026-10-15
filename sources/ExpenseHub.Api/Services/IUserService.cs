using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Dtos;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Cadastro, autenticação e administração de usuários.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Cadastra um usuário sem nenhuma role.
    /// </summary>
    /// <param name="request">Dados de cadastro.</param>
    /// <returns>O usuário criado.</returns>
    Task<UserResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Valida as credenciais e monta o principal com as roles atuais.
    /// </summary>
    /// <param name="request">Credenciais.</param>
    /// <returns>O principal, ou <see langword="null"/> quando as credenciais são inválidas.</returns>
    Task<ClaimsPrincipal?> AuthenticateAsync(LoginRequest request);

    /// <summary>
    /// Lista todos os usuários com suas roles.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os usuários.</returns>
    Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Substitui as roles de um usuário e invalida os tokens anteriores dele.
    /// </summary>
    /// <param name="actor">Admin que altera.</param>
    /// <param name="userId">Identificador do usuário alterado.</param>
    /// <param name="roles">Roles desejadas.</param>
    /// <returns>O usuário atualizado.</returns>
    Task<UserResponse> UpdateRolesAsync(CurrentUser actor, string userId, IReadOnlyCollection<string> roles);
}
