---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0003-mandatory-capture
ticket: EP-0043-US-0003-TK-0005-capture-results-ui
title: Подсказка в результатах боя
stage: approved
layer: client
depends_on: [EP-0043-US-0003-TK-0003-capture-exit-gate, EP-0043-US-0003-TK-0004-capture-capacity]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Подсказка в результатах боя

## Why

D-03 и D-04. Допрос требует хотя бы одного реально взятого на борт участника засады. [История](../EP-0043-US-0003-mandatory-capture.md).

## Decisions

D-03 и D-04. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-02. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs:79` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.Sources.cs` | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.Sources.cs:75` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Client/Localization.cs` | `src/DeepSpaceSaga.Client/Localization.cs:1` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0003Tk0005Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Отметить требование рядом с выходом, показать источник капсулы с имеющейся строкой «Взять в плен», после нового snapshot снять блокировку и вернуть прежнее подтверждение для остальной добычи. Отобразить reason от движка на RU/EN. Сохранить исходную расстановку тактических сил и использовать обычный UI capture.

## Out of scope

Переработка общей добычи, вместимости и сценариев поражения. Любые изменения вне allowlist.

## Invariants

Арест относится к одному бою и занимает обычную каюту ровно один раз. Точка интеграции — `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs:79`.

## Tests

- AC-01 — `ResultsRequireCaptureBeforeExit`.
- AC-02 — `EscapePodUsesExistingArrestAction`.
- AC-03 — `EscapePodUsesExistingArrestAction`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0003Tk0005Tests.ResultsRequireCaptureBeforeExit|FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0003Tk0005Tests.EscapePodUsesExistingArrestAction' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.Sources.cs src/DeepSpaceSaga.Client/Localization.cs
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0003Tk0005Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
