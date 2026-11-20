---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0004-parallel-bulk
ticket: EP-0042-US-0004-TK-0003-bulk-action
title: Массовый запуск и раскрытие карточек
stage: draft
layer: client
depends_on: [EP-0042-US-0003-TK-0003-row-progress, EP-0042-US-0004-TK-0002-cargo-completion]
files_touched: 5
serves: [AC-01, AC-03, AC-04]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Массовый запуск и раскрытие карточек

## Why

Одна команда охватывает все источники боя, включая свёрнутые; раскрываются все карточки с доступным материальным лутом. Все доступные операции получают общий старт, включая строки, которые сейчас не поместятся. Пилоты не арестовываются. Расходники принимаются частично до вместимости; остаток остаётся доступен. Успешный модуль/лом без места остаётся доступным со статусом «Не поместилось» и отдельным действием «Забрать» без повторного демонтажа. Неудачная попытка остаётся видимой и не перезапускается. Повторная команда не дополнительно запускает активные/терминальные строки, не тратит второй RNG draw и не повторяет grant или арест.

## Decisions

Точная запись исходных решений: [D-0042-05, D-0042-06, D-0042-07, D-0042-10, D-0042-12, D-0042-14](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | футер модали | Добавить «Забрать всё». После подтверждения прихода bulk start раскрыть все соответствующие материальные источники и продолжить отдельные полосы. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleResultsSourceList.cs` | будущий список открытых карточек | Поддержать раскрытие набора источников без изменения ordinal; скролл остаётся управляемым. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs` | будущие строки/действия | Добавить видимое «Не поместилось» и «Забрать» с режимом cargo-full; итог и частичное количество сохраняются в строках. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootPresentation.cs` | предложенная проекция операций | Единый запрос batch и закладка identity при неизвестном результате, без посылки N start-команд. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0004Tk0003Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

No public API change; у bulk свои receipt и список authoritative running rows.

## Implementation steps

1. Добавить «Забрать всё». После подтверждения прихода bulk start раскрыть все соответствующие материальные источники и продолжить отдельные полосы.
2. Поддержать раскрытие набора источников без изменения ordinal; скролл остаётся управляемым.
3. Добавить видимое «Не поместилось» и «Забрать» с режимом cargo-full; итог и частичное количество сохраняются в строках.
4. Единый запрос batch и закладка identity при неизвестном результате, без посылки N start-команд.
5. Свободный объём не является критерием раскрытия/старта. Failed/Collected не стартуют снова. Только пилот без материального лута не является целью Get all.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Свободный объём не является критерием раскрытия/старта. Failed/Collected не стартуют снова. Только пилот без материального лута не является целью Get all.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BulkLoot_AllCardsExpand_ArrestRemainsManual`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0004-tk0003-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BulkLoot_AllCardsExpand_ArrestRemainsManual' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs src/DeepSpaceSaga.Client/UI/Demo/BattleResultsSourceList.cs src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs src/DeepSpaceSaga.Client/UI/Demo/BattleLootPresentation.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0003-TK-0003-row-progress](../../EP-0042-US-0003-timed-loot/EP-0042-US-0003-TK-0003-row-progress/EP-0042-US-0003-TK-0003-row-progress.md), [EP-0042-US-0004-TK-0002-cargo-completion](../../EP-0042-US-0004-parallel-bulk/EP-0042-US-0004-TK-0002-cargo-completion/EP-0042-US-0004-TK-0002-cargo-completion.md). Точный allowlist и owning test project указаны выше.
