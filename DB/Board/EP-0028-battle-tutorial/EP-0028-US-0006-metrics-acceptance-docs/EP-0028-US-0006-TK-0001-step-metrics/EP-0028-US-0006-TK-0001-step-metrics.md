---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
ticket: EP-0028-US-0006-TK-0001-step-metrics
title: Метрики шагов обучения
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0004-TK-0003-battle-toggle, EP-0028-US-0003-TK-0005-debrief]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialMetrics.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialMetricsTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs
serves: [US-0006/AC-01]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Метрики шагов обучения

[История](../EP-0028-US-0006-metrics-acceptance-docs.md) · [Эпик](../../Documentation.md)

## Цель

Измерять «отвал» по шагам: локальный JSON с событиями каждого шага обучения рядом с отчётом боя.

## Decisions

A-08 — только локально, без персональных данных ([эпик](../../Documentation.md#допущения)). Инфраструктура — существующие `TacticalBattleMetrics.DirectoryPath` и `ReportPath` (`UI/Screens/TacticalBattle/TacticalBattleMetrics.cs:17-20,61-65`); файл `TacticalBattleMetrics.cs` не меняется.

## Assumptions

- Файл: `Path.ChangeExtension(TacticalBattleMetrics.ReportPath(battleId, dir), ".tutorial.json")`; перезаписывается атомарно (temp + `File.Move(overwrite)`, как `TacticalBattleMetrics.cs:49-57`) после каждого нового события.
- Время событий — `nowMs` наблюдения относительно первого наблюдения боя (без абсолютного времени пользователя, кроме `updatedUtc`, как в отчёте боя).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialMetrics.cs` | Нет файла | Новый писатель событий |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | Директор отдаёт `DrainEvents()` (US-0001-TK-0002); переключатель подсказок (US-0004-TK-0003) | После `Observe` и переключения — записать события (`hints-disabled`/`hints-enabled` при смене) |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialMetricsTests.cs` | Нет файла. Образец — `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMetricsTests.cs:18-58` (явный каталог) | Новые тесты |

## Public API after the change

```csharp
internal sealed class TutorialMetrics(string battleId, string? directory = null)   // directory ?? TacticalBattleMetrics.DirectoryPath
{
    internal string FilePath { get; }
    internal void Record(TutorialEvent e);                 // shown | completed | dismissed | expired
    internal void RecordHints(bool enabled, string phase, int turn, long atMs);
}
```

JSON: `{ "schemaVersion": 1, "battleId", "scenarioId", "seed", "updatedUtc", "events": [ { "step", "kind", "phase", "turn", "atMs" } ] }`.

## Implementation steps

1. Писатель с атомарной записью; ошибки IO/доступа → одна запись `TUTORIAL METRICS: …` в InterfaceLog, бой продолжается.
2. Сессия создаёт `TutorialMetrics` по `BattleId` первого наблюдения и пишет события.

## Acceptance criteria

1. Сценарий: показ → выполнение → закрытие другого → выключение подсказок даёт события в порядке возникновения.
2. Файл не содержит имён пользователя/путей профиля; запись атомарна; ошибка записи не бросает.

## Verification signal

Первым красным — `TutorialMetricsTests.StepEventsAreWrittenInOrderNextToBattleReport`; затем `HintsToggleIsRecorded`, `WriteFailureIsLoggedAndBattleContinues`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0006-tk0001-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
DSS_VALIDATION_ROOT="$RUN" dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~BattleMetricsTests|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=metrics.trx"
# финальная полная проверка эпика (один раз, после этого тикета)
dotnet build DeepSpaceSaga.sln > "$RUN/build.log"
DSS_VALIDATION_ROOT="$RUN" dotnet test DeepSpaceSaga.sln --results-directory "$RUN/full" --logger trx > "$RUN/test.log"
```

Падения полного прогона сравнить с базой (G-03): список падений до эпика против после; новые — исправить в соответствующем тикете-исправлении. Длинные наборы делить по тестовым проектам/ID; process-tree cap 300000 ms на процесс.

## Out of scope

Сетевые метрики; правка `TacticalBattleMetrics.cs`.

## Invariants

- Код вне `/TacticalBattle/`; без персональных данных.

## Known fallout

Нет.

## Definition of Done

- Шаги выполнены; named tests зелёные; полный прогон выполнен и классифицирован в «Журнал».
- Отдельный коммит и `git push`.

## Self-containment check

Путь файла, формат, атомарность и источники API указаны.

## Результат

- `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialMetrics.cs` (новый): `TutorialMetrics(battleId, directory)` с `ScenarioId`, `Seed`, `OriginMs`, `Warning`; `FilePath` = `Path.ChangeExtension(TacticalBattleMetrics.ReportPath(battleId, dir), ".tutorial.json")`; `Record(TutorialEvent)` → `shown|completed|dismissed|expired`, `RecordHints(enabled, phase, turn, atMs)` → `hints-enabled|hints-disabled` (`step: null`). После каждого события файл перезаписывается атомарно (temp + `File.Move(overwrite)`); IO/доступ → одна строка `TUTORIAL METRICS: …` на бой (по умолчанию в InterfaceLog), события остаются в памяти, бой продолжается. `TacticalBattleMetrics.cs` не менялся.
- JSON: `{ schemaVersion: 1, battleId, scenarioId, seed, updatedUtc, events: [ { step, kind, phase, turn, atMs } ] }`; `atMs` — время наблюдения относительно первого кадра боя (часы окна — `Environment.TickCount64`, в файл не пишутся).
- `TutorialSession`: `MetricsDirectory` (null — метрики выключены), `MetricsWarning`, `Metrics`; метрики создаются по `BattleId`/`ScenarioId`/`Seed` первого наблюдения; события директора забираются (`DrainEvents`) после `Observe`, кнопок панели и переключения подсказок; `hints-disabled/enabled` пишутся только при смене и после `dismissed` строки, закрытой выключением. Без метрик события остаются в директоре (прежнее поведение `DrainEvents` для тестов).
- `SkiaWindow.CreateTutorialSession`: `MetricsDirectory = TacticalBattleMetrics.DirectoryPath` (рядом с отчётом боя; `DSS_VALIDATION_ROOT/battle-metrics` в проверках).
- Тесты `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialMetricsTests.cs`: `StepEventsAreWrittenInOrderNextToBattleReport`, `DismissedAndExpiredStepsAreRecorded`, `HintsToggleIsRecorded`, `WriteFailureIsLoggedAndBattleContinues`, `NoMetricsWithoutDirectory`; `TutorialFlowTests.NewGameWithoutSavesStartsTutorialBattleAndNeverShowsTheMap` проверяет, что окно включает метрики в `TacticalBattleMetrics.DirectoryPath`.

Проверки 2026-11-14:

- Красный: `us0006-tk0001-20261114T195421/red.log` (нет API `MetricsDirectory`/`Metrics`).
- Целевой набор (`…Tutorial|BattleMetricsTests|TutorialFlowTests`) — **193/193** — `us0006-tk0001-20261114T195421/metrics.trx`, `metrics.log`; живые бои обучения из `TutorialFlowTests` пишут `*.tutorial.json` рядом с отчётами в `…/battle-metrics/`. Первый прогон дал 1 падение (`TutorialScreenIntegrationTests.RenderedFramesFeedTheDirectorAndPilotSelectionCompletesTheStep` читает `DrainEvents` напрямую) — исправлено: сессия забирает события только при включённых метриках (`metrics-first.log`).
- Полная проверка эпика (один раз): `dotnet build DeepSpaceSaga.sln` — 0 ошибок, 0 предупреждений (`us0006-tk0001-20261114T195421/build.log`); `dotnet test DeepSpaceSaga.sln --no-build --logger trx` с `DSS_VALIDATION_ROOT` — `us0006-tk0001-full/` (`test.log`, 6 TRX):

| Проект | Всего | Прошло | Упало |
|---|---|---|---|
| DeepSpaceSaga.Client.Tests | 2649 | 2641 | 8 |
| DeepSpaceSaga.Contracts.Tests | 199 | 199 | 0 |
| DeepSpaceSaga.Motion.Tests | 1283 | 1282 | 1 |
| DeepSpaceSaga.Engine.Tests | 2952 | 2341 | 611 |
| DeepSpaceSaga.EconomyBalance.Tests | 61 | 58 | 3 |
| DeepSpaceSaga.Performance.Tests | 16 | 6 | 10 |
| **Итого** | **7360** | **6727** | **633** |

- Классификация (G-03): упавшие тесты (по классам) запущены на базе `d8ab8f71` в отдельном `git worktree` (`us0006-tk0001-base/`, `base-*.trx`, `failed-base.txt`; worktree удалён). На базе падают те же **633** теста; `us0006-tk0001-full/new-failures.txt` — **0 новых**, `pre-existing-failures.txt` — 633, `base-only-failures.txt` — 0. Состав: Engine — корпуса System/Cluster/FullMap correctness, ClusterSaveState/Voyage/ResourcePlacement, OrbitalDocking, CatalogCompatibility, MasterSeed, MapEnvironmentSave, FullMapLocalLoad, `ContentSyncTests` (нет `pwsh`); Client — карта/камера (ApproachCamera, ClusterInteractionEvidence, FullMapRenderEvidence, MapLayerControls, TerritoryRendering); EconomyBalance — LongVoyageRunner; Performance — отчёты; Motion — `ForbiddenCallsTests`.
- `ForbiddenCallsTests`: `forbidden-scan.ps1` (US-0001-TK-0004) по базе и HEAD — по 183 находки, `forbidden-diff.txt` пуст (эпик не добавил находок).

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`.

Расширение allowlist без остановки по указанию пользователя 2026-11-14 («сделай без остановок»): `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` — одна строка `MetricsDirectory = TacticalBattleMetrics.DirectoryPath` в `CreateTutorialSession`: метрики включаются только окном, чтобы сотни юнит-тестов сессии не писали файлы в LocalAppData и не шумели в общий Interface.log.

Расширение allowlist без остановки по указанию пользователя 2026-11-14 («сделай без остановок»): `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs` — одна проверка, что настоящий запуск обучения включает метрики в каталоге отчёта боя.

Узкие решения исполнителя: (1) `atMs` относительно первого наблюдения (ticket Assumptions), а не машинные часы окна; (2) события `hints-*` без шага (`step: null`), только при фактической смене; (3) отметка «подсказки выключены с начала боя» не пишется (тикет требует событие только при смене); (4) без метрик сессия не забирает события директора — прежний контракт `DrainEvents`.
