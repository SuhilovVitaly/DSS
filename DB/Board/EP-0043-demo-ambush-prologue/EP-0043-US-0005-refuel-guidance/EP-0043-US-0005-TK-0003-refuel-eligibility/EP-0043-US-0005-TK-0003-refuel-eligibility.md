---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0005-refuel-guidance
ticket: EP-0043-US-0005-TK-0003-refuel-eligibility
title: Выбор станции и доклад инженера
stage: approved
layer: engine
depends_on: [EP-0043-US-0005-TK-0001-story-target-contract, EP-0043-US-0005-TK-0002-refuel-dialogue-data]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Выбор станции и доклад инженера

## Why

D-05 и D-06. Экипаж знает цель, но сначала игрок должен выбрать достижимую станцию. [История](../EP-0043-US-0005-refuel-guidance.md).

## Decisions

D-05 и D-06. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-04 и A-06. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:111` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.VoyageFuel.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.VoyageFuel.cs:20` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:68` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs:13` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0005Tk0003Tests.cs` | Новый файл в DeepSpaceSaga.Engine.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

При переходе в FuelBriefingPending использовать авторитетную позицию, пригодные баки и штатную модель топлива. Для сюжетного прогноза прямого плеча из открытого космоса использовать чистую оценку через CaptureFuelDistance и CalculateVoyageFuel при нейтральном коэффициенте 1000. Ручная навигация и стыковка используют существующие команды. Прогноз для доклада остаётся оценкой, а сценарный интеграционный тест отдельно подтверждает достижение и заправку на выбранной станции. Ближайшая — минимальная дистанция из достижимых доступных для стыковки известных станций с топливом и валидной стоимостью достаточной заправки; при равенстве ordinal StationId. Аванпост размещать на планируемом достижимом после заправки плече и вне текущего запаса, без произвольной блокировки команд. Записать цель/станцию вместе и открыть инженерный доклад с его именем и параметром станции. Независимая проверка штатного полёта обязательна, иначе не утверждать, что сценарий сбалансирован. Значения расстояний переводить из координат мира в километры существующим WorldUnitsPerKm.

## Out of scope

Ввод новой экономики топлива и спасение пилотов. Любые изменения вне allowlist.

## Invariants

Отправление происходит только после команды игрока. Точка интеграции — `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:111`.

## Tests

- AC-01 — `FuelToStationFitsButFuelToOutpostDoesNot`.
- AC-02 — `NearestEligibleStationUsesStableTieBreak`.
- AC-03 — `ContinueRestoresTargetAndEngineerState`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0005Tk0003Tests.FuelToStationFitsButFuelToOutpostDoesNot|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0005Tk0003Tests.NearestEligibleStationUsesStableTieBreak|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0005Tk0003Tests.ContinueRestoresTargetAndEngineerState' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs src/DeepSpaceSaga.Engine/SimulationEngine.VoyageFuel.cs src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs
dotnet format whitespace tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0005Tk0003Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
