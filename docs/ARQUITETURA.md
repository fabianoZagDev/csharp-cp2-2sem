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
  histórico na mesma transação. No pagamento, o `PaymentRecord` entra pela navegação `Expense.Payment` e é gravado no
  mesmo `SaveChangesAsync` da transição.
- `FindAsync` e `ListAsync` fazem `Include(Payment)`, para o `ExpenseResponse` trazer `PaidById` e `PaidAtUtc`. O histórico
  não é carregado com o reembolso: `ListHistoryAsync` consulta `ExpenseHistories` com `Where` no banco, ordenado por
  `OccurredAtUtc` (e `Id` no empate).

## Entidades

| Entidade | Papel |
|---|---|
| `Expense` | reembolso com valor único, proprietário, estado, datas e token de concorrência |
| `ExpenseCategory` | categoria da despesa; Ids 1 a 5 semeados pelo mapeamento |
| `ExpenseHistory` | entrada imutável do histórico: ação, ator, instante UTC, estados anterior e posterior, justificativa e alterações |
| `PaymentRecord` | pagamento simulado: pagador, instante UTC e valor |

As propriedades das entidades têm `private set`. Alterações acontecem somente por métodos de domínio, que validam
estado e registram histórico.

## Autenticação e usuários

- Identity (`AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>()`) sobre o mesmo `AppDbContext`, com bearer token
  (`IdentityConstants.BearerScheme`). O login monta o principal com `IUserClaimsPrincipalFactory`, então o token carrega
  `NameIdentifier`, as roles e o security stamp.
- `Auth/SecurityStampMiddleware` compara o stamp do token com o do banco: token emitido antes de uma troca de roles recebe 401.
  `UserService.UpdateRolesAsync` renova o stamp.
- `Auth/IdentitySeeder` cria as 5 roles e **uma** conta Admin (e-mail em `Seed:Admin:Email`, senha em
  `Seed:Admin:Password` via user-secrets ou variável de ambiente `Seed__Admin__Password`). Sem senha configurada, só as roles são criadas.
- `POST /register` aceita só e-mail e senha (campos extras, como `roles`, geram 400). E-mail repetido gera 409.
- `PUT /api/admin/users/{id}/roles` substitui o conjunto de roles; role desconhecida gera 400 e o Admin não remove a própria role Admin (403).

## Matriz implementada

Ordem das checagens: role (atributo, 403) → escopo (404) → ownership (403) → estado (409).

| Linha da matriz | Onde a regra é aplicada | Teste que cobre |
|---|---|---|
| Registrar usuário (nunca aceita role) | `RegisterRequest` sem campo de role + `UnmappedMemberHandling.Disallow` | smoke HTTP (`ExpenseHub.Api.http`) |
| Listar usuários / alterar roles (somente Admin) | `[Authorize(Roles = Roles.Admin)]` em `AdminUsersController` | `RoleChangePolicyTests` |
| Não remover a própria role Admin | `RoleChangePolicy.Resolve` | `RoleChangePolicyTests.Resolve_AdminRemovingOwnAdminRole_ThrowsForbidden` |
| Criar reembolso (Employee; dono vem do token) | atributo `Employee` + `Expense.CreateDraft(user.Id, ...)` | `ExpensesControllerAuthorizationTests`, `ExpenseServiceDraftTests.CreateAsync_ValidRequest_UsesAuthenticatedOwnerAndDraftStatus` |
| Editar (dono e Draft) | atributo `Employee` + `ExpenseAccessPolicy.EnsureCanActAsOwner` + `Expense.UpdateDraft` | `ExpenseServiceDraftTests.UpdateAsync_*`, `ExpenseAccessPolicyTests.EnsureCanActAsOwner_*` |
| Enviar (dono e Draft) | atributo `Employee` + `EnsureCanActAsOwner` + `Expense.Submit` | `ExpenseServiceSubmitTests.SubmitAsync_*` |
| Listar (próprios / Submitted / Approved e Paid / todos) | `ExpenseAccessPolicy.ReadableBy`, aplicado na consulta (`IExpenseRepository.ListAsync`) | `ExpenseAccessPolicyTests.ReadableBy_*`, `ExpenseServiceSubmitTests.ListAsync_*` |
| Consultar detalhe (fora do escopo = 404) | `ExpenseAccessPolicy.CanRead` em `ExpenseService.GetByIdAsync` | `ExpenseServiceSubmitTests.GetByIdAsync_ApproverOnDraft_ThrowsNotFound` |
| Auditor e Admin sem escrita | atributos das rotas de escrita só com `Employee` | `ExpensesControllerAuthorizationTests.WriteActions_*` |
| Admin sem acesso funcional | Admin não está nas roles de nenhuma rota de reembolso | `ExpensesControllerAuthorizationTests.ReadActions_*` |
| Aprovar/reprovar/pagar (não proprietário) | `ExpenseAccessPolicy.EnsureCanDecide` | `ExpenseAccessPolicyTests.EnsureCanDecide_*` |
| Aprovar (Approver não dono, `Submitted → Approved`) | atributo `Approver` + `EnsureCanDecide` + `Expense.Approve` | `ExpenseDecisionTests.Approve_*`, `ExpenseServiceDecisionTests.ApproveAsync_*`, `ExpensesControllerAuthorizationTests.WriteAction_Roles_MatchAuthorizationMatrix` |
| Reprovar com justificativa de 10 a 500 caracteres (`Submitted → Rejected`) | `RejectExpenseRequest` (DataAnnotations) + atributo `Approver` + `EnsureCanDecide` + `Expense.Reject` (Trim e limites) | `ExpenseDecisionTests.Reject_*`, `ExpenseServiceDecisionTests.RejectAsync_*` |
| Pagar (Finance não dono, `Approved → Paid`, cria `PaymentRecord`) | atributo `Finance` + `EnsureCanDecide` + `Expense.MarkAsPaid` | `ExpensePaymentTests.*`, `ExpenseServicePaymentTests.PayAsync_*` |
| Histórico (mesma visibilidade do detalhe; fora do escopo = 404) | `ExpenseService.GetHistoryAsync` (reusa `GetByIdAsync`) + `IExpenseRepository.ListHistoryAsync` | `ExpenseServicePaymentTests.GetHistoryAsync_*`, `ExpenseServicePaymentTests.FullFlow_CreateToPaid_RecordsOrderedHistoryWithOneSavePerOperation` |

