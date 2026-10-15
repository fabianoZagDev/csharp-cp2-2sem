namespace ExpenseHub.Api.Domain;

/// <summary>
/// Categoria de despesa. As categorias disponíveis são semeadas pelo mapeamento do Entity Framework Core.
/// </summary>
public class ExpenseCategory
{
    /// <summary>
    /// Quantidade máxima de caracteres do nome.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Construtor usado pelo Entity Framework Core.
    /// </summary>
    private ExpenseCategory()
    {
    }

    /// <summary>
    /// Obtém o identificador da categoria.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Obtém o nome da categoria.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
}
