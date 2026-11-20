---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0004-parallel-bulk
ticket: EP-0042-US-0004-TK-0001-bulk-order
title: Единый состав пачки и порядок строк
stage: implemented
layer: engine
depends_on: [EP-0042-US-0003-TK-0002-single-engine]
files_touched: 3
serves: [AC-01, AC-02, AC-04]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Единый состав пачки и порядок строк

## Why

Одна команда охватывает все источники боя, включая свёрнутые; раскрываются все карточки с доступным материальным лутом. Все доступные операции получают общий старт, включая строки, которые сейчас не поместятся. Пилоты не арестовываются. Место не резервируется. Очерёдность выдачи — срок окончания, при одном сроке порядок строк сверху вниз. Результаты меняют фактический трюм атомарно, без переполнения и без зависимости от viewport или скорости отрисовки. Повторная команда не дополнительно запускает активные/терминальные строки, не тратит второй RNG draw и не повторяет grant или арест.

## Decisions

Точная запись исходных решений: [D-0042-05, D-0042-06, D-0042-07, D-0042-10, D-0042-12, D-0042-14](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.LootOperations.cs` | будущий US-0003/TK-0002 | Добавить admission режима all и стабильное присвоение ordinal по frozen порядку карточек и строк. Обрабатывать все due в порядке (dueAt, ordinal). |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs` | BuildBattleLootProjection, PrepareBattleLootCommand | Один стабильный порядок источников и строк для проекции и bulk admission; отсечь destroyed, finished, running, пилотов. |
| `tests/DeepSpaceSaga.Engine.Tests/PostbattleResults/EP0042Us0004Tk0001Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Использовать проектируемый selector All из US-0003/TK-0001. Видимый порядок совпадает с immutable row ordinals в Engine.

## Implementation steps

1. Добавить admission режима all и стабильное присвоение ordinal по frozen порядку карточек и строк. Обрабатывать все due в порядке (dueAt, ordinal).
2. Один стабильный порядок источников и строк для проекции и bulk admission; отсечь destroyed, finished, running, пилотов.
3. Один timestamp для всех операций пачки. Не применять предварительный cargo фильтр. Никаких repaint/scroll зависимостей. Допущение при досрочном due reconcile: отсортировать все уже истёкшие по исходным дедлайнам, затем по видимому порядку.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Один timestamp для всех операций пачки. Не применять предварительный cargo фильтр. Никаких repaint/scroll зависимостей. Допущение при досрочном due reconcile: отсортировать все уже истёкшие по исходным дедлайнам, затем по видимому порядку.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BulkLoot_StableTopDownTiesAndNoCargoPrefilter`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0004-tk0001-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~BulkLoot_StableTopDownTiesAndNoCargoPrefilter' --results-directory $run
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.LootOperations.cs src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0003-TK-0002-single-engine](../../EP-0042-US-0003-timed-loot/EP-0042-US-0003-TK-0002-single-engine/EP-0042-US-0003-TK-0002-single-engine.md). Точный allowlist и owning test project указаны выше.

## Исполнение

- Одна команда планирует за один старт все доступные материальные строки. Трюм на старте не резервируется. Срок и порядок ledger определяют следующую выдачу. Пилоты не входят в пачку.
- Сборка Engine Tests, scoped whitespace format и `git diff --check` прошли.