## Testes: apoio

- `Fakes/FixedTimeProvider`: horário fixo.
- `Fakes/InMemoryExpenseRepository`: repositório em memória; `SaveChangesCalls` conta as gravações; `ListAsync` usa `filter.Compile()`.
- `Fakes/ExpenseFactory.Create(ownerId, status)`: cria um reembolso em qualquer estado passando pelas transições reais
  (`Submit`, `Approve`, `Reject`, `MarkAsPaid`, com `ExpenseFactory.ApproverId` e `ExpenseFactory.FinanceId`). Nenhum teste força estado por reflexão.
- `InMemoryExpenseRepository.SaveFailure` simula falha de persistência; `ListHistoryCalls` mostra se o histórico foi consultado.
- A justificativa da reprovação é validada duas vezes: `RejectExpenseRequest` (400 automático) e `Expense.Reject`, que
  aplica Trim antes de medir (só espaços ou 9 caracteres úteis geram `DomainValidationException`).

## Testes: organização e regressão

| Suíte | Regra coberta |
|---|---|
| `Domain/ExpenseTransitionTableTests` | todos os estados × todas as ações: 5 transições válidas (estado, ator, horário, histórico) e as 20 inválidas (409 sem mudar estado, histórico nem `UpdatedAtUtc`) |
| `Domain/ExpenseDraftTests`, `ExpenseDecisionTests`, `ExpensePaymentTests` | limites de descrição, valor, data, categoria e justificativa; Trim; histórico de cada operação; `PaymentRecord` |
| `Services/ExpenseServiceOwnershipTests` | ordem escopo (404) → dono (403) → estado (409); leitor que não é dono; dono acumulando roles |
| `Services/ExpenseService*Tests` | operações completas com uma única gravação, ator e horário do servidor, visibilidade de detalhe e histórico |
| `Services/ExpenseAccessPolicyTests`, `Controllers/ExpensesControllerAuthorizationTests` | união das roles na leitura e matriz de roles por rota |

Regressão verificada (I09): cada regra abaixo foi quebrada de propósito, uma por vez, e revertida em seguida.

| Defeito introduzido | Testes que falharam (exemplos) |
|---|---|
| `EnsureCanDecide` sem checagem de dono | `EnsureCanDecide_OwnerWith*`, `ApproveAsync/RejectAsync/PayAsync_Owner*` (9 falhas) |
| data da despesa `>` trocado por `>=` | `CreateDraft_ExpenseDateToday_Succeeds`, `UpdateDraft_ExpenseDate_TodayAcceptedTomorrowRejected` (127 falhas) |
| justificativa mínima 9 | `Reject_JustificationTooShort_*` (2 falhas) |
| `MarkAsPaid` aceitando qualquer estado não-Draft | `MarkAsPaid_NotApproved_*`, `PayAsync_Twice_*`, `Transition_NotAllowed_*` (12 falhas) |
| Approver enxergando `Approved` em vez de `Submitted` | `ReadableBy_Approver_SeesOnlySubmitted`, `GetHistoryAsync_ApproverOnApproved_ThrowsNotFound` (3 falhas) |
| justificativa sem Trim | `Reject_JustificationWithPadding_IsTrimmed`, `Reject_JustificationTooShort_*` (3 falhas) |
| `Approve` sem registrar histórico | `Approve_Submitted_BecomesApprovedWithHistory`, `FullFlow_*`, `Transition_Allowed_*` (6 falhas) |
