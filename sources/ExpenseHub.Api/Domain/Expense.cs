using System;
using System.Collections.Generic;
using System.Globalization;
using ExpenseHub.Api.Errors;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitação de reembolso de uma despesa corporativa. Cada reembolso possui um único valor.
/// </summary>
public class Expense
{
    /// <summary>
    /// Quantidade mínima de caracteres da descrição.
    /// </summary>
    public const int DescriptionMinLength = 10;

    /// <summary>
    /// Quantidade máxima de caracteres da descrição.
    /// </summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>
    /// Quantidade mínima de caracteres da justificativa de reprovação.
    /// </summary>
    public const int RejectionReasonMinLength = 10;

    /// <summary>
    /// Quantidade máxima de caracteres da justificativa de reprovação.
    /// </summary>
    public const int RejectionReasonMaxLength = 500;

    /// <summary>
    /// Menor valor aceito para um reembolso (R$ 0,01).
    /// </summary>
    public const decimal MinAmount = 0.01m;

    /// <summary>
    /// Maior valor aceito para um reembolso (R$ 2.147.483.647,00, equivalente a <see cref="int.MaxValue"/>).
    /// </summary>
    public const decimal MaxAmount = int.MaxValue;

    private readonly List<ExpenseHistory> _history = [];

    /// <summary>
    /// Construtor usado pelo Entity Framework Core.
    /// </summary>
    private Expense()
    {
    }

    /// <summary>
    /// Obtém o identificador gerado pelo servidor.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Obtém o identificador do usuário proprietário, obtido do token autenticado.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém a descrição da despesa.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém o valor solicitado em reais.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Obtém a data em que a despesa ocorreu.
    /// </summary>
    public DateOnly ExpenseDate { get; private set; }

    /// <summary>
    /// Obtém o identificador da categoria da despesa.
    /// </summary>
    public int CategoryId { get; private set; }

    /// <summary>
    /// Obtém a categoria da despesa, quando carregada.
    /// </summary>
    public ExpenseCategory? Category { get; private set; }

    /// <summary>
    /// Obtém o estado atual, controlado exclusivamente pelo servidor.
    /// </summary>
    public ExpenseStatus Status { get; private set; }

    /// <summary>
    /// Obtém a justificativa informada na reprovação.
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Obtém o instante de criação em UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o instante da última alteração em UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o token de concorrência, renovado a cada gravação pelo contexto.
    /// </summary>
    public Guid Version { get; private set; }

    /// <summary>
    /// Obtém as entradas de histórico do reembolso.
    /// </summary>
    public IReadOnlyCollection<ExpenseHistory> History => _history;

    /// <summary>
    /// Obtém o registro de pagamento, quando existir.
    /// </summary>
    public PaymentRecord? Payment { get; private set; }

    /// <summary>
    /// Cria um reembolso em rascunho, validando os dados e registrando o histórico de criação.
    /// </summary>
    /// <param name="ownerId">Identificador do proprietário, vindo do token.</param>
    /// <param name="description">Descrição da despesa.</param>
    /// <param name="amount">Valor solicitado.</param>
    /// <param name="expenseDate">Data da despesa.</param>
    /// <param name="categoryId">Identificador da categoria.</param>
    /// <param name="nowUtc">Instante atual em UTC.</param>
    /// <returns>O reembolso em estado <see cref="ExpenseStatus.Draft"/>.</returns>
    /// <exception cref="DomainValidationException">Quando algum dado é inválido.</exception>
    public static Expense CreateDraft(
        string ownerId,
        string description,
        decimal amount,
        DateOnly expenseDate,
        int categoryId,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        string cleanDescription = ValidateFields(description, amount, expenseDate, categoryId, nowUtc);

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Description = cleanDescription,
            Amount = amount,
            ExpenseDate = expenseDate,
            CategoryId = categoryId,
            Status = ExpenseStatus.Draft,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };

        expense._history.Add(new ExpenseHistory(
            expense.Id,
            ExpenseAction.Created,
            ownerId,
            nowUtc,
            null,
            ExpenseStatus.Draft));

