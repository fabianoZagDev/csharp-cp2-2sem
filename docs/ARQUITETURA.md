# Arquitetura e convenções

Este documento registra as decisões técnicas do grupo. Toda issue nova deve seguir estas convenções e
atualizar este arquivo quando tomar uma decisão que afete as demais.

## Estrutura de `sources/ExpenseHub.Api`

| Pasta | Conteúdo |
|---|---|
| `Data/` | `AppDbContext`, inicialização do banco e repositórios EF |
| `Data/Configurations/` | mapeamentos `IEntityTypeConfiguration<T>` |
| `Data/Migrations/` | migrations geradas pelo `dotnet ef` |
| `Domain/` | entidades e enums; as transições de estado são métodos da entidade `Expense` |
| `Auth/` | `Roles` (constantes) e `CurrentUser` (id e roles extraídos do token) |
| `Controllers/` | controllers finos: `CurrentUser.FromPrincipal(User)` → serviço → resposta |
| `Contracts/` | DTOs de **resposta** (sem DataAnnotations) |
| `Dtos/` | DTOs de **entrada**; todo arquivo daqui tem DataAnnotations |
| `Errors/` | exceções de negócio e o handler que gera ProblemDetails |
| `Services/` | regras de negócio, ownership e visibilidade (`ExpenseAccessPolicy`) |

Não existe pasta `Models/`: entidades ficam em `Domain/` e nunca são recebidas diretamente por endpoints.

## Persistência

- Provider SQLite, configurado por `ConnectionStrings:DefaultConnection`.
- Cada entidade tem uma classe de configuração em `Data/Configurations/`, aplicada por `ApplyConfigurationsFromAssembly`.
- Enums são gravados como texto.
- Todo `DateTime` é UTC: o `UtcDateTimeConverter` marca os valores lidos do banco como `DateTimeKind.Utc`.
- `Expense.Version` é token de concorrência. O `AppDbContext` renova o valor a cada gravação de um `Expense`, então
  duas alterações simultâneas do mesmo reembolso geram `DbUpdateConcurrencyException` na segunda.
- Cada operação de negócio deve terminar com **um único** `SaveChangesAsync`, que grava a mudança de estado e o
  histórico na mesma transação.

## Entidades

| Entidade | Papel |
|---|---|
| `Expense` | reembolso com valor único, proprietário, estado, datas e token de concorrência |
| `ExpenseCategory` | categoria da despesa; Ids 1 a 5 semeados pelo mapeamento |
| `ExpenseHistory` | entrada imutável do histórico: ação, ator, instante UTC, estados anterior e posterior, justificativa e alterações |
| `PaymentRecord` | pagamento simulado: pagador, instante UTC e valor |

As propriedades das entidades têm `private set`. Alterações acontecem somente por métodos de domínio, que validam
estado e registram histórico.
