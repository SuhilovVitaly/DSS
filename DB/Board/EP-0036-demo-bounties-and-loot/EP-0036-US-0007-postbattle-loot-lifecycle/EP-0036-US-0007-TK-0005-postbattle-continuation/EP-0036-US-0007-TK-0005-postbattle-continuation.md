---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0007-postbattle-loot-lifecycle
ticket: EP-0036-US-0007-TK-0005-postbattle-continuation
title: Checkpoint пообъектного лута и восстановление UI
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: local-client
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0007-TK-0003-postbattle-lifecycle]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:19:08Z
revision: 1
---

# Checkpoint пообъектного лута и восстановление UI

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

[EP-0036-US-0007-postbattle-loot-lifecycle](../EP-0036-US-0007-postbattle-loot-lifecycle.md). Checkpoint пообъектного лута и восстановление UI. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

## Decisions

Нормативные D-36-01…28 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Для этого тикета: лут конкретного объекта после победы; одна попытка демонтажа/сбора лома; целый trophy без durability/установки; cargo по массе; шансы и 50%-броски определены пользователем. Ранее разрешённая декомпозиция не означает исполнение/приёмку. Последняя запись 2026-11-17T19:19:08Z — время записи, не отправки.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API проверять по prerequisite diff до исполнения.
- Балансовые A в шагах/каталоге — стартовый плейтестовый baseline, не утверждение доказанного баланса.
- Runtime rejects stale/duplicate identity через единый campaign pipeline; запись rejected receipt не равна выдаче добычи.
- Все изменения ограничены EP-0036 результатом; при непредвиденном drift пересмотреть этот allowlist до расширения diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | ExecuteDemoCampaignAsync:234, continuation checkpoint:207. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.Autosave.cs` | Existing autosave gate/serialized captures. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0007Tk0005Tests.cs` | Новый local integration tests (existing Engine test surface). | Только шаги данного тикета. |

Разрешены 3 implementation files, включая тесты; один layer local-client. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

No new public transport: существующий ExecuteDemoCampaignAsync и snapshots получают Rewards из US3 contract. Полностью frozen loot phase живёт в scenario campaign data, active combat save не вводить.

## Implementation steps

1. После begin loot/успешной loot команды/finish записать authoritative continuation checkpoint через existing serial save route
2. storage failure не выдаёт ложный durability success, повтор не применяет gameplay повторно. Сохранение LootOpen включает sources/poses/RNG results/attempts и battle commit receipt
3. Load восстанавливает postbattle projection, не prebattle replay. Autosave не перезаписывает LootOpen старым pending prebattle checkpoint. Close pending IO завершать штатно. Matching test project Engine.Tests допустим по существующей integration convention.

## Invariants

- Engine владеет шансом/RNG, capacity, деньгами и подтверждённым результатом; UI не вычисляет новый исход.
- Stable BattleId/ObjectId/module slot/command identity, phase и terminal attempt переживают Load. Не reroll и не дублировать cargo/postings.
- Текущий single inventory/PlayerCredits сохраняются; новый trophy только обычный продаваемый item, не отдельный wallet.
- Успешный salvage выдаёт исправный commodity; не вводить прочность 0–100%, ремонт или установку трофея.
- 50%-бросок касается предметов, не пилота. Самоуничтожение не создаёт неоговорённый AoE.
- CargoFull до попытки не тратит RNG/попытку. Generic catalog corruption отклоняется до публикации нового мира.

## Out of scope

Пытки/вербовка, установка трофеев, новая модульная повреждаемость/ремонт, мировые persistent вреки, произвольная фракционная симуляция, несвязанные исправления и публикация. Арест реализуется отдельно US9, инженер US8; подключать их API по prerequisites, не копировать правила.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0007Tk0005Tests.LootOpenRoundtripResumesFrozenMap`
- `DemoEp0036Us0007Tk0005Tests.PartialCollectionRoundtrip`
- `DemoEp0036Us0007Tk0005Tests.CheckpointFailureRetryNoDoubleGrant`
- `DemoEp0036Us0007Tk0005Tests.AutosaveDoesNotRestorePrebattle`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0007-TK-0005-postbattle-continuation/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0007Tk0005Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist; для content-only не требуется. Длительный corpus делить по полным IDs, не расширять лимит. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах и owning layer, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants, нет второго источника inventory/денег.
- Тесты/build/format не выдаются за успешные без выполнения; native только при отдельной разрешённой проверке на меньшем мониторе.
- Временные результаты убраны по AGENTS; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite создатели новых файлов указаны. Общие интеграционные границы распределены US3 (loot ledger), US6 (module salvage), US7 (lifecycle), US8 (engineer), US9 (custody). Не исполнять scaffold без prerequisite; draft означает ожидаемую сверку составленного плана, не отсутствующую игровую реализацию.

## Execution scope correction

Also update BattleLootPresentation.cs and TacticalBattleFlow.cs so a transport/storage failure retains the exact command identity for retry; otherwise a new UI identity could repeat an already-applied collect or leave a closed finish awaiting acknowledgement. This is the client half of the required no-double-grant checkpoint retry contract. Local connection disposal drains the checkpoint gate before disposing the engine.

## Execution — 2026-11-17

Victory-causing move/fire/debug actions and every successful loot action now write the current continuation before returning success. A pending failed write is retried under the checkpoint gate before another loot command; engine receipts retain exactly-once effects. UI retries preserve original identities; finish waits for durable acknowledgement. Disposal drains active checkpoint IO before engine disposal. Existing save gate already excludes prebattle replacement when WorldEncounter is Finished, so no autosave-file edit was necessary. Authored real file/failed-replace/autosave/load tests; Client (including LocalClient) build/scoped format/diff PASS, tests NOT RUN until US7 boundary. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us7-tk5-build.log.

## Independent review correction
Scope includes TacticalBattleFlow.cs and TacticalBattleScreen.cs plus Client/Engine US7 regression tests. Cached victory receipts must not checkpoint a closed ledger; an unavailable winning response must recover the authoritative frozen map without treating the failed write as success.

Review corrections verified at US7 boundary: Engine 13/13 (us7-engine-review.log), Client 14/14 including bounty regression (us7-client-review.log), under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/. Both real storage failure recovery and stale receipt after a new battle are covered. No game launch.

## Owner review correction 6
Extend scope to SimulationEngine.DemoCampaign.cs: save command receipt outcomes without derived Rewards snapshots; exact live retries stay unchanged, restored compact retries attach current Rewards and current revision without replaying effects. Existing integration tests add bounded save growth and retry coverage.

Independent review follow-up: LocalGameSessionConnection checkpoints only newly committed loot commands, not historical Applied receipt replays; pending failed writes still retry first. Regression closes A, starts B, retries Finish A and checks pending state remains clear. Extend test scope to EP34 US6 SaveLoad receipt comparison by serialized value, avoiding ImmutableArray reference equality.
