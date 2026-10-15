using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras de escopo, visibilidade e ownership dos reembolsos.
/// </summary>
public static class ExpenseAccessPolicy
{
    /// <summary>
    /// Monta o filtro de leitura do usuário: a união das roles, traduzível pelo Entity Framework.
    /// Employee vê os próprios, Approver vê <c>Submitted</c>, Finance vê <c>Approved</c>/<c>Paid</c> e Auditor vê todos.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <returns>O filtro a aplicar na consulta.</returns>
    public static Expression<Func<Expense, bool>> ReadableBy(CurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        string userId = user.Id;
        bool isEmployee = user.IsInRole(Roles.Employee);
        bool isApprover = user.IsInRole(Roles.Approver);
        bool isFinance = user.IsInRole(Roles.Finance);
        bool isAuditor = user.IsInRole(Roles.Auditor);

        return expense =>
            isAuditor
            || (isEmployee && expense.OwnerId == userId)
            || (isApprover && expense.Status == ExpenseStatus.Submitted)
            || (isFinance && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }

    /// <summary>
    /// Indica se o usuário pode ler o reembolso.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="expense">Reembolso avaliado.</param>
    /// <returns><see langword="true"/> quando o reembolso está no escopo do usuário.</returns>
    public static bool CanRead(CurrentUser user, Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return ReadableBy(user).Compile().Invoke(expense);
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

    /// <summary>
    /// Garante que o usuário pode decidir (aprovar, reprovar ou pagar) o reembolso.
    /// Rascunho fora do escopo gera 404; o próprio dono gera 403, mesmo acumulando roles. O estado é checado depois pela entidade.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="expense">Reembolso avaliado.</param>
    /// <exception cref="ResourceNotFoundException">Quando é um rascunho que o usuário não pode ler.</exception>
    /// <exception cref="ForbiddenOperationException">Quando o usuário é o dono.</exception>
    public static void EnsureCanDecide(CurrentUser user, Expense expense)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(expense);

        if (expense.Status == ExpenseStatus.Draft && !CanRead(user, expense))
        {
            throw new ResourceNotFoundException("Reembolso não encontrado.");
        }

        if (expense.OwnerId == user.Id)
        {
            throw new ForbiddenOperationException("O proprietário não pode decidir o próprio reembolso.");
        }
    }
}
