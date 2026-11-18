---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0007-postbattle-loot-lifecycle
ticket: EP-0036-US-0007-TK-0003-postbattle-lifecycle
title: Подтверждённая фаза лута до закрытия боя
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0003-TK-0003-engine, EP-0036-US-0006-TK-0003-engine, EP-0036-US-0007-TK-0002-carrier-selfdestruct]
files_touched: 6
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:19:08Z
revision: 2
---

# Подтверждённая фаза лута до закрытия боя

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

[EP-0036-US-0007-postbattle-loot-lifecycle](../EP-0036-US-0007-postbattle-loot-lifecycle.md). Подтверждённая фаза лута до закрытия боя. Груминг заменяет прежнюю агрегированную loot-модель проверяемым результатом этого тикета.

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
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs` | Новый postbattle lifecycle/frozen battlefield helpers. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | Exit:150–175 сейчас commit только на выходе, clear session. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | :174 SetSimulationSpeed отклоняет ≠0 только при _tacticalBattleSession. | Расширить gate на phase=LootOpen (шаг 6), больше ничего. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs` | CommitSortieOutcome:17 duplicate guard, encounter settlement:63. | Только шаги данного тикета. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | Execute:39–45 блокирует все команды при active battle, dispatch:105. | Только шаги данного тикета. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0007Tk0003Tests.cs` | Новый lifecycle tests. | Только шаги данного тикета. |

Разрешены 6 implementation files, включая тесты; один layer engine. Read-only зависимости и source grounding находятся в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Проект Phase Combat->LootOpen->Closed, BeginPostBattleLoot(session) при confirmed player victory. Frozen battlefield DTO/data содержит pose и ID всех selectable sources и results, без нового активного боя. ILootCampaignCommand marker BattleId разрешён в LootOpen; прочие campaign commands сохраняют запрет. FinishBattleLoot(ConfirmDiscard=false) returns ConfirmDiscardRequired если остаток; true closes/discards атомарно.

## Implementation steps

1. До первого показа лута committed battle outcome/потери игрока/encounter retirement применить ровно один раз и materialize frozen sources одним commit. Перенести owning commit moment с exit на victory, не дублировать CommitSortieOutcome при exit
2. его guard должен переживать load/повтор того же BattleId. Loot commands возможны пока underlying battle отображается: gate проверяет explicit phase+BattleId+current session/campaign, бой не возобновляется. Construct source DTO/poses из фактических statuses/equipment/ammo/pilot before clearing
3. serialized data US3 используется для restore postbattle map без active battle save. Exit без подтверждения не закрывает
4. confirmed сохраняет discarded quantities и closes, subsequent exit/command no side effects. Для поражения/aborted нет LootOpen. Snapshot/response Rewards публиковать на каждой операции
5. no unrelated trade allowed during loot; исключение — JettisonCargoCommand US10 TK3 (ILootCampaignCommand, D-36-35).
6. Мировые часы стоят от победы до закрытия LootOpen (D-36-37 сверка): пока phase=LootOpen, SetSimulationSpeed≠0 отклоняется той же причиной, что и при активном бое (SimulationEngine.cs:174 сейчас опирается на _tacticalBattleSession), пайки/жалованье/движение пиратов/новые встречи не тикают. Load в LootOpen восстанавливает паузу.

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
- `DemoEp0036Us0007Tk0003Tests.VictoryCommitsBeforeFirstLoot`
- `DemoEp0036Us0007Tk0003Tests.ExitDoesNotSettleTwice`
- `DemoEp0036Us0007Tk0003Tests.LoadRestoresFrozenSourcesNotCombat`
- `DemoEp0036Us0007Tk0003Tests.WrongBattleRejected`
- `DemoEp0036Us0007Tk0003Tests.ExitWarningThenDiscard`
- `DemoEp0036Us0007Tk0003Tests.DefeatHasNoLoot`
- `DemoEp0036Us0007Tk0003Tests.WorldClockFrozenDuringLootOpen`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0007-TK-0003-postbattle-lifecycle/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0007Tk0003Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist; для content-only не требуется. Длительный corpus делить по полным IDs, не расширять лимит. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах и owning layer, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants, нет второго источника inventory/денег.
- Тесты/build/format не выдаются за успешные без выполнения; native только при отдельной разрешённой проверке на меньшем мониторе.
- Временные результаты убраны по AGENTS; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite создатели новых файлов указаны. Общие интеграционные границы распределены US3 (loot ledger), US6 (module salvage), US7 (lifecycle), US8 (engineer), US9 (custody). Не исполнять scaffold без prerequisite; draft означает ожидаемую сверку составленного плана, не отсутствующую игровую реализацию.

## Execution preflight correction — 2026-11-17

US12 SetLootDebugRequest is an explicit non-gameplay exception to combat/LootOpen command gates. It changes only process-local debug overrides for future rolls and must never reroll or rewrite existing loot. Other unrelated campaign commands remain blocked.

## Execution scope correction

Lifecycle requires extending actual direct-entry gates beyond the planned six files: SimulationEngine.Time.cs, Rearm.cs, PirateWorldStep.cs, PassengerContracts.cs, TransportFailure.cs, SensorContactReports.cs, WorldEncounterBattle.cs, Dialogue/SimulationEngine.Dialogue.cs, TargetBounty.cs and shared BattleLoot.cs/BattleLootData.cs. These changes only apply LootOpen pause/command restrictions, frozen reward projection and self-destruct cause. SimulationEngine.cs also validates and restores frozen loot before publishing loaded state. Existing combat save restriction remains combat-only; completed loot is serializable.

## Execution — 2026-11-17

Victory atomically settles owned sortie and encounter with frozen loot before the response, then clears live combat while preserving command/clock gates. Loot commands return authoritative Rewards; finish requires explicit discard when unclaimed sources remain. Load validates victory, frozen map, source identity/status/pose, exact scrap output and module/consumable mappings, then restores pause without combat. Independent review found missing mapping validation; corrected before publication with corruption regression cases. Wrong-battle Exit is validated before transition. Engine build/scoped formatting/diff PASS; tests NOT RUN until US7 completion. LocalClient checkpoint and map-panel integration remain the designated dependent tickets. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us7-tk3-reviewed-build.log.

## Bounty regression fixture adaptation

Extend test allowlist to DemoEp0036Us0001Tk0002Tests.cs and Client DemoEp0036Us0002Tk0003Tests.cs: old victory helpers previously exited directly to world; they now explicitly confirm closing the added loot phase before tracking/claim-at-station checks. This preserves their original bounty purpose without weakening the LootOpen world-action gate. Regression run follows publication.
