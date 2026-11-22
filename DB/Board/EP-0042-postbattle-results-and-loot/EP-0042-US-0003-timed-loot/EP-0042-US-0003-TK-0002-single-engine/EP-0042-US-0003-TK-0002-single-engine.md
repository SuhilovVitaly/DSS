---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0003-timed-loot
ticket: EP-0042-US-0003-TK-0002-single-engine
title: Отложенная одиночная операция
stage: implemented
layer: engine
depends_on: [EP-0042-US-0003-TK-0001-operation-contract]
files_touched: 5
serves: [AC-01, AC-02]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Отложенная одиночная операция

## Why

Ручной старт по одному источнику и строке: расходники 3 секунды, модули 5, лом 10. Реальное монотонное время движется при паузе игрового мира; часы игры стоят. Каждый модуль/лом использует свой существующий шанс, один бросок и видимый итог. Блокировать повторный старт Running/Failed/Collected; при техническом повторе сохранять ту же identity.

## Decisions

Точная запись исходных решений: [D-0042-05, D-0042-08, D-0042-13](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.LootOperations.cs` | новый | Владение очередью в lock, монотонные сроки 3000/5000/10000, стабильные operation ids, single admission и due runner. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | ExecuteDemoCampaign, PrepareDemoCampaignCommand | Добавить типы queue-команд, ревизию/receipts и gate для закрытия/активности. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | PrepareBattleLootCommand, BuildBattleLootProjection | Подключить результат и состояние операций к существующему loot ledger. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.ModuleSalvage.cs` | PrepareModuleSalvage | Повторно использовать один roll и существующие chance formulas; результат по заполнению добавит US-0004. |
| `tests/DeepSpaceSaga.Engine.Tests/PostbattleResults/EP0042Us0003Tk0002Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Engine исполняет проектируемые команды TK-0001 через ExecuteDemoCampaign и снимает authoritative due entries; время принадлежит Engine.

## Implementation steps

1. Владение очередью в lock, монотонные сроки 3000/5000/10000, стабильные operation ids, single admission и due runner.
2. Добавить типы queue-команд, ревизию/receipts и gate для закрытия/активности.
3. Подключить результат и состояние операций к существующему loot ledger.
4. Повторно использовать один roll и существующие chance formulas; результат по заполнению добавит US-0004.
5. Не ждать в мировом lock и не продвигать симуляцию. Клиентский callback только будит проверку Engine, который сам проверяет монотонный срок. Не использовать task per row, не менять order/Pilot identity/RNG формулы.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Не ждать в мировом lock и не продвигать симуляцию. Клиентский callback только будит проверку Engine, который сам проверяет монотонный срок. Не использовать task per row, не менять order/Pilot identity/RNG формулы.

## Tests

Будущая проверка, не запускалась. Именованный тест: `LootOperations_SingleDurationsAndOneRoll`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0003-tk0002-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~LootOperations_SingleDurationsAndOneRoll' --results-directory $run
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.LootOperations.cs src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs src/DeepSpaceSaga.Engine/SimulationEngine.ModuleSalvage.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0003-TK-0001-operation-contract](../../EP-0042-US-0003-timed-loot/EP-0042-US-0003-TK-0001-operation-contract/EP-0042-US-0003-TK-0001-operation-contract.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

LocalClient не пишет checkpoint при одном принятии таймера или промежуточном завершении нескольких операций. Хранилище получает снимок после последней операции. Стадия результатов блокирует выход и прямые команды, пока запущен таймер. При очистке сценария runtime очередь очищается. Эти границы добавлены к исходному allowlist. Продвижение обрабатывает первую истёкшую строку в авторитетном порядке при каждом вызове, без ожидания в lock и отдельной задачи на строку.

## Исполнение

- Engine проверяет сроки 3/5/10 с по монотонным часам. Повторный старт той же строки и выход до окончания отклоняются. Изменения cargo и RNG применяются в прежнем мировом lock.
- Сборка Engine Tests, scoped whitespace format и `git diff --check` прошли. Проверка очереди и приёма команд запланирована на завершение US-0003.
