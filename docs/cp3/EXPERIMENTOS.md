# CP3 — Dossiê dos experimentos E01–E05

Ambiente executado em 2026-11-05 00:15 UTC (04/11, 21h15 em Brasília) com `docker compose` e as **imagens publicadas** no GHCR para o commit
`28688c5d28476cbeb936abc7a956fc9b1a0939a1` (merge do PR #11, último commit que altera o código da web e do worker;
os commits seguintes mudam somente documentação, scripts e testes, e o workflow `images` também publica imagens
com o SHA de cada um deles):

| Serviço | Imagem | Digest local |
| --- | --- | --- |
| web | `ghcr.io/fabianozagdev/expensehub-web:28688c5d28476cbeb936abc7a956fc9b1a0939a1` | `sha256:65cd6e0410ea…` |
| worker | `ghcr.io/fabianozagdev/expensehub-worker:28688c5d28476cbeb936abc7a956fc9b1a0939a1` | `sha256:8d3349a104c2…` |

As saídas completas e sanitizadas estão em [`evidencias/`](evidencias). Tokens, senhas e cabeçalhos de
autenticação nunca foram gravados (as requisições aparecem como `Bearer <token omitido>`).

## Como reproduzir

```bash
cp .env.example .env          # IMAGE_TAG=28688c5d28476cbeb936abc7a956fc9b1a0939a1 e senhas
docker compose pull
docker compose up -d
```

```powershell
# Senhas só em variáveis de ambiente (o script de experimentos também lê o .env)
$env:EXPENSEHUB_ADMIN_PASSWORD = '<ADMIN_PASSWORD do .env>'
$env:EXPENSEHUB_USER_PASSWORD = '<senha para os usuários de teste>'
./scripts/cp3/Invoke-Cp3Seed.ps1            # usuários, roles e reembolsos conhecidos, só pela API
./scripts/cp3/Invoke-Cp3Experiments.ps1     # E01–E05, grava docs/cp3/evidencias
./scripts/cp3/Invoke-Cp3BrokerDown.ps1      # E06 complementar (para e religa só o RabbitMQ)
./scripts/cp3/Invoke-Cp3ExcelCheck.ps1 -ReportId <id do E01>   # abre a planilha no Excel (Windows)
```

Dados conhecidos (criados pela API, passando por envio, aprovação e pagamento):

| Reembolso | Dono | Data | Estado | Valor |
| --- | --- | --- | --- | --- |
| 1 | employee-a | 2026-09-10 | Approved | 100,00 |
| 2 | employee-a | 2026-09-15 | Approved | 50,00 |
| 3 | employee-a | 2026-09-20 | Paid | 25,00 |
| 4 | employee-b | 2026-09-12 | Approved | 200,00 |
| 5 | employee-a | 2026-08-31 | Paid | 500,00 (fora do período) |

Esperado para 2026-09-01..2026-09-30: **Mine (employee-a) = 3 / 175,00** e **Global = 4 / 375,00**.

## Versão em execução — [`00-versao.txt`](evidencias/00-versao.txt)

`docker compose ps` mostra as duas imagens com a tag do commit; `GET /health` responde
`{"status":"ok","version":"28688c5d…"}`; o label `org.opencontainers.image.revision` dos contêineres e a primeira
linha de log (`ExpenseHub web/worker iniciado na versão 28688c5d…`) confirmam a mesma versão.

## E01 — Fluxo normal — [`E01-fluxo-normal.txt`](evidencias/E01-fluxo-normal.txt)

- reportId: `9bac42f5-b3e3-40b6-a03c-ac1c2fe9919f` (employee-a, `Mine`).
- `POST /api/reports` → **202**, `Location: http://localhost:5245/api/reports/9bac42f5-…`, envelope `Pending`.
- Acompanhamento: `Pending` → `Completed` em poucos segundos (primeira solicitação após a subida do ambiente).
- Resultado: `totalCount 3`, `totalAmount 175`, Approved 2 / 150, Paid 1 / 25, Draft/Submitted/Rejected com 0.
- `GET …/excel` → **200**, `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`,
  `Content-Disposition: attachment; filename=report-9bac42f5-….xlsx`. A planilha foi aberta como pacote Open XML
  e transcrita no arquivo: id, escopo, período, instante de geração e a tabela com os cinco estados e o total, com
  quantidades e valores em **células numéricas** iguais ao resultado salvo.
