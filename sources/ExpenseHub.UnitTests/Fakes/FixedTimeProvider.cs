using System;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Fonte de tempo com horário fixo para os testes.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    /// <summary>
    /// Inicializa a fonte de tempo.
    /// </summary>
    /// <param name="utcNow">Instante devolvido por <see cref="GetUtcNow"/>.</param>
    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}
