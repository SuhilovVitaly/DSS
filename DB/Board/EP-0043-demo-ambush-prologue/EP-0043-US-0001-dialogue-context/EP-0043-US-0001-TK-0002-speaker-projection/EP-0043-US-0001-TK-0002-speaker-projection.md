---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0001-dialogue-context
ticket: EP-0043-US-0001-TK-0002-speaker-projection
title: Смена реплики и идентификация говорящего
stage: approved
layer: engine
depends_on: [EP-0043-US-0001-TK-0001-dialogue-contract]
files_touched: 2
serves: [AC-01, AC-02]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Смена реплики и идентификация говорящего

## Why

D-01. Вступление, допрос и доклад инженера используют одно представление реплик. [История](../EP-0043-US-0001-dialogue-context.md).

## Decisions

D-01. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-03. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:68` | Основное изменение по шагам тикета. |
| `tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0001Tk0002Tests.cs` | Новый файл в DeepSpaceSaga.Engine.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

До смены узла материализовать реплику из подтверждённого состояния и сохранить её как PreviousUtterance в active state. Разрешать капитана, назначенного инженера, оператора и пленного через реальный контекст сессии и ID персонажа. Использовать тот же resolver для текущего и предыдущего голоса и сохранить существующее разрешение ролей станций. Для старого сейва с отсутствующим полем опустить предыдущую реплику до следующего принятого перехода.

## Out of scope

Озвучка и журнал полного диалога. Любые изменения вне allowlist.

## Invariants

Стабильный порядок реплик и изоляция сцен. Точка интеграции — `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:68`.

## Tests

- AC-01 — `PreviousUtteranceBelongsToAcceptedNode`.
- AC-02 — `SaveLoadKeepsBothSpeakers`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0001Tk0002Tests.PreviousUtteranceBelongsToAcceptedNode|FullyQualifiedName=DeepSpaceSaga.Engine.Tests.DemoEp0043Us0001Tk0002Tests.SaveLoadKeepsBothSpeakers' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs
dotnet format whitespace tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Engine.Tests/DemoEp0043Us0001Tk0002Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