- A mesma planilha foi aberta no **Microsoft Excel 16.0** por automação
  ([`E01b-excel-aberto-no-excel.txt`](evidencias/E01b-excel-aberto-no-excel.txt)): aba `Relatorio`, intervalo
  `A1:C13`, quantidades e valores lidos pelo Excel como números (`Double`) e `SOMA(C8:C12) = 175`.
- Global (auditor) `636ffd8f-f07f-4803-9640-187f0102a061`: **4 / 375**.
- Logs correlacionados pelo id: web `registrado como Pending` → `publicado com confirmação do broker`; worker
  `recebida` → `iniciado (tentativa 1)` → `concluído com 3 reembolsos` → `confirmada (ack) com desfecho Completed`.

## E02 — Worker parado — [`E02-worker-parado.txt`](evidencias/E02-worker-parado.txt)

1. `docker compose stop worker`.
2. `POST` (employee-a, Mine) → **202**, reportId `a577e45f-7a49-45fb-96f5-b433741c48b2`.
3. Fila: `durable=True ready=1 unacked=0 consumers=0` — a mensagem confirmada está disponível e ninguém consome.
4. `GET` → `Pending`; `GET …/excel` → **409** (`O relatório está Pending…`).
5. `docker compose up -d worker` → o mesmo id sai de `Pending` e chega a `Completed` (3 / 175) cerca de 9 s depois, já contando a inicialização do contêiner do worker.
6. Fila depois: `ready=0 unacked=0 consumers=1`. Logs do worker com o mesmo id: recebida, iniciado, concluído, ack.

`Ready` e `Unacked` são métricas do RabbitMQ, não os estados `Pending`/`Processing` da aplicação; a correlação
foi feita pelo reportId nos logs e no envelope.

## E03 — Duplicata — [`E03-duplicata.txt`](evidencias/E03-duplicata.txt)

1. Antes: `9bac42f5-…` `Completed`, `completedAt 2026-11-05T00:15:15.4819787Z`, `generatedAt 2026-11-05T00:15:15.4318189Z`, total 175.
2. `docker compose restart worker` (descarta qualquer estado em memória).
3. Republicação do payload `{"reportId":"9bac42f5-…"}` na fila pelo painel de gerenciamento
   (`POST /api/exchanges/%2F/amq.default/publish`, `delivery_mode 2`) → `routed=True`.
4. Worker: `Relatório 9bac42f5-… já está Completed; duplicata ignorada sem novo efeito` e ack com desfecho
   `Duplicate`. Fila volta a `ready=0 unacked=0`.
5. Depois: envelope **idêntico** (mesmos `completedAt`, `generatedAt` e resultado); a listagem de employee-a
   não ganhou solicitação nova.

A proteção é o estado `Completed` salvo no banco (`ReportProcessor`), por isso vale após o reinício. Reentregar a
mesma mensagem é diferente de um novo `POST`, que sempre cria outro id.

## E04 — Isolamento — [`E04-isolamento.txt`](evidencias/E04-isolamento.txt)

Requisições na ordem do arquivo:

| Cenário | Resposta |
| --- | --- |
| Anônimo lista / solicita | 401 / 401 |
| employee-a pede `Global` | 403 |
| auditor (sem Employee) pede `Mine` | 403 |
| employee-b consulta / baixa Excel do relatório de employee-a | 404 / 404 |
| auditor consulta / baixa Excel do relatório de employee-a | 404 / 404 |
| employee-a consulta o relatório Global do auditor | 404 |
| employee-b lista | 200 com somente as próprias solicitações (lista vazia) |
| auditor consulta o próprio Global | 200 |
| Período invertido / escopo `Everyone` / campo `ownerId` | 400 / 400 / 400 |
| Id inexistente | 404 |

