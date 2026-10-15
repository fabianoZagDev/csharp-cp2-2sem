using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Endpoints dos reembolsos.
/// </summary>
[ApiController]
[Authorize]
[Route("api/expenses")]
public sealed class ExpensesController : ControllerBase
{
    private const string ReadRoles = Roles.Employee + "," + Roles.Approver + "," + Roles.Finance + "," + Roles.Auditor;

    private readonly IExpenseService _service;

    /// <summary>
    /// Inicializa o controller.
    /// </summary>
    /// <param name="service">Serviço de reembolsos.</param>
    public ExpensesController(IExpenseService service)
    {
        _service = service;
    }

    /// <summary>
    /// Cria um reembolso em rascunho.
    /// </summary>
    /// <param name="request">Dados do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso criado.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<ExpenseResponse>> Create(
        CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        Expense expense = await _service.CreateAsync(CurrentUser.FromPrincipal(User), request, cancellationToken);
        ExpenseResponse response = ExpenseResponse.FromEntity(expense);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Edita um rascunho.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso atualizado.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<ExpenseResponse>> Update(
        Guid id,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        Expense expense = await _service.UpdateAsync(CurrentUser.FromPrincipal(User), id, request, cancellationToken);

        return ExpenseResponse.FromEntity(expense);
    }

    /// <summary>
    /// Consulta um reembolso.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso encontrado.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<ExpenseResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        Expense expense = await _service.GetByIdAsync(CurrentUser.FromPrincipal(User), id, cancellationToken);

        return ExpenseResponse.FromEntity(expense);
    }

    /// <summary>
    /// Envia um rascunho para aprovação.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso enviado.</returns>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<ExpenseResponse>> Submit(Guid id, CancellationToken cancellationToken)
    {
        Expense expense = await _service.SubmitAsync(CurrentUser.FromPrincipal(User), id, cancellationToken);

        return ExpenseResponse.FromEntity(expense);
    }

    /// <summary>
    /// Aprova um reembolso enviado.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso aprovado.</returns>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Approver)]
    public async Task<ActionResult<ExpenseResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        Expense expense = await _service.ApproveAsync(CurrentUser.FromPrincipal(User), id, cancellationToken);

        return ExpenseResponse.FromEntity(expense);
    }

    /// <summary>
    /// Reprova um reembolso enviado, com justificativa.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Justificativa da reprovação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso reprovado.</returns>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Approver)]
    public async Task<ActionResult<ExpenseResponse>> Reject(
        Guid id,
        RejectExpenseRequest request,
        CancellationToken cancellationToken)
    {
        Expense expense = await _service.RejectAsync(CurrentUser.FromPrincipal(User), id, request, cancellationToken);

        return ExpenseResponse.FromEntity(expense);
    }

    /// <summary>
    /// Registra o pagamento simulado de um reembolso aprovado.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso pago.</returns>
    [HttpPost("{id:guid}/pay")]
    [Authorize(Roles = Roles.Finance)]
    public async Task<ActionResult<ExpenseResponse>> Pay(Guid id, CancellationToken cancellationToken)
    {
        Expense expense = await _service.PayAsync(CurrentUser.FromPrincipal(User), id, cancellationToken);

        return ExpenseResponse.FromEntity(expense);
    }

    /// <summary>
    /// Consulta o histórico de um reembolso.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>As entradas do histórico em ordem cronológica.</returns>
    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<IReadOnlyList<ExpenseHistoryResponse>>> History(
        Guid id,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ExpenseHistory> history =
            await _service.GetHistoryAsync(CurrentUser.FromPrincipal(User), id, cancellationToken);

        return history.Select(ExpenseHistoryResponse.FromEntity).ToList();
    }

    /// <summary>
    /// Lista os reembolsos visíveis ao usuário.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos visíveis.</returns>
    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<Expense> expenses = await _service.ListAsync(CurrentUser.FromPrincipal(User), cancellationToken);

        return expenses.Select(ExpenseResponse.FromEntity).ToList();
    }
}
