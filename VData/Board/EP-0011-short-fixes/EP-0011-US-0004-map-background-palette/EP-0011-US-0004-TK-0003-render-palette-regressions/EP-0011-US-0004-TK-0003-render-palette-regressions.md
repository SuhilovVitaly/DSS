---
epic: EP-0011-short-fixes
story: EP-0011-US-0004-map-background-palette
ticket: EP-0011-US-0004-TK-0003-render-palette-regressions
title: Регрессии новой семантики корабля, выхлопа и HP
stage: approved
layer: client
depends_on: [EP-0011-US-0003-TK-0003-navigation-and-trails, EP-0011-US-0003-TK-0004-combat-role-routing, EP-0011-US-0003-TK-0005-labels-and-grid]
files_touched: 3
serves: [AC-01, AC-05, AC-06]
created: 2026-10-18T10:22:18.237117+00:00
revision: 1
---

# Палитра — интеграционные регрессии

## Why
Full Client run выявил старые ожидания green player/orange exhaust/lime HP/opaque wreck. Обновить только эти визуальные ожидания, сохранив проверки orientation, marker diameter/zoom и HP geometry.

## Decisions / Assumptions
D-P03 разрешает создание и выполнение тикетов с проверками без игры/commit/push. A-P07: новый regression ticket дополняет собственные tests implementation tickets.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/GameSessionPlayerShipTests.cs` | tests/DeepSpaceSaga.Client.Tests/GameSessionPlayerShipTests.cs:1, render assertions прежней палитры | Новые RGB/alpha; geometry invariants оставить |
| `tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs` | tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs:1, render assertions прежней палитры | Новые RGB/alpha; geometry invariants оставить |
| `tests/DeepSpaceSaga.Client.Tests/CombatTrajectoryTests.cs` | tests/DeepSpaceSaga.Client.Tests/CombatTrajectoryTests.cs:1, render assertions прежней палитры | Новые RGB/alpha; geometry invariants оставить |

## Public API after the change
No API change. CombatVisualSettings.Load(string), schemaVersion1.

## Implementation steps
Обновить expected roles; редактировать countermeasure через JsonNode, проверить invalid optional fallback; сохранить scenario behavior assertions; выполнить focused/full Client suites.

## Out of scope / Invariants
Нет production изменений, gameplay/Engine/Contracts/Motion/save неизменны; JSON #RRGGBBAA, все прежние ключи поддерживаются.

## Tests
GameSessionPlayerShipTests, CombatMapVisualTests, CombatTrajectoryTests. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1`.

## Definition of Done
Named + full Client tests PASS; scoped format/diff PASS. Native/product OPEN.

## Evidence
Implementation DONE; focused/full Client tests PASS (175/175 and 2085/2085); Client build PASS; native/product OPEN.

Validation details: [общий execution evidence](../../ImplementationStatus.md#palette-execution--2026-10-18). Scoped format остальных файлов PASS. Для GridRendererTests.cs общий scoped format имеет 9 pre-existing WHITESPACE diagnostics на строках22/23/60, подтверждено git show HEAD. Новые строки без diagnostics. diff check PASS. Commit/push/игра NOT RUN по D-P03.
