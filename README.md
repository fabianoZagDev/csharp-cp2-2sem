# ExpenseHub — Checkpoint 2 (C#)

API REST corporativa de reembolsos: funcionários registram despesas, aprovadores analisam, o financeiro registra
o pagamento e auditores consultam todo o histórico.

O backlog e os critérios de aceite estão nas issues do repositório original
[Racass/checkpoint-csharpracass-expensehub](https://github.com/Racass/checkpoint-csharpracass-expensehub).

## Stack

- .NET 10 e ASP.NET Core
- Entity Framework Core 10 com SQLite
- MSTest para testes unitários

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Como executar

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

A API sobe em `http://localhost:5245`. O endpoint `GET /health` confirma que a aplicação iniciou.

## Banco de dados

| Item | Valor |
|---|---|
| Provider | SQLite |
| Pacotes | `Microsoft.EntityFrameworkCore.Sqlite` e `Microsoft.EntityFrameworkCore.Design` (10.0.12) |
| Configuração | `ConnectionStrings:DefaultConnection` em `sources/ExpenseHub.Api/appsettings.json` |
| Valor padrão | `Data Source=expensehub.db`, arquivo criado no diretório de execução da API |
| Migrations | `sources/ExpenseHub.Api/Data/Migrations` |

O banco é criado ou atualizado automaticamente na inicialização da API, que aplica as migrations pendentes.
Para aplicar manualmente:

```shell
dotnet tool restore
dotnet ef database update --project ./sources/ExpenseHub.Api
```

Para criar uma nova migration depois de alterar o modelo:

```shell
dotnet ef migrations add <Nome> --project ./sources/ExpenseHub.Api --output-dir Data/Migrations
```

A connection string pode ser sobrescrita pela variável de ambiente `ConnectionStrings__DefaultConnection`.
Para recriar o banco do zero, pare a API e apague os arquivos `expensehub.db*`. Os arquivos do banco não são
versionados.

## Testes

```shell
dotnet test ./sources/ExpenseHub.slnx
```

Os testes unitários não dependem de banco, rede ou serviço externo.

## Documentação

- [Arquitetura e convenções do grupo](docs/ARQUITETURA.md)
- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Rubrica](docs/RUBRICA.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)

## Integrantes
- Lorran dos Santos Sarmento - RM 558982
- Fabiano Zague - RM 555524
- Pedro Certo - RM 556268
