---
epic: EP-0011-short-fixes
story: EP-0011-US-0003-tactical-map-palette
ticket: EP-0011-US-0003-TK-0002-object-and-combat-palette
title: Палитра объектов и runtime-настройки
stage: approved
layer: client
depends_on: []
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
created: 2026-10-18T10:15:17.837194+00:00
revision: 1
---

# Палитра объектов и runtime-настройки

## Why
Реализовать соответствующие роли полного продуктового контракта US-0003/TK-0001. Таблицы RGB/opacity и A-P01–A-P04 каноничны; HSL S повторно не применять.

## Decisions / Assumptions
D-P03: пользователь: «Да, подготовь исполнительные тикеты в Board и сразу выполни их с проверками. Без commit/push и запуска игры.» Источник: текущий чат, 2026-10-18; created — время записи.
A-P05: безопасные derived shadow/highlight допустимы, старые насыщенные подслои удаляются. Существующий alpha fade подписей умножается на заданную непрозрачность. Все входы известны после dependency.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs` | src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs` | src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json` | src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `tests/DeepSpaceSaga.Client.Tests/SpaceMapColorResolverTests.cs` | tests/DeepSpaceSaga.Client.Tests/SpaceMapColorResolverTests.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `tests/DeepSpaceSaga.Client.Tests/CombatVisualSettingsTests.cs` | tests/DeepSpaceSaga.Client.Tests/CombatVisualSettingsTests.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |

Разрешён только этот набор implementation files; Board execution metadata разрешены отдельным запросом пользователя. Matching project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.

## Public API after the change
Internal TacticalMapPalette in SpaceMapColorResolver.cs: semantic SKColor roles, Alpha(double percent), Trail(SKColor,double,double near=35,double far=5). Wreck routing сохраняет alpha65%.

GetColor(string?, string?), CombatVisualSettings.Load(string): новые RGB/alpha, optional TorpedoRange/ManualDefenseRange с fallback v1; production JSON равен Default.
Engine/Contracts/Motion/save API — без изменений.

## Implementation steps
1. Применить role→RGB/alpha продуктовой таблицы к перечисленным render seams и runtime/defaults.
2. Добавить meaningful routing/raster/boundary регрессии в перечисленные tests; обновить прежние palette expectations.
3. Выполнить focused/full Client tests, Client build, scoped format и diff check. Обновить evidence в Board.

## Out of scope / Invariants
Нет изменений simulation/AI/RNG/timing, прогнозной математики, geometry/visibility, неизвестных объектов, layout и чужих изменений. Торпедный history сохранён и скрыт. Не запускать игру, не commit/push.

## Tests
Named suites: `SpaceMapColorResolverTests|FullyQualifiedName~CombatVisualSettingsTests`. Проверяются фактические resolver/settings/render paths, rgba/default fallback, alpha endpoints и сохранение существующих регрессий по роли тикета.
```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~SpaceMapColorResolverTests|FullyQualifiedName~CombatVisualSettingsTests"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -m:1
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json tests/DeepSpaceSaga.Client.Tests/SpaceMapColorResolverTests.cs tests/DeepSpaceSaga.Client.Tests/CombatVisualSettingsTests.cs
git diff --check
```

## Definition of Done
Implementation + automated PASS при реальном успешном выводе; AC-06 и native/product gates остаются OPEN по запрету запуска. Совпадение цвета проверяется автоматикой; читаемость/FPS не объявляются доказанными.

## Self-containment check
Точный allowlist <=5 файлов, один owning Client layer, matching tests; зависимость задаёт базовые semantic roles. Полный контракт находится в US-0003/TK-0001, дополнительных продуктовых решений не требуется.

## Evidence
Implementation DONE; focused/full Client tests PASS (175/175 and 2085/2085); Client build PASS (0 warnings/errors); native/product OPEN.

Validation details: [общий execution evidence](../../ImplementationStatus.md#palette-execution--2026-10-18). Scoped format остальных файлов PASS. Для GridRendererTests.cs общий scoped format имеет 9 pre-existing WHITESPACE diagnostics на строках22/23/60, подтверждено git show HEAD. Новые строки без diagnostics. diff check PASS. Commit/push/игра NOT RUN по D-P03.
