---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0003-timed-loot
ticket: EP-0042-US-0003-TK-0003-row-progress
title: Полосы прогресса и ручной запуск
stage: draft
layer: client
depends_on: [EP-0042-US-0002-TK-0003-loot-rows, EP-0042-US-0003-TK-0002-single-engine]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Полосы прогресса и ручной запуск

## Why

Ручной старт по одному источнику и строке: расходники 3 секунды, модули 5, лом 10. Реальное монотонное время движется при паузе игрового мира; часы игры стоят. Каждый модуль/лом использует свой существующий шанс, один бросок и видимый итог. Блокировать повторный старт Running/Failed/Collected; при техническом повторе сохранять ту же identity. Для Running справа от кнопки вертикальная шкала от низа к верху. Для терминального результата строка остаётся, а прогресс прекращается. Арест не входит в 3/5/10 и не запускается автоматически.

## Decisions

Точная запись исходных решений: [D-0042-05, D-0042-08, D-0042-13](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootPresentation.cs` | SubmitAsync, Pending | Один запрос подтверждения на текущую ревизию, несколько Running строк; действующий retry с тем же ID. Таймер обновления планирует запрос окончания вне отрисовки, даже при скрытой строке. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs` | будущий US-0002/TK-0003 | Полоса справа от кнопки, растёт снизу вверх; terminal badges остаются видимы. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs` | будущий US-0001/TK-0003 | Подключить lifecycle обновления, клики по кнопкам и disabled hit zones. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0003Tk0003Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

No public API change; progress ограничивается [0,1] и не выдаёт локальное предсказание за исход.

## Implementation steps

1. Один запрос подтверждения на текущую ревизию, несколько Running строк; действующий retry с тем же ID. Таймер обновления планирует запрос окончания вне отрисовки, даже при скрытой строке.
2. Полоса справа от кнопки, растёт снизу вверх; terminal badges остаются видимы.
3. Подключить lifecycle обновления, клики по кнопкам и disabled hit zones.
4. Сворачивание и прокрутка не пересоздают identity, не останавливают срок. На клиенте нет локального влияния на трюм/шанс. Арест не запускается кнопкой демонтажа.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Сворачивание и прокрутка не пересоздают identity, не останавливают срок. На клиенте нет локального влияния на трюм/шанс. Арест не запускается кнопкой демонтажа.

## Tests

Будущая проверка, не запускалась. Именованный тест: `LootOperationRow_ProgressBottomUp_ManualAndCollapse`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0003-tk0003-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~LootOperationRow_ProgressBottomUp_ManualAndCollapse' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Demo/BattleLootPresentation.cs src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleResultsScreen.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0002-TK-0003-loot-rows](../../EP-0042-US-0002-loot-accordion/EP-0042-US-0002-TK-0003-loot-rows/EP-0042-US-0002-TK-0003-loot-rows.md), [EP-0042-US-0003-TK-0002-single-engine](../../EP-0042-US-0003-timed-loot/EP-0042-US-0003-TK-0002-single-engine/EP-0042-US-0003-TK-0002-single-engine.md). Точный allowlist и owning test project указаны выше.