## E05 — Persistência e versão — [`E05-persistencia-versao.txt`](evidencias/E05-persistencia-versao.txt)

1. `docker compose up -d --force-recreate web worker` (sem `down -v`; volumes preservados).
2. `docker compose ps`: contêineres novos (`Up 13 seconds`) com as mesmas imagens publicadas `…:28688c5d…`;
   `/health` e os logs de inicialização confirmam a versão.
3. O token emitido antes da recriação continuou válido (chaves de Data Protection em `/data/keys` no volume).
4. `GET /api/reports/9bac42f5-…` depois da recriação → resultado **igual** ao anterior; o Excel também foi baixado
   de novo (**200**).

## E06 (complementar) — Broker indisponível — [`E06-broker-indisponivel.txt`](evidencias/E06-broker-indisponivel.txt)

Não é obrigatório, mas confirma em execução o tratamento já coberto por testes:

1. `docker compose stop rabbitmq` (somente o broker).
2. `POST` (employee-a) → **503** com o envelope do id persistido `2ef6e840-85a5-4ad1-974e-c5665ac96f3f`,
   `status Pending` e `error.code = PublishNotConfirmed` (mensagem sanitizada, sem detalhes internos).
3. `GET` desse id → **200**, `Pending`. Log da web: `registrado como Pending` → `persistido, mas a publicação
   não foi confirmada` (`BrokerUnreachableException`).
4. `docker compose start rabbitmq` → `healthy`. Um **novo** `POST` → **202** e `Completed` (id
   `ee89dc31-43da-437e-b82a-edbcfa647c4a`); o worker reconectou sozinho.
5. O pedido `2ef6e840-…` continua `Pending`: nenhuma mensagem foi confirmada, então reiniciar o worker ou o broker
   não o recupera — exatamente a janela sem Transactional Outbox descrita abaixo.

## Diagnóstico

- **Aceitação ≠ conclusão.** O `202` significa que a solicitação foi salva e o broker confirmou a mensagem
  persistente numa fila durável. A conclusão é assíncrona e aparece no envelope (`status`, `completedAt`, `result`).
- **Worker parado ≠ broker indisponível.** Worker parado (E02): a mensagem foi confirmada e espera em `Ready`;
  ao voltar, o mesmo id é processado. Broker fora: não há confirmação, a web responde **503** com o id já
  persistido e `error.code = PublishNotConfirmed` (coberto por `ReportsControllerTests` e `ReportServiceTests`);
  esse pedido fica `Pending` sem mensagem.
- **Duplicata ≠ novo POST.** A reentrega da mesma mensagem é reconhecida pelo estado salvo e não gera efeito (E03);
  um novo `POST` cria outro id.
- **Janela sem Transactional Outbox.** Salvar e publicar são operações separadas. Se o processo cair entre o
  `SaveChanges` e a confirmação, ou se a confirmação se perder, a solicitação fica `Pending` sem mensagem.
  Reiniciar o worker não recupera uma mensagem que nunca foi publicada; não há republicação automática. O usuário
  recebe o id (503) e pode fazer um novo pedido.
- **Ack depois de salvar.** O worker só envia ack depois de gravar `Completed` + resultado (ou `Failed`). Se cair
  antes do ack, a mensagem volta e cai no caso de duplicata. Falha transitória (banco indisponível) volta à fila
  após 5 s; depois de 5 tentativas registradas, a solicitação vira `Failed` (`MaxAttemptsExceeded`), sem
  reencaminhamento infinito.

## Testes unitários (separados dos experimentos)

```bash
dotnet test sources/ExpenseHub.slnx
```

220 testes aprovados localmente; o job `test` do workflow `images` executa a mesma suíte antes de publicar as imagens.
