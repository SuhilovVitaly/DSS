---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0012-loot-debug-commands
ticket: EP-0036-US-0012-TK-0001-loot-debug-overrides
title: Отладочные переопределения бросков лута и бонуса инженера
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0006-TK-0003-engine, EP-0036-US-0003-TK-0003-engine, EP-0036-US-0008-TK-0002-identity-and-skill]
files_touched: 6
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Отладочные переопределения бросков лута и бонуса инженера

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

[EP-0036-US-0012-loot-debug-commands](../EP-0036-US-0012-loot-debug-commands.md). Отладочные переопределения бросков лута и бонуса инженера. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/FogOfWarContracts.cs` | SetFogOfWarRequest — образец debug DemoCampaignCommand, DemoDebugSettings. | Append SetLootDebugRequest и поля DemoDebugSettings. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | Dispatch SetFogOfWarRequest. | Dispatch SetLootDebugRequest. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | US3 TK3 броски 50%/лома. | Точка переопределения исхода броска. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.ModuleSalvage.cs` | US6 TK3 бросок демонтажа. | Точка переопределения исхода попытки. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.ShipEngineer.cs` | US8 TK2 GetAssignedEngineerBonusFraction. | Process-level override бонуса. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0012Tk0001Tests.cs` | Новый. | Named tests ниже. |

Разрешены 6 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

Проект: SetLootDebugRequest(CommandId, ExpectedRevision, int? EngineerBonusPp, string? DestructionRolls(auto|keep|destroy), string? Attempts(auto|success|fail)). Настройки process-level как fog_of_war: не сериализуются в save, по умолчанию auto/null. Исход переопределённого броска записывается в ledger тем же путём, что и обычный, поэтому повтор/Load его не перебрасывают. Read-only snapshot текущих настроек в DemoDebugSettings.

## Implementation steps

1. Добавить request и dispatch по образцу SetFogOfWarRequest; ответ Applied только после фиксации настройки.
2. EngineerBonusPp: null = persisted навык; 0 или 10…15 — временное значение getter US8; persisted значение не меняется.
3. DestructionRolls/Attempts подменяют только результат RNG в момент первого броска; уже записанные результаты не меняются; CargoFull/одна попытка/закрытая сессия работают как обычно.
4. Сохранение во время override сохраняет полученные результаты, но не сам override.

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0012Tk0001Tests.OverrideNotPersistedInSave`
- `DemoEp0036Us0012Tk0001Tests.EngineerBonusOverrideDoesNotChangeSkill`
- `DemoEp0036Us0012Tk0001Tests.ForcedKeepAndSuccessRecordedOnce`
- `DemoEp0036Us0012Tk0001Tests.RecordedResultsNotRewrittenByOverride`
- `DemoEp0036Us0012Tk0001Tests.RejectedRequestNotAcknowledged`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0012-TK-0001-loot-debug-overrides/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0012Tk0001Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.

## Scope correction
DemoDebugSettings actually lives in Contracts/DebugTuning.cs. Add read-only DemoCampaignSnapshot.DebugSettings and process mutation helper in SimulationEngine.DemoDebug.cs so console reads the authoritative process settings without save persistence. SetLootDebugRequest is an explicit narrow exception to combat/loot/captured gates; normal gameplay commands remain blocked. Persist neither debug request values nor debug projection in campaign save data.

## Execution
Process overrides for engineer0/10..15/auto, destructionauto/keep/destroy and attemptauto/success/fail. Applied through explicit engine dispatch, allowed during combat/loot; RNG draw progression still advances and only future decisions change. No debug values in save data; read-only process settings in live snapshot. Engine/test build PASS after test namespace correction (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us12-tk1-build-repeat.log); tests deferred until both US12 tickets complete.
