---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0002-loot-accordion
ticket: EP-0042-US-0002-TK-0003-loot-rows
title: Таблица доступных предметов и пилота
stage: draft
layer: client
depends_on: [EP-0042-US-0002-TK-0002-source-accordion]
files_touched: 5
serves: [AC-03, AC-04]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Таблица доступных предметов и пилота

## Why

Строка модуля/лома показывает иконку, название, оценку цены, шанс, размер и кнопку «Демонтировать». Пилот виден отдельной строкой с именем и «Арестовать»; расходники показывают количество и действие подбора. Предметы, недоступные из-за боевых потерь/начального броска сохранности, скрыты; исход запущенной игроком операции остаётся виден. При непобеде никакие loot/arrest действия не доступны.

## Decisions

Точная запись исходных решений: [D-0042-03, D-0042-04, D-0042-10, D-0042-13](../../Documentation.md#решения-пользователя).

## Assumptions

Применяются [A-01…A-05](../../Documentation.md#технические-допущения-и-границы) в пределах критериев тикета. Названия новых внутренних типов — проектные, факты о текущем коде указаны отдельно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs` | новый | Строки, столбцы, иконки, формат цены/кг/шанса, disabled-состояния и привязка ручного ареста. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleResultsSourceList.cs` | будущий ticket US-0002/TK-0002 | Встроить строки в общий список с устойчивыми идентификаторами. |
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootPresentation.cs` | State, SubmitAsync | Подключить ручные действия к существующим idempotent командам там, где семантика сохраняется; операции таймера подключаются US-0003. |
| `src/DeepSpaceSaga.Client/UI/Demo/ModuleSalvagePresentation.cs` | действующие подписи/значения модулей | Переиспользовать цену, массу и формат шанса; без новых ценовых правил. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0002Tk0003Tests.cs` | новый | Именованная проверка этого тикета. |

## Public API after the change

No public API change; UI использует ChanceBasisPoints, EstimatedValueTokens, RequiredMassKg и современный state из snapshot.

## Implementation steps

1. Строки, столбцы, иконки, формат цены/кг/шанса, disabled-состояния и привязка ручного ареста.
2. Встроить строки в общий список с устойчивыми идентификаторами.
3. Подключить ручные действия к существующим idempotent командам там, где семантика сохраняется; операции таймера подключаются US-0003.
4. Переиспользовать цену, массу и формат шанса; без новых ценовых правил.
5. Цену показывать как существующую оценку, не выдумывать оценку, которой нет. Арест отдельно от загрузки груза, существующая проверка кают и сохранение. Скрывать initial destroyed, сохранять failed/collected после собственной попытки; строки успеха без места подключаются US-0004.

## Out of scope

Файлы за пределами allowlist, несвязанные системы и новые продуктовые решения. Если baseline отличается от указанного, сначала записать конкретный drift в каноническом тикете.

## Invariants

Цену показывать как существующую оценку, не выдумывать оценку, которой нет. Арест отдельно от загрузки груза, существующая проверка кают и сохранение. Скрывать initial destroyed, сохранять failed/collected после собственной попытки; строки успеха без места подключаются US-0004.

## Tests

Будущая проверка, не запускалась. Именованный тест: `BattleResultsRows_ModuleChanceArrestAndHiddenDestroyed`. Запуск из `D:/DeepSpaceSaga/DSS`; runner ограничивает test process tree 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0002-tk0003-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsRows_ModuleChanceArrestAndHiddenDestroyed' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Demo/BattleResultsLootRows.cs src/DeepSpaceSaga.Client/UI/Demo/BattleResultsSourceList.cs src/DeepSpaceSaga.Client/UI/Demo/BattleLootPresentation.cs src/DeepSpaceSaga.Client/UI/Demo/ModuleSalvagePresentation.cs
```

Результаты и любые перенаправления — только под `$run`. При проверке UI использовать headless тест. Ручная приёмка описана в эпике.

## Definition of Done

Именованный тест подтверждает наблюдаемые критерии из `serves`. Scoped build и format проходят либо не связанное с тикетом исходное падение подтверждено и записано. Контракт, зависимости, инварианты и границы соблюдены.

## Self-containment check

Зависимости: [EP-0042-US-0002-TK-0002-source-accordion](../../EP-0042-US-0002-loot-accordion/EP-0042-US-0002-TK-0002-source-accordion/EP-0042-US-0002-TK-0002-source-accordion.md). Точный allowlist и owning test project указаны выше.

## Уточнение реализации

Фактический список находится в `BattleResultsScreen.Sources.cs`. Изменения добавляют строки и кнопки в этот список и футер повтора записи. Для лома Engine передаёт базовую оценку из существующего каталога, когда она определена и помещается в long. Добавлены соответствующие поля проекций в Contracts и Engine.

## Исполнение

- Строки строятся из авторитетного ledger и заполняют ширину списка. Кнопки передают ID строки и источника через существующие команды. Повтор при сбое записи использует исходную identity.
- Сборка Client Tests, scoped whitespace format и `git diff --check` прошли после исправления формы assert. Тематические тесты запланированы на границу истории.
