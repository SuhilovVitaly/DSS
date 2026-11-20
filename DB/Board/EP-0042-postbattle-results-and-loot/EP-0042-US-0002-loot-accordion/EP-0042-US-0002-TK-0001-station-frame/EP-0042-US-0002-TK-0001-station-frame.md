---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0002-loot-accordion
ticket: EP-0042-US-0002-TK-0001-station-frame
title: Геометрия окна станции
stage: draft
layer: client
depends_on: [EP-0042-US-0001-TK-0003-summary-shell]
files_touched: 3
serves: [AC-01, AC-02]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Геометрия окна станции

## Why

Границы модали совпадают с окном станции при одинаковом viewport и масштабе. Слева карточки сбитых или отключённых вражеских дронов и уничтоженных носителей, с состоянием и именем. Клик раскрывает/сворачивает только выбранную карточку. Строки сразу под карточкой раздвигают список, занимают доступную ширину модали; фиксированный футер не входит в область прокрутки. Нет кликов сквозь обрезанную часть списка.

## Decisions

Точная запись исходных решений: [D-0042-03, D-0042-04, D-0042-10, D-0042-13](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | будущий US-0001/TK-0003 | Использовать те же PanelLeft, PanelTop, PanelWidth и PanelHeight, одинаковые преобразования viewport и зоны hit-test. |
| `src/DeepSpaceSaga.Client/UI/Screens/Station/StationLayout.cs` | PanelWidth/PanelHeight; PanelLeft, PanelTop | Разрешено только выделить общий exact rect при необходимости; геометрия окна станции не меняется. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0002Tk0001Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

No public API change; потребитель общего layout станции.

## Implementation steps

1. Использовать те же PanelLeft, PanelTop, PanelWidth и PanelHeight, одинаковые преобразования viewport и зоны hit-test.
2. Разрешено только выделить общий exact rect при необходимости; геометрия окна станции не меняется.
3. Программно сравнить прямоугольники на двух viewport и масштабах, включая малый экран. Скролл остаётся внутри рамки и не двигает футер.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Программно сравнить прямоугольники на двух viewport и масштабах, включая малый экран. Скролл остаётся внутри рамки и не двигает футер.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsBounds_MatchStation`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0002-tk0001-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsBounds_MatchStation' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs src/DeepSpaceSaga.Client/UI/Screens/Station/StationLayout.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0001-TK-0003-summary-shell](../../EP-0042-US-0001-results-modal/EP-0042-US-0001-TK-0003-summary-shell/EP-0042-US-0001-TK-0003-summary-shell.md). Точный allowlist и owning test project указаны выше.

## Исполнение

- Рамка результатов использует размеры, центрирование и стиль окна станции. Список ограничен видимой областью. Футер остаётся за пределами списка и видим на малом viewport.
- Сборка Client Tests, scoped whitespace format и `git diff --check` прошли.
