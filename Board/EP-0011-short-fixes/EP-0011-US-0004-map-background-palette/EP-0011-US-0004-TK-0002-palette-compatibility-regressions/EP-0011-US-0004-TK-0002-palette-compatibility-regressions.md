---
epic: EP-0011-short-fixes
story: EP-0011-US-0004-map-background-palette
ticket: EP-0011-US-0004-TK-0002-palette-compatibility-regressions
title: Совместимость внешней палитры и регрессии старого набора
stage: approved
layer: client
depends_on: [EP-0011-US-0003-TK-0002-object-and-combat-palette]
files_touched: 4
serves: [AC-01, AC-05, AC-06]
created: 2026-10-18T10:22:18.237117+00:00
revision: 1
---

# Палитра — интеграционные регрессии

## Why
Focused/full run выявили четыре существующих теста с прежними цветами и числом JSON keys. Обновить контрактный JSON regression и mutation test, чтобы проверять production palette и реальные loader/fallback paths. Новый продуктовый scope отсутствует; необходимая проверка разрешённой реализации.

## Decisions / Assumptions
D-P03 разрешает создание и выполнение тикетов с проверками без игры/commit/push. A-P07: новый regression ticket дополняет собственные tests implementation tickets.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/CountermeasurePaletteTests.cs` | tests/DeepSpaceSaga.Client.Tests/CountermeasurePaletteTests.cs:1, старые palette assertions | Новые RGB/alpha и mutation по JSON key |
| `tests/DeepSpaceSaga.Client.Tests/CountermeasureVisualSettingsTests.cs` | tests/DeepSpaceSaga.Client.Tests/CountermeasureVisualSettingsTests.cs:1, старые palette assertions | Новые RGB/alpha и mutation по JSON key |
| `tests/DeepSpaceSaga.Client.Tests/CombatPaletteContentTests.cs` | tests/DeepSpaceSaga.Client.Tests/CombatPaletteContentTests.cs:1, старые palette assertions | Новые RGB/alpha и mutation по JSON key |
| `tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs` | tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs:1, старые palette assertions | Новые RGB/alpha и mutation по JSON key |

## Public API after the change
No API change. CombatVisualSettings.Load(string), schemaVersion1.

## Implementation steps
Обновить expected roles; редактировать countermeasure через JsonNode, проверить invalid optional fallback; сохранить scenario behavior assertions; выполнить focused/full Client suites.

## Out of scope / Invariants
Нет production изменений, gameplay/Engine/Contracts/Motion/save неизменны; JSON #RRGGBBAA, все прежние ключи поддерживаются.

## Tests
CountermeasurePaletteTests, CountermeasureVisualSettingsTests, CombatPaletteContentTests, PirateScenarioTests. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1`.

## Definition of Done
Named + full Client tests PASS; scoped format/diff PASS. Native/product OPEN.

## Evidence
Implementation DONE; focused/full Client tests PASS (175/175 and 2085/2085); Client build PASS; native/product OPEN.

Validation details: [общий execution evidence](../../ImplementationStatus.md#palette-execution--2026-10-18). Scoped format остальных файлов PASS. Для GridRendererTests.cs общий scoped format имеет 9 pre-existing WHITESPACE diagnostics на строках22/23/60, подтверждено git show HEAD. Новые строки без diagnostics. diff check PASS. Commit/push/игра NOT RUN по D-P03.
