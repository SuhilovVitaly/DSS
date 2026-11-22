---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0004-parallel-bulk
ticket: EP-0042-US-0004-TK-0002-cargo-completion
title: Атомарное начисление и частичные пачки
stage: implemented
layer: engine
depends_on: [EP-0042-US-0004-TK-0001-bulk-order]
files_touched: 4
serves: [AC-02, AC-03, AC-04]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Атомарное начисление и частичные пачки

## Why

Место не резервируется. Очерёдность выдачи — срок окончания, при одном сроке порядок строк сверху вниз. Результаты меняют фактический трюм атомарно, без переполнения и без зависимости от viewport или скорости отрисовки. Расходники принимаются частично до вместимости; остаток остаётся доступен. Успешный модуль/лом без места остаётся доступным со статусом «Не поместилось» и отдельным действием «Забрать» без повторного демонтажа. Неудачная попытка остаётся видимой и не перезапускается. Повторная команда не дополнительно запускает активные/терминальные строки, не тратит второй RNG draw и не повторяет grant или арест.

## Decisions

Точная запись исходных решений: [D-0042-05, D-0042-06, D-0042-07, D-0042-10, D-0042-12, D-0042-14](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.LootOperations.cs` | будущий due runner | Последовательная обработка due в lock, подготовить/применить корректные ledger/cargo commits с общими состояниями. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | TryPrepareLootCargo, CollectLootConsumablesCommand, AttemptScrapCommand | Оценить максимум из целых единиц исходя из действующего поконтейнерного размещения/резервов; грамотно обновлять Remaining и HasUnclaimed. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.ModuleSalvage.cs` | PrepareModuleSalvage | Разделить single roll и доставку при успехе, при нехватке места один раз записать ExtractedWaiting. Claim не запускает RNG. |
| `tests/DeepSpaceSaga.Engine.Tests/PostbattleResults/EP0042Us0004Tk0002Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемое использование ClaimExtractedLootCommand, устойчивая причина CargoFull для сохранённого трофея.

## Implementation steps

1. Последовательная обработка due в lock, подготовить/применить корректные ledger/cargo commits с общими состояниями.
2. Оценить максимум из целых единиц исходя из действующего поконтейнерного размещения/резервов; грамотно обновлять Remaining и HasUnclaimed.
3. Разделить single roll и доставку при успехе, при нехватке места один раз записать ExtractedWaiting. Claim не запускает RNG.
4. При одновременно доступном месте пересчитывать capacity после каждого фактического начисления. Пример: 10 торпед, место под 4 → взять 4 и сохранить 6. Модуль и партия лома требуют полного установленного объёма. Арест не расходует трюм.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

При одновременно доступном месте пересчитывать capacity после каждого фактического начисления. Пример: 10 торпед, место под 4 → взять 4 и сохранить 6. Модуль и партия лома требуют полного установленного объёма. Арест не расходует трюм.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BulkLoot_PartialConsumablesWaitingClaimNoReroll`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0004-tk0002-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~BulkLoot_PartialConsumablesWaitingClaimNoReroll' --results-directory $run
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.LootOperations.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs src/DeepSpaceSaga.Engine/SimulationEngine.ModuleSalvage.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0004-TK-0001-bulk-order](../../EP-0042-US-0004-parallel-bulk/EP-0042-US-0004-TK-0001-bulk-order/EP-0042-US-0004-TK-0001-bulk-order.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

После успешного броска без места сохраняется ExtractedWaiting; допустимость состояния проверяет BattleLootData. Contracts публикует готовность отдельного подбора. Dispatch принимает ClaimExtractedLootCommand, а архивный отчёт отображает добытое без места и переводит его в оставленное после закрытия. Legacy-команды сохраняют исходную проверку вместимости до броска. Эти файлы расширяют исходный allowlist.

## Исполнение

- Расходники размещаются по реальным секциям трюма до вместимого целого количества. Один успешный бросок модуля/лома без места сохраняет ExtractedWaiting; подбор проверяет cargo без RNG.
- Сборка Engine Tests, scoped whitespace format и `git diff --check` прошли. Проверки полного и частичного трюма назначены на границу истории.
