using System;
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
    [Authorize(Roles = Roles.Employee)]
    public async Task<ActionResult<ExpenseResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        Expense expense = await _service.GetByIdAsync(CurrentUser.FromPrincipal(User), id, cancellationToken);

        return ExpenseResponse.FromEntity(expense);
    }
}
