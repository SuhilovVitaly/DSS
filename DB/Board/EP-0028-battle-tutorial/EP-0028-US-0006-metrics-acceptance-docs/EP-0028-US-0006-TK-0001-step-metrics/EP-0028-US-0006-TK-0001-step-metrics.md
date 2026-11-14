---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
ticket: EP-0028-US-0006-TK-0001-step-metrics
title: Метрики шагов обучения
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0004-TK-0003-battle-toggle, EP-0028-US-0003-TK-0005-debrief]
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialMetrics.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialMetricsTests.cs
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

## Журнал

2026-11-14T15:04:43Z — тикет создан.
