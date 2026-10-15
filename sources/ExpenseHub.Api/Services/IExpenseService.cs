using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras de negócio dos reembolsos.
/// </summary>
public interface IExpenseService
{
    /// <summary>
    /// Cria um reembolso em rascunho para o usuário autenticado.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="request">Dados do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso criado.</returns>
    Task<Expense> CreateAsync(CurrentUser user, CreateExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Edita um rascunho do usuário autenticado.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso atualizado.</returns>
    Task<Expense> UpdateAsync(CurrentUser user, Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Envia um rascunho do usuário autenticado para aprovação.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso enviado.</returns>
    Task<Expense> SubmitAsync(CurrentUser user, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Aprova um reembolso enviado de outro usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso aprovado.</returns>
    Task<Expense> ApproveAsync(CurrentUser user, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Reprova um reembolso enviado de outro usuário, com justificativa.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Justificativa da reprovação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso reprovado.</returns>
    Task<Expense> RejectAsync(CurrentUser user, Guid id, RejectExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os reembolsos visíveis ao usuário, com o filtro aplicado na consulta.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos visíveis.</returns>
    Task<IReadOnlyList<Expense>> ListAsync(CurrentUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Consulta um reembolso dentro do escopo do usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso encontrado.</returns>
    Task<Expense> GetByIdAsync(CurrentUser user, Guid id, CancellationToken cancellationToken);
}
