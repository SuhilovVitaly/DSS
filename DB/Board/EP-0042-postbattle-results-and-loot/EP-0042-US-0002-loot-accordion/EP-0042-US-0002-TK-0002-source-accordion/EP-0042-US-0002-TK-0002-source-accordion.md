---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0002-loot-accordion
ticket: EP-0042-US-0002-TK-0002-source-accordion
title: Независимое раскрытие и прокрутка
stage: draft
layer: client
depends_on: [EP-0042-US-0002-TK-0001-station-frame]
files_touched: 4
serves: [AC-01, AC-02]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Независимое раскрытие и прокрутка

## Why

Границы модали совпадают с окном станции при одинаковом viewport и масштабе. Слева карточки сбитых или отключённых вражеских дронов и уничтоженных носителей, с состоянием и именем. Клик раскрывает/сворачивает только выбранную карточку. Строки сразу под карточкой раздвигают список, занимают доступную ширину модали; фиксированный футер не входит в область прокрутки. Нет кликов сквозь обрезанную часть списка.

## Decisions

Точная запись исходных решений: [D-0042-03, D-0042-04, D-0042-10, D-0042-13](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | будущий контейнер US-0001 | Подключить clipping, список карточек и hit-test строки/футера. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleResultsSourceList.cs` | новый | Проецировать вражеские источники из frozen outcome по ObjectId; управлять набором раскрытых карточек, scrollbar, paint/hit bounds. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | проекция боевых карточек | Переиспользовать оформление/структуру карточек через ограниченную общую проекцию без изменения поведения боевого HUD. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0002Tk0002Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Внутренний state: stable source index и множество раскрытых ObjectId. Развёрнутые строки не меняют их порядок.

## Implementation steps

1. Подключить clipping, список карточек и hit-test строки/футера.
2. Проецировать вражеские источники из frozen outcome по ObjectId; управлять набором раскрытых карточек, scrollbar, paint/hit bounds.
3. Переиспользовать оформление/структуру карточек через ограниченную общую проекцию без изменения поведения боевого HUD.
4. Включить уничтоженные, отключённые и wreck carriers. Название/статус из frozen snapshot, без чтения скрытой позиции живых. Сворачивание не меняет операции. При resize домен scroll зажимается к новым границам.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Включить уничтоженные, отключённые и wreck carriers. Название/статус из frozen snapshot, без чтения скрытой позиции живых. Сворачивание не меняет операции. При resize домен scroll зажимается к новым границам.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsAccordion_IndependentExpansionClippedInputs`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0002-tk0002-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsAccordion_IndependentExpansionClippedInputs' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs src/DeepSpaceSaga.Client/UI/Demo/BattleResultsSourceList.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0002-TK-0001-station-frame](../../EP-0042-US-0002-loot-accordion/EP-0042-US-0002-TK-0001-station-frame/EP-0042-US-0002-TK-0001-station-frame.md). Точный allowlist и owning test project указаны выше.