        return expense;
    }

    /// <summary>
    /// Atualiza um rascunho, revalidando os dados e registrando o que mudou no histórico.
    /// </summary>
    /// <param name="actorId">Identificador de quem edita, vindo do token.</param>
    /// <param name="description">Nova descrição.</param>
    /// <param name="amount">Novo valor.</param>
    /// <param name="expenseDate">Nova data da despesa.</param>
    /// <param name="categoryId">Nova categoria.</param>
    /// <param name="nowUtc">Instante atual em UTC.</param>
    /// <exception cref="StateConflictException">Quando o reembolso não está em rascunho.</exception>
    /// <exception cref="DomainValidationException">Quando algum dado é inválido.</exception>
    public void UpdateDraft(
        string actorId,
        string description,
        decimal amount,
        DateOnly expenseDate,
        int categoryId,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        if (Status != ExpenseStatus.Draft)
        {
            throw new StateConflictException("Somente reembolsos em rascunho podem ser editados.");
        }

        string cleanDescription = ValidateFields(description, amount, expenseDate, categoryId, nowUtc);
        string changes = DescribeChanges(cleanDescription, amount, expenseDate, categoryId);

        Description = cleanDescription;
        Amount = amount;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        UpdatedAtUtc = nowUtc;

        _history.Add(new ExpenseHistory(
            Id,
            ExpenseAction.Updated,
            actorId,
            nowUtc,
            ExpenseStatus.Draft,
            ExpenseStatus.Draft,
            changes: changes));
    }

    /// <summary>
    /// Envia o rascunho para aprovação, registrando o histórico da transição.
    /// </summary>
    /// <param name="actorId">Identificador de quem envia, vindo do token.</param>
    /// <param name="nowUtc">Instante atual em UTC.</param>
    /// <exception cref="StateConflictException">Quando o reembolso não está em rascunho.</exception>
    public void Submit(string actorId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        if (Status != ExpenseStatus.Draft)
        {
            throw new StateConflictException("Somente reembolsos em rascunho podem ser enviados.");
        }

        Status = ExpenseStatus.Submitted;
        UpdatedAtUtc = nowUtc;

        _history.Add(new ExpenseHistory(
            Id,
            ExpenseAction.Submitted,
            actorId,
            nowUtc,
            ExpenseStatus.Draft,
            ExpenseStatus.Submitted));
    }

    /// <summary>
    /// Aprova um reembolso enviado, registrando o histórico da transição.
    /// </summary>
    /// <param name="actorId">Identificador do aprovador, vindo do token.</param>
    /// <param name="nowUtc">Instante atual em UTC.</param>
    /// <exception cref="StateConflictException">Quando o reembolso não está enviado.</exception>
    public void Approve(string actorId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        if (Status != ExpenseStatus.Submitted)
        {
            throw new StateConflictException("Somente reembolsos enviados podem ser aprovados.");
        }

        Status = ExpenseStatus.Approved;
        UpdatedAtUtc = nowUtc;

        _history.Add(new ExpenseHistory(
            Id,
            ExpenseAction.Approved,
            actorId,
            nowUtc,
            ExpenseStatus.Submitted,
            ExpenseStatus.Approved));
    }

    /// <summary>
    /// Reprova um reembolso enviado, gravando a justificativa e o histórico da transição.
    /// </summary>
    /// <param name="actorId">Identificador do aprovador, vindo do token.</param>
    /// <param name="justification">Justificativa da reprovação.</param>
    /// <param name="nowUtc">Instante atual em UTC.</param>
    /// <exception cref="DomainValidationException">Quando a justificativa é vazia, curta ou longa.</exception>
    /// <exception cref="StateConflictException">Quando o reembolso não está enviado.</exception>
    public void Reject(string actorId, string justification, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        string cleanJustification = justification?.Trim() ?? string.Empty;

        if (cleanJustification.Length is < RejectionReasonMinLength or > RejectionReasonMaxLength)
        {
            throw new DomainValidationException(
                $"A justificativa deve ter entre {RejectionReasonMinLength} e {RejectionReasonMaxLength} caracteres.");
        }

        if (Status != ExpenseStatus.Submitted)
        {
            throw new StateConflictException("Somente reembolsos enviados podem ser reprovados.");
        }

        Status = ExpenseStatus.Rejected;
        RejectionReason = cleanJustification;
        UpdatedAtUtc = nowUtc;

        _history.Add(new ExpenseHistory(
            Id,
            ExpenseAction.Rejected,
            actorId,
            nowUtc,
            ExpenseStatus.Submitted,
            ExpenseStatus.Rejected,
            justification: cleanJustification));
    }

    /// <summary>
    /// Registra o pagamento simulado de um reembolso aprovado, criando o <see cref="PaymentRecord"/> e o histórico.
    /// </summary>
    /// <param name="actorId">Identificador do usuário do financeiro, vindo do token.</param>
    /// <param name="nowUtc">Instante atual em UTC.</param>
    /// <exception cref="StateConflictException">Quando o reembolso não está aprovado.</exception>
    public void MarkAsPaid(string actorId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        if (Status != ExpenseStatus.Approved)
        {
            throw new StateConflictException("Somente reembolsos aprovados podem ser pagos.");
        }

        Status = ExpenseStatus.Paid;
        UpdatedAtUtc = nowUtc;
        Payment = new PaymentRecord(Id, actorId, nowUtc, Amount);

        _history.Add(new ExpenseHistory(
            Id,
            ExpenseAction.Paid,
            actorId,
            nowUtc,
            ExpenseStatus.Approved,
            ExpenseStatus.Paid));
    }

    private static string ValidateFields(
        string description,
        decimal amount,
        DateOnly expenseDate,
        int categoryId,
        DateTime nowUtc)
    {
        string cleanDescription = description?.Trim() ?? string.Empty;

        if (cleanDescription.Length is < DescriptionMinLength or > DescriptionMaxLength)
        {
            throw new DomainValidationException(
                $"A descrição deve ter entre {DescriptionMinLength} e {DescriptionMaxLength} caracteres.");
        }

        if (amount is < MinAmount or > MaxAmount)
        {
            throw new DomainValidationException("O valor deve estar entre 0,01 e 2.147.483.647.");
        }

        if (expenseDate > DateOnly.FromDateTime(nowUtc))
        {
            throw new DomainValidationException("A data da despesa não pode ser futura.");
        }

        if (categoryId <= 0)
        {
            throw new DomainValidationException("A categoria é inválida.");
        }

        return cleanDescription;
    }

    private string DescribeChanges(string description, decimal amount, DateOnly expenseDate, int categoryId)
    {
        var changes = new List<string>();

        if (!string.Equals(Description, description, StringComparison.Ordinal))
        {
            changes.Add($"Description: '{Description}' -> '{description}'");
        }

        if (Amount != amount)
        {
            changes.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"Amount: {Amount} -> {amount}"));
        }

        if (ExpenseDate != expenseDate)
        {
            changes.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"ExpenseDate: {ExpenseDate:yyyy-MM-dd} -> {expenseDate:yyyy-MM-dd}"));
        }

        if (CategoryId != categoryId)
        {
            changes.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"CategoryId: {CategoryId} -> {categoryId}"));
        }

        string text = changes.Count == 0 ? "Nenhuma alteração." : string.Join("; ", changes);

        return text.Length > ExpenseHistory.ChangesMaxLength ? text[..ExpenseHistory.ChangesMaxLength] : text;
    }
}
