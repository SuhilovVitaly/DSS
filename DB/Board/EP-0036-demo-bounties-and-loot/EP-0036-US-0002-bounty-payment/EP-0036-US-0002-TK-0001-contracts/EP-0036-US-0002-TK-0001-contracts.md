---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0002-bounty-payment
ticket: EP-0036-US-0002-TK-0001-contracts
title: Контракт однократного получения bounty по BountyId
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0036-US-0001-TK-0003-client]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 3
---

# Контракт однократного получения bounty по BountyId

## Why

[История и полный пользовательский результат](../EP-0036-US-0002-bounty-payment.md). Устранить зависимость выплаты от предварительного принятия, сохраняя станцию заказчика и однократность. Этот тикет выполняет только слой contracts.

## Decisions

- **D-36-01:** «Давай на станции заказчика. Зафиксируй это в тикетах через сабагента». Выплата только при подтверждённой стыковке со станцией заказчика; ранее записано 2026-11-17T18:59:55Z.
- **D-36-29:** «Да, без срока и залога».
- **D-36-30:** «Да, свободная отмена без штрафа и награды». Отмена сама по себе не выдаёт деньги.
- **D-36-31:** «Да: станция выплатит награду за уже уничтоженную группу».
- **D-36-32:** «За конкретную группу, на которую у этой станции есть заказ; достаточно подтверждённой победы игрока».
- **D-36-33:** «Да, отмена только убирает заказ из активного журнала; право на однократную награду сохраняется».

D-36-29…33 записаны 2026-11-17T19:19:15Z (время записи, не отправки). Предварительное принятие и непрерывное отслеживание не являются условиями выплаты.

## Assumptions

- **A-B01:** один persistent BountyId на конкретную группу в каталоге кампании, один issuer; дубликаты TargetGroupId запрещены при загрузке. Ключ выплаты — BountyId в сохранённой кампании, не CommandId/ContractId/визит/acceptance attempt. Повторное принятие меняет только Tracking.
- **A-B02:** определения BountyId, issuer, target и обещанная сумма материализуются однократно в campaign state и не перегенерируются при открытии доски, отмене или календарной публикации перевозок. Новое сохранение без bounty section получает определения из совместимого каталога, включая сопоставление с уже сохранёнными победами; несовместимый каталог не публикуется.
- **A-B03:** сумма — целые неотрицательные Tokens, конфигурация требует положительную награду. Нет deadline/deposit/cancellation fee; временные поля перевозок не переиспользуются с фиктивным бесконечным сроком.
- **A-B04:** состояние отслеживания отдельно от eligibility и Paid. Погибшая цель без подтверждённой победы игрока отображается недоступной, не создаёт невыполнимый активный заказ. Клиент не определяет eligibility.

Имена новых API ниже — проект. Workflow approval разрешает подготовку тикета, не является human approval кода/баланса или поручением реализовать.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | существует, :4 общий command, :11 snapshot, :35 rewards record | Добавить optional Bounties/BountyPayment в DemoRewardsSnapshot; не менять чужие sections. |
| `src/DeepSpaceSaga.Contracts/BountyPaymentContracts.cs` | новый файл | Claim command, due/paid projections and receipt. |
| `tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0036Us0002Tk0001Tests.cs` | новый файл этого тикета | Named tests ниже; реальный owning API, не копия бизнес-логики. |

Разрешены только эти 3 implementation files; Board status/evidence обновляются отдельно. Общий read-only grounding — в story; внешние prerequisites читаются только в canonical Board.

## Public API after the change

Планируется ClaimBountyCommand(string CommandId,long ExpectedRevision,string BountyId) : DemoCampaignCommand.
BountyPaymentEntry(string BountyId,string IssuerStationId,string TargetGroupId,string? VictoryBattleId,string State,long DueTokens,long PaidTokens,string? PostingId,long? PaidAtGameTimeMs,string? Reason).
BountyPaymentSnapshot(ImmutableArray<BountyPaymentEntry> Entries); optional DemoRewardsSnapshot.BountyPayment.
States unavailable / claimable / paid. Reasons bounty_not_found, bounty_no_verified_victory, bounty_wrong_station, bounty_already_paid. ContractId не ключ выплаты и не обязательный input.

