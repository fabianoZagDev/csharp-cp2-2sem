using System;
using System.Globalization;
using System.Linq;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Errors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Testes das regras de criação e edição de rascunho em <see cref="Expense"/>.
/// </summary>
[TestClass]
public sealed class ExpenseDraftTests
{
    private static readonly DateTime _now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly _today = new(2026, 10, 15);

    /// <summary>
    /// Descrições fora do intervalo de 10 a 500 caracteres são rejeitadas.
    /// </summary>
    /// <param name="length">Tamanho da descrição.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(9)]
    [DataRow(501)]
    public void CreateDraft_DescriptionOutOfRange_ThrowsDomainValidation(int length)
    {
        string description = new('a', length);

        Assert.ThrowsExactly<DomainValidationException>(
            () => Expense.CreateDraft("user-1", description, 10m, _today, 1, _now));
    }

    /// <summary>
    /// Descrição composta só de espaços é rejeitada.
    /// </summary>
    [TestMethod]
    public void CreateDraft_DescriptionOnlyWhitespace_ThrowsDomainValidation()
    {
        Assert.ThrowsExactly<DomainValidationException>(
            () => Expense.CreateDraft("user-1", new string(' ', 20), 10m, _today, 1, _now));
    }

    /// <summary>
    /// Os limites de 10 e 500 caracteres são aceitos.
    /// </summary>
    /// <param name="length">Tamanho da descrição.</param>
    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public void CreateDraft_DescriptionAtLimit_Succeeds(int length)
    {
        Expense expense = Expense.CreateDraft("user-1", new string('a', length), 10m, _today, 1, _now);

        Assert.AreEqual(length, expense.Description.Length);
    }

    /// <summary>
    /// A descrição é gravada sem espaços nas pontas.
    /// </summary>
    [TestMethod]
    public void CreateDraft_DescriptionWithPadding_IsTrimmed()
    {
        Expense expense = Expense.CreateDraft("user-1", "  Almoço com cliente  ", 10m, _today, 1, _now);

        Assert.AreEqual("Almoço com cliente", expense.Description);
    }

    /// <summary>
    /// Valores abaixo de 0,01 ou acima de 2.147.483.647 são rejeitados.
    /// </summary>
    /// <param name="amount">Valor testado.</param>
    [TestMethod]
    [DataRow("0")]
    [DataRow("0.001")]
    [DataRow("-1")]
    [DataRow("2147483647.01")]
    public void CreateDraft_AmountOutOfRange_ThrowsDomainValidation(string amount)
    {
        decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);

        Assert.ThrowsExactly<DomainValidationException>(
            () => Expense.CreateDraft("user-1", "Almoço com cliente", value, _today, 1, _now));
    }

    /// <summary>
    /// Os limites 0,01 e 2.147.483.647 são aceitos.
    /// </summary>
    /// <param name="amount">Valor testado.</param>
    [TestMethod]
    [DataRow("0.01")]
    [DataRow("2147483647")]
    public void CreateDraft_AmountAtLimit_Succeeds(string amount)
    {
        decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);

        Expense expense = Expense.CreateDraft("user-1", "Almoço com cliente", value, _today, 1, _now);

        Assert.AreEqual(value, expense.Amount);
    }

    /// <summary>
    /// A data de hoje é aceita.
    /// </summary>
    [TestMethod]
    public void CreateDraft_ExpenseDateToday_Succeeds()
    {
        Expense expense = Expense.CreateDraft("user-1", "Almoço com cliente", 10m, _today, 1, _now);

        Assert.AreEqual(_today, expense.ExpenseDate);
    }

    /// <summary>
    /// A data de amanhã é rejeitada.
    /// </summary>
    [TestMethod]
    public void CreateDraft_ExpenseDateTomorrow_ThrowsDomainValidation()
    {
        Assert.ThrowsExactly<DomainValidationException>(
            () => Expense.CreateDraft("user-1", "Almoço com cliente", 10m, _today.AddDays(1), 1, _now));
    }

    /// <summary>
    /// A criação define dono, estado Draft e o histórico Created.
    /// </summary>
    [TestMethod]
    public void CreateDraft_ValidData_SetsOwnerDraftStatusAndCreatedHistory()
    {
        Expense expense = Expense.CreateDraft("user-1", "Almoço com cliente", 10m, _today, 1, _now);

        ExpenseHistory entry = expense.History.Single();
        Assert.AreEqual("user-1", expense.OwnerId);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreNotEqual(Guid.Empty, expense.Id);
        Assert.AreEqual(ExpenseAction.Created, entry.Action);
        Assert.IsNull(entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, entry.ToStatus);
        Assert.AreEqual("user-1", entry.ActorId);
        Assert.AreEqual(_now, entry.OccurredAtUtc);
    }

    /// <summary>
    /// A edição de um rascunho altera os campos e registra Updated com as mudanças.
    /// </summary>
    [TestMethod]
    public void UpdateDraft_ChangedFields_RecordsUpdatedHistoryWithChanges()
    {
        Expense expense = Expense.CreateDraft("user-1", "Almoço com cliente", 10m, _today, 1, _now);
        DateTime later = _now.AddHours(1);

        expense.UpdateDraft("user-1", "Almoço com cliente X", 12.5m, _today, 2, later);

        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Updated, entry.Action);
        Assert.AreEqual(ExpenseStatus.Draft, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, entry.ToStatus);
        Assert.IsNotNull(entry.Changes);
        StringAssert.Contains(entry.Changes, "Amount: 10 -> 12.5");
        StringAssert.Contains(entry.Changes, "CategoryId: 1 -> 2");
        Assert.AreEqual(12.5m, expense.Amount);
        Assert.AreEqual(later, expense.UpdatedAtUtc);
        Assert.AreEqual(_now, expense.CreatedAtUtc);
    }

    /// <summary>
    /// Editar um reembolso que não está em rascunho gera conflito de estado.
    /// </summary>
    [TestMethod]
    public void UpdateDraft_NotDraft_ThrowsStateConflict()
    {
        Expense expense = Expense.CreateDraft("user-1", "Almoço com cliente", 10m, _today, 1, _now);
        typeof(Expense).GetProperty(nameof(Expense.Status))!.SetValue(expense, ExpenseStatus.Submitted);

        Assert.ThrowsExactly<StateConflictException>(
            () => expense.UpdateDraft("user-1", "Almoço com cliente", 10m, _today, 1, _now));
        Assert.HasCount(1, expense.History);
    }

    /// <summary>
    /// Uma edição inválida não altera o reembolso nem o histórico.
    /// </summary>
    [TestMethod]
    public void UpdateDraft_InvalidData_KeepsExpenseUntouched()
    {
        Expense expense = Expense.CreateDraft("user-1", "Almoço com cliente", 10m, _today, 1, _now);

        Assert.ThrowsExactly<DomainValidationException>(
            () => expense.UpdateDraft("user-1", "curta", 10m, _today, 1, _now));
        Assert.AreEqual("Almoço com cliente", expense.Description);
        Assert.HasCount(1, expense.History);
    }
}
