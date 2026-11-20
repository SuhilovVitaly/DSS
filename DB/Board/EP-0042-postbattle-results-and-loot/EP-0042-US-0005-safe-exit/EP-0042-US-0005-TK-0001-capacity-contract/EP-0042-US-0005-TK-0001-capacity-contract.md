---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0005-safe-exit
ticket: EP-0042-US-0005-TK-0001-capacity-contract
title: Проекция статуса трюма и кают
stage: draft
layer: contracts
depends_on: [EP-0042-US-0003-TK-0001-operation-contract]
files_touched: 2
serves: [AC-01]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Проекция статуса трюма и кают

## Why

Футер указывает живому носителю свободный трюм и число свободных кают для пленных рядом, включая явный ноль. Каюты берутся из общей ёмкости с действующими резервациями, без клиентского пересчёта; успешный арест обновляет значение.

## Decisions

Точная запись исходных решений: [D-0042-02, D-0042-09, D-0042-11, D-0042-15](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs` | LootSessionSnapshot.FreeCargoKg | Добавить nullable authoritative FreePrisonerCabins либо общий read-only ShipStatus для окна, сохранив старые поля. Явный 0 отличается от отсутствующего/устаревшего значения. |
| `tests/DeepSpaceSaga.Contracts.Tests/PostbattleResults/EP0042Us0005Tk0001Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

Проектируемый LootSessionSnapshot.FreePrisonerCabins: int? (плюс опциональная проекция текущего корабля для режимов без лута, если применимо).

## Implementation steps

1. Добавить nullable authoritative FreePrisonerCabins либо общий read-only ShipStatus для окна, сохранив старые поля. Явный 0 отличается от отсутствующего/устаревшего значения.
2. Сериализация старых снимков работает. Не изобретать отдельный тип кают или целевую capacity для пленных.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Сериализация старых снимков работает. Не изобретать отдельный тип кают или целевую capacity для пленных.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsCapacity_ZeroDistinctFromUnknown`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0005-tk0001-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsCapacity_ZeroDistinctFromUnknown' --results-directory $run
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/BattleLootContracts.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0003-TK-0001-operation-contract](../../EP-0042-US-0003-timed-loot/EP-0042-US-0003-TK-0001-operation-contract/EP-0042-US-0003-TK-0001-operation-contract.md). Точный allowlist и owning test project указаны выше.