## Implementation steps

1. Описать typed Claim без station/amount/outcome входов: Engine берёт их из frozen order/history.
2. Due отдельно от cash, Paid с durable PostingId и точным BattleId. Для untracked/cancelled order DTO одинаково представляет claimable.
3. Сохранить backward-compatible optional section и существующий CommandId/ExpectedRevision pipeline.

## Served acceptance criteria

- **AC-01:** Подтверждённая победа игрока над точной целью существующего bounty даёт Claimable независимо от предварительного принятия/отмены. Чужая группа, поражение, ничья, уход и неподтверждённый результат не подходят.
- **AC-02:** Claim доступен только при стыковке с issuer; платит ровно один раз по BountyId. Нет автоматической выплаты после боя; новые CommandId и повтор принятия/отмены не обходят Paid.
- **AC-03:** Единственный баланс PlayerCredits увеличивается на frozen reward; durable posting содержит bounty/issuer/target/battle, сумму и время. Due и Paid раздельны, bounty не маркируется пассажирским доходом и не смешивается с лутом.
- **AC-04:** Claims без предварительного принятия, после отмены и после Load доступны через реальный экран станции; ошибочные/повторные команды не меняют деньги/проводки, повреждённые сохранения отклоняются до публикации.

Вклад ограничен contracts; story AC принимается после всей dependency chain, не одним DTO или unit test.

## Out of scope

Новые пиратские цели, универсальные награды за любого врага, штрафы/залог/deadline, другой issuer, лут/арест, дополнительный cash wallet, установка трофеев, реализация соседних эпиков и публикация.

## Invariants

- EngineRequirements: деньги и результаты принадлежат Engine; ошибочная команда не даёт частичной мутации.
- Один BountyId в campaign — одно право выплаты; Tracking/CommandId/открытие экрана не создают новую награду.
- Только frozen issuer/target/reward и валидированная история собственной победы; Retired/исчезновение сами по себе не evidence.
- Нет срока, залога, платы за отмену или автоматической выплаты после победы; paid не может стать claimable после отмены.
- Save/Load валидируется до публикации. Интеграция использует existing ExecuteDemoCampaign hash/revision/candidate; никакой конкурирующей SaveFormat версии.

## Tests

Будущие проверки, **NOT RUN**:
- `DemoEp0036Us0002Tk0001Tests.Claim_RequiresBountyIdNotAcceptedContract`
- `DemoEp0036Us0002Tk0001Tests.DueAndPaid_AreDistinct`
- `DemoEp0036Us0002Tk0001Tests.Projection_RoundTripsPostingAndBattleIdentity`

Из корня DSS; process-tree cap 300000 ms на каждый процесс. Named class corpus включает только перечисленные проверки этого тикета. Команда создаёт уникальный абсолютный run:
```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0002-TK-0001-contracts/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0036Us0002Tk0001Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <absolute-run>/build`; выполнить под таким же capped wrapper, без unbounded process. Для content-data достаточно loader/parity tests; для code layer `dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/DemoCampaign.cs src/DeepSpaceSaga.Contracts/BountyPaymentContracts.cs` под тем же cap. Native/game не запускать ради planning; будущий UI evidence отдельно от automated.

## Definition of Done

- Шаги выполнены в allowlist после dependencies; named tests и owning checks PASS либо конкретный исходный blocker записан.
- AC coverage и негативные cases подтверждены, false/duplicate/Load не дублируют деньги.
- Новые DTO и сохранение согласованы с prerequisite schemas; нет скрытого изменения баланса или доверия клиентским исходам.
- Board отражает фактические результаты; планирование не обозначено реализацией/приёмкой.
- Validation outputs только в указанном artifacts run; disposable файлы убраны, чужие изменения сохранены.

## Self-containment check

Описаны authority, identity, отсутствие acceptance prerequisite, frozen definitions, schema, dependencies, allowlist, шаги и named tests. Открытого продуктового вопроса по этому результату нет; численный content baseline — A-B05 для плейтеста, не обещание доказанного баланса.
