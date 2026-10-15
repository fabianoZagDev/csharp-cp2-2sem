using System;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras de escopo e ownership dos reembolsos.
/// </summary>
public static class ExpenseAccessPolicy
{
    /// <summary>
    /// Indica se o usuário pode ler o reembolso.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="expense">Reembolso avaliado.</param>
    /// <returns><see langword="true"/> quando o reembolso está no escopo do usuário.</returns>
    public static bool CanRead(CurrentUser user, Expense expense)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(expense);

        return expense.OwnerId == user.Id;
    }

    /// <summary>
    /// Garante que o usuário é o dono do reembolso: fora do escopo gera 404 e não sendo dono gera 403.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="expense">Reembolso avaliado.</param>
    /// <exception cref="ResourceNotFoundException">Quando o usuário não pode ler o reembolso.</exception>
    /// <exception cref="ForbiddenOperationException">Quando o usuário não é o dono.</exception>
    public static void EnsureCanActAsOwner(CurrentUser user, Expense expense)
    {
        if (!CanRead(user, expense))
        {
            throw new ResourceNotFoundException("Reembolso não encontrado.");
        }

        if (expense.OwnerId != user.Id)
        {
            throw new ForbiddenOperationException("Somente o proprietário pode alterar o reembolso.");
        }
    }
}
