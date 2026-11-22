---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0003-mandatory-capture
ticket: EP-0043-US-0003-TK-0003-capture-exit-gate
title: Выход только после захвата
stage: approved
layer: engine
depends_on: [EP-0043-US-0003-TK-0002-opening-loot]
files_touched: 5
serves: [AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Выход только после захвата

## Why

D-03 и D-04. Допрос требует хотя бы одного реально взятого на борт участника засады. [История](../EP-0043-US-0003-mandatory-capture.md).

## Decisions

D-03 и D-04. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs:26` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:126` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs:58` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs:173` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0003Tk0003Tests.cs` | Новый файл в DeepSpaceSaga.Engine.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Проверять custody по текущему BattleId и кораблю до discard/закрытия. Спроецировать обязательность из авторитетного ledger после подтверждённого захвата. Не подтверждать результат и не разблокировать карту при отказе. Зафиксировать ID собеседника как первого подтверждённо захваченного из этого боя, при нескольких арестах сохранять первый. На поражении использовать прежний итог, без захвата или допроса.

## Out of scope

Переработка общей добычи, вместимости и сценариев поражения. Любые изменения вне allowlist.

## Invariants

Арест относится к одному бою и занимает обычную каюту ровно один раз. Точка интеграции — `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs:26`.

## Tests

- AC-02 — `ConfirmDiscardCannotBypassCapture`.
- AC-03 — `CaptureUnblocksFinishAndKeepsFirstPrisoner` и `DefeatDoesNotEnforceCapture`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0003Tk0003Tests.ConfirmDiscardCannotBypassCapture|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0003Tk0003Tests.CaptureUnblocksFinishAndKeepsFirstPrisoner|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0003Tk0003Tests.DefeatDoesNotEnforceCapture' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs
dotnet format whitespace tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0003Tk0003Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
