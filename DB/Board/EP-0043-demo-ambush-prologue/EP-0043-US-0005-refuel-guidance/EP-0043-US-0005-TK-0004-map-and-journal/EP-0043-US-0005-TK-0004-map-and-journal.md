---
epic: EP-0043-demo-ambush-prologue
story: EP-0043-US-0005-refuel-guidance
ticket: EP-0043-US-0005-TK-0004-map-and-journal
title: Отметки и ручной маршрут
stage: approved
layer: client
depends_on: [EP-0043-US-0005-TK-0003-refuel-eligibility, EP-0043-US-0004-TK-0003-interrogation-text]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:58:51Z
revision: 1
---

# Отметки и ручной маршрут

## Why

D-05 и D-06. Экипаж знает цель, но сначала игрок должен выбрать достижимую станцию. [История](../EP-0043-US-0005-refuel-guidance.md).

## Decisions

D-05 и D-06. [Продуктовые решения и исходные ответы](../../Documentation.md#решения-пользователя).

## Assumptions

A-04 и A-06. [Предположения эпика](../../Documentation.md#технические-предположения).

## Code context

| Файл | Точка интеграции | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1300` | Основное изменение по шагам тикета. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` | `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs:279` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs:110` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `src/DeepSpaceSaga.Client/Localization.cs` | `src/DeepSpaceSaga.Client/Localization.cs:1` | Только необходимая интеграция или проверка данных в рамках шагов. |
| `tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0005Tk0004Tests.cs` | Новый файл в DeepSpaceSaga.Client.Tests. | Только именованные тесты тикета. |

## Public API after the change

Переиспользовать текущие публичные маршруты и команды. Новые поля проекций определяют тикеты слоя Contracts.

## Implementation steps

Показать аванпост и рекомендованную станцию по авторитетной проекции, на стандартных координатах и с существующим выбором станции. Добавить в журнал постоянную цель и ближайший шаг, локализовать доклад с именем станции и метки на RU/EN. Не отправлять команду выбора курса автоматически. Конечная проверка из демо-профиля включает ручной курс, валидную цену и возможность штатной дозаправки.

## Out of scope

Ввод новой экономики топлива и спасение пилотов. Любые изменения вне allowlist.

## Invariants

Отправление происходит только после команды игрока. Точка интеграции — `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1300`.

## Tests

- AC-01 — `MapShowsBothTargetsWithoutSubmittingRoute`.
- AC-02 — `JournalShowsRescueAndRefuelMilestone`.
- AC-03 — `PlayerCanSelectStationAndPlotRoute`.

Будущий запуск из корня DSS через ограничитель дерева процессов 300000 ms. Создать отдельный абсолютный каталог запуска и направить туда stdout/stderr.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter 'FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0005Tk0004Tests.MapShowsBothTargetsWithoutSubmittingRoute|FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0005Tk0004Tests.JournalShowsRescueAndRefuelMilestone|FullyQualifiedName=DeepSpaceSaga.Client.Tests.DemoEp0043Us0005Tk0004Tests.PlayerCanSelectStationAndPlotRoute' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/test-results' --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path 'D:/DeepSpaceSaga/artifacts/validation/EP-0043-demo-ambush-prologue/<run>/build'
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs src/DeepSpaceSaga.Client/Localization.cs
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/DemoEp0043Us0005Tk0004Tests.cs
```

## Definition of Done

- Шаги выполнены в пределах allowlist и критерии AC-01, AC-02, AC-03 покрыты указанными проверками.
- Именованные проверки, сборка и применимое форматирование проходят. Исходное падение записано отдельно, если применимо.
- Поведение на штатных входах, повторе операции и границах истории проверено в применимой части.

## Self-containment check

Code context задаёт разрешённые точки интеграции. Входные решения, предшествующие работы и наблюдаемый результат указаны в истории и depends_on. Статус утверждения относится к декомпозиции по workflow.
