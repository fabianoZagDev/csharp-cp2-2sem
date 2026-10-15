using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Implementação de <see cref="IUserService"/> sobre o ASP.NET Core Identity.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<IdentityUser> _principalFactory;
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Inicializa o serviço.
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários.</param>
    /// <param name="principalFactory">Fábrica de principals com as claims do usuário.</param>
    /// <param name="dbContext">Contexto do banco, usado na listagem.</param>
    public UserService(
        UserManager<IdentityUser> userManager,
        IUserClaimsPrincipalFactory<IdentityUser> principalFactory,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _principalFactory = principalFactory;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        IdentityResult result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            bool duplicated = result.Errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName");
            string details = string.Join(" ", result.Errors.Select(error => error.Description));

            throw duplicated
                ? new StateConflictException("Já existe um usuário com este e-mail.")
                : new DomainValidationException(details);
        }

        return new UserResponse(user.Id, user.Email, []);
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal?> AuthenticateAsync(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IdentityUser? user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        return await _principalFactory.CreateAsync(user);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var assignments = await _dbContext.UserRoles
            .AsNoTracking()
            .Join(_dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (userRole, role) => new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        ILookup<string, string> rolesByUser = assignments.ToLookup(item => item.UserId, item => item.Name ?? string.Empty);

        return users
            .Select(user => new UserResponse(
                user.Id,
                user.Email,
                rolesByUser[user.Id].OrderBy(role => role, StringComparer.Ordinal).ToList()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<UserResponse> UpdateRolesAsync(CurrentUser actor, string userId, IReadOnlyCollection<string> roles)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(roles);

        IdentityUser user = await _userManager.FindByIdAsync(userId)
            ?? throw new ResourceNotFoundException("Usuário não encontrado.");

        IList<string> current = await _userManager.GetRolesAsync(user);
        IReadOnlyList<string> desired = RoleChangePolicy.Resolve(actor.Id, user.Id, current, roles);

        string[] toRemove = current.Except(desired, StringComparer.Ordinal).ToArray();
        string[] toAdd = desired.Except(current, StringComparer.Ordinal).ToArray();

        if (toRemove.Length > 0)
        {
            EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, toRemove));
        }

        if (toAdd.Length > 0)
        {
            EnsureSucceeded(await _userManager.AddToRolesAsync(user, toAdd));
        }

        EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));

        return new UserResponse(user.Id, user.Email, desired.OrderBy(role => role, StringComparer.Ordinal).ToList());
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
