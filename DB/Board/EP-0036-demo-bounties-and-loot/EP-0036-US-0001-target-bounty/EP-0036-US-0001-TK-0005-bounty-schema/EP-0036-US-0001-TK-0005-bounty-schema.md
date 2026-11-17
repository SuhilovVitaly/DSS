---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0001-target-bounty
ticket: EP-0036-US-0001-TK-0005-bounty-schema
title: Схема фиксированных станционных bounty
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0001-TK-0001-contracts]
files_touched: 2
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:19:15Z
revision: 1
---

# Схема фиксированных станционных bounty

## Why

[История и полный пользовательский результат](../EP-0036-US-0001-target-bounty.md). Устранить зависимость выплаты от предварительного принятия, сохраняя станцию заказчика и однократность. Этот тикет выполняет только слой engine.

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
| `src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs` | существует :31 DemoJobsDefinition и :113 Validate | Optional Bounties, parsing/fingerprint/reference validation. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0001Tk0005Tests.cs` | новый файл этого тикета | Named tests ниже; реальный owning API, не копия бизнес-логики. |

Разрешены только эти 2 implementation files; Board status/evidence обновляются отдельно. Общий read-only grounding — в story; внешние prerequisites читаются только в canonical Board.

## Public API after the change

Планируется DemoBountyDefinition(string Id, string IssuerStationId, string TargetGroupId, long RewardTokens) и optional ImmutableArray<DemoBountyDefinition> Bounties в DemoJobsDefinition. Отсутствие старого поля = пустой список. No deadline/deposit. Сумма/issuer — content, не код клиента.

## Implementation steps

1. Добавить optional bounty catalog в jobs reader и fingerprint; ValidateReferences проверяет issuer в cluster.StationIds, target pirate:NN в диапазоне encounters.GroupCount, уникальные BountyId и TargetGroupId, положительную сумму.
2. Не менять календарную генерацию и deadlines перевозок: bounty definitions живут отдельно от JobOffer expiry.
3. Проверить совместимую загрузку jobs без нового поля и rejection unknown issuer/target/duplicate. Это engine schema ticket, runtime JSON принадлежит TK0004.

## Served acceptance criteria

- **AC-01:** Доска станции показывает её конкретные bounty, цель, разрешённый район поиска, награду и место выплаты; срок/залог отсутствуют.
- **AC-02:** Уничтоженная игроком цель до принятия доступна как требование к выплате; уничтоженная/недоступная цель без доказанной победы не выдаётся как невыполнимый активный заказ.
- **AC-03:** Принять/отменить означает включить/выключить отслеживание одного BountyId; отмена без штрафа/выплаты и не уничтожает право на награду. Повтор не создаёт копий.
- **AC-04:** Save/Load и повторные команды сохраняют исходные issuer/target/reward/Tracking; платёжная eligibility передаётся в US-0002, UI не обещает ретроактивную оплату за постороннюю группу.

Вклад ограничен engine; story AC принимается после всей dependency chain, не одним DTO или unit test.

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
- `DemoEp0036Us0001Tk0005Tests.MissingBounties_BackwardCompatible`
- `DemoEp0036Us0001Tk0005Tests.DuplicateTargetOrId_Rejected`
- `DemoEp0036Us0001Tk0005Tests.UnknownIssuerOrTarget_Rejected`
- `DemoEp0036Us0001Tk0005Tests.InvalidReward_Rejected`
- `DemoEp0036Us0001Tk0005Tests.BountyChange_ChangesFingerprint`

Из корня DSS; process-tree cap 300000 ms на каждый процесс. Named class corpus включает только перечисленные проверки этого тикета. Команда создаёт уникальный абсолютный run:
```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0001-TK-0005-bounty-schema/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0036Us0001Tk0005Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`; выполнить под таким же capped wrapper, без unbounded process. Для content-data достаточно loader/parity tests; для code layer `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs` под тем же cap. Native/game не запускать ради planning; будущий UI evidence отдельно от automated.

## Definition of Done

- Шаги выполнены в allowlist после dependencies; named tests и owning checks PASS либо конкретный исходный blocker записан.
- AC coverage и негативные cases подтверждены, false/duplicate/Load не дублируют деньги.
- Новые DTO и сохранение согласованы с prerequisite schemas; нет скрытого изменения баланса или доверия клиентским исходам.
- Board отражает фактические результаты; планирование не обозначено реализацией/приёмкой.
- Validation outputs только в указанном artifacts run; disposable файлы убраны, чужие изменения сохранены.

## Self-containment check

Описаны authority, identity, отсутствие acceptance prerequisite, frozen definitions, schema, dependencies, allowlist, шаги и named tests. Открытого продуктового вопроса по этому результату нет; численный content baseline — A-B05 для плейтеста, не обещание доказанного баланса.
