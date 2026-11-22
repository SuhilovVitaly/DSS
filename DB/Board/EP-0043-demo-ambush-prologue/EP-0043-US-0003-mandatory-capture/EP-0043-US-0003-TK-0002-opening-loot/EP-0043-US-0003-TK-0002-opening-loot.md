---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0003-mandatory-capture
ticket: EP-0043-US-0003-TK-0002-opening-loot
title: Выживший и результаты стартового боя
stage: approved
layer: engine
depends_on: [EP-0043-US-0003-TK-0001-capture-projection]
files_touched: 5
serves: [AC-01, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Выживший и результаты стартового боя

## Why

D-03 и D-04. Допрос требует хотя бы одного реально взятого на борт участника засады. [История](../EP-0043-US-0003-mandatory-capture.md).

## Decisions

D-03 и D-04. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs:14` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs:11` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs:14` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:12` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0003Tk0002Tests.cs` | Новый файл в DeepSpaceSaga.Engine.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Увязывать открытие добычи с точным BattleId пролога через альтернативу обычному WorldEncounter binding. При наличии DisabledDrone использовать существующих живых пилотов. При победе без такого пилота добавить один источник EscapePod без иных лут-строк, с ID на базе стабильного выбора реального вражеского пилота боя. По ordinal ID выбрать первого участника с Build, задать уникальный ID источника и не дублировать уже доступного пилота. Расширить проверку загрузки и обычный путь ArrestPilot на этот тип, сохранив запись бой/источник/пилот, каюты и RNG существующей добычи.

## Out of scope

Переработка общей добычи, вместимости и сценариев поражения. Любые изменения вне allowlist.

## Invariants

Арест относится к одному бою и занимает обычную каюту ровно один раз. Точка интеграции — `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs:14`.

## Tests

- AC-01 — `OpeningVictoryWithOnlyDestroyedEnemiesHasOneCapturablePilot`.
- AC-03 — `NormalBattleNeverAddsOpeningEscapePod` и `ContinuePreservesPilotIdentity`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0003Tk0002Tests.OpeningVictoryWithOnlyDestroyedEnemiesHasOneCapturablePilot|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0003Tk0002Tests.NormalBattleNeverAddsOpeningEscapePod|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0003Tk0002Tests.ContinuePreservesPilotIdentity' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs
dotnet format whitespace tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0003Tk0002Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
