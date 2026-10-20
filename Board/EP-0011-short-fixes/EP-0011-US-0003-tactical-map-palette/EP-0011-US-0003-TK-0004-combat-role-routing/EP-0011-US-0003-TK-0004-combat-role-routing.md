---
epic: EP-0011-short-fixes
story: EP-0011-US-0003-tactical-map-palette
ticket: EP-0011-US-0003-TK-0004-combat-role-routing
title: Боевые линии и радиусы
stage: approved
layer: client
depends_on: [EP-0011-US-0003-TK-0002-object-and-combat-palette]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
created: 2026-10-18T10:15:17.840902+00:00
revision: 1
---

# Боевые линии и радиусы

## Why
Реализовать соответствующие роли полного продуктового контракта US-0003/TK-0001. Таблицы RGB/opacity и A-P01–A-P04 каноничны; HSL S повторно не применять.

## Decisions / Assumptions
D-P03: пользователь: «Да, подготовь исполнительные тикеты в Board и сразу выполни их с проверками. Без commit/push и запуска игры.» Источник: текущий чат, 2026-10-18; created — время записи.
A-P05: безопасные derived shadow/highlight допустимы, старые насыщенные подслои удаляются. Существующий alpha fade подписей умножается на заданную непрозрачность. Все входы известны после dependency.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `tests/DeepSpaceSaga.Client.Tests/TacticalPaletteRoutingTests.cs` | NEW — явно разрешённый новый test file | Только palette/routing/регрессии в рамках Why |

Разрешён только этот набор implementation files; Board execution metadata разрешены отдельным запросом пользователя. Matching project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.

## Public API after the change
internal SKColor CombatTargetPathColor(ObjectMotionSnapshot?,bool=false); internal SKColor CountermeasureTrailColor(double). Torpedo terminal history remains stored; DrawCombatEffects hides its line.

DrawCombatGeometry(...,string? targetId = null): вспомогательный путь target RGB alpha30%; нейтральный текст; разные torpedo/auto/manual rings; CountermeasureTrail alpha10..40% по длине. Forecast/expiry/geometry не менять.
Engine/Contracts/Motion/save API — без изменений.

## Implementation steps
1. Применить role→RGB/alpha продуктовой таблицы к перечисленным render seams и runtime/defaults.
2. Добавить meaningful routing/raster/boundary регрессии в перечисленные tests; обновить прежние palette expectations.
3. Выполнить focused/full Client tests, Client build, scoped format и diff check. Обновить evidence в Board.

## Out of scope / Invariants
Нет изменений simulation/AI/RNG/timing, прогнозной математики, geometry/visibility, неизвестных объектов, layout и чужих изменений. Торпедный history сохранён и скрыт. Не запускать игру, не commit/push.

## Tests
Named suites: `TacticalPaletteRoutingTests`. Проверяются фактические resolver/settings/render paths, rgba/default fallback, alpha endpoints и сохранение существующих регрессий по роли тикета.
```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~TacticalPaletteRoutingTests"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -m:1
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs tests/DeepSpaceSaga.Client.Tests/TacticalPaletteRoutingTests.cs
git diff --check
```

## Definition of Done
Implementation + automated PASS при реальном успешном выводе; AC-06 и native/product gates остаются OPEN по запрету запуска. Совпадение цвета проверяется автоматикой; читаемость/FPS не объявляются доказанными.

## Self-containment check
Точный allowlist <=5 файлов, один owning Client layer, matching tests; зависимость задаёт базовые semantic roles. Полный контракт находится в US-0003/TK-0001, дополнительных продуктовых решений не требуется.

## Evidence
Implementation DONE; focused/full Client tests PASS (175/175 and 2085/2085); Client build PASS (0 warnings/errors); native/product OPEN.

Validation details: [общий execution evidence](../../ImplementationStatus.md#palette-execution--2026-10-18). Scoped format остальных файлов PASS. Для GridRendererTests.cs общий scoped format имеет 9 pre-existing WHITESPACE diagnostics на строках22/23/60, подтверждено git show HEAD. Новые строки без diagnostics. diff check PASS. Commit/push/игра NOT RUN по D-P03.
