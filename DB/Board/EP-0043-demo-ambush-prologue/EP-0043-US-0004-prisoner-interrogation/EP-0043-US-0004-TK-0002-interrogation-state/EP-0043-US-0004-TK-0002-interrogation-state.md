---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0004-prisoner-interrogation
ticket: EP-0043-US-0004-TK-0002-interrogation-state
title: Привязка допроса и открытие цели
stage: approved
layer: engine
depends_on: [EP-0043-US-0004-TK-0001-interrogation-data, EP-0043-US-0003-TK-0003-capture-exit-gate, EP-0043-US-0002-TK-0004-opening-checkpoint]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Привязка допроса и открытие цели

## Why

D-03 и D-05. Захваченный пират раскрывает происхождение маяка и судьбу пилотов. [История](../EP-0043-US-0004-prisoner-interrogation.md).

## Decisions

D-03 и D-05. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02 и A-05. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs:58` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:68` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs:13` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0004Tk0002Tests.cs` | Новый файл в DeepSpaceSaga.Engine.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

На успешном acknowledge сохранить этап допроса и ID из custody в той же кампанийной операции, открывать следующую сцену только за снятым combat/results gate. Принятый reveal-outpost сохраняет durable отметку завершённого допроса и переводит пролог в FuelBriefingPending. Рассчитать и раскрыть координаты и запустить инженерный доклад должна история US-0005 в том же сценарном переходе. Повторные receipts и старые сохранения без этапа не создают допрос. Прерванный workflow восстанавливается из persisted этапа, существующая запись диалога возвращает тот же узел. Запись перехода опирается на существующий continuation checkpoint для AcknowledgeBattleResultsCommand.

## Out of scope

Общие механики допроса и взаимодействия с пленными. Любые изменения вне allowlist.

## Invariants

Отметка аванпоста появляется только после раскрытия игроку. Точка интеграции — `src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs:58`.

## Tests

- AC-01 — `InterrogationOnlyAfterSuccessfulResultsAcknowledgement`.
- AC-02 — `InterrogationUsesCapturedIdentityAfterLoad`.
- AC-03 — `FinalLineArmsInvestigationOnce`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0004Tk0002Tests.InterrogationOnlyAfterSuccessfulResultsAcknowledgement|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0004Tk0002Tests.InterrogationUsesCapturedIdentityAfterLoad|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0004Tk0002Tests.FinalLineArmsInvestigationOnce' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.BattleResults.cs src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs
dotnet format whitespace tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0004Tk0002Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
