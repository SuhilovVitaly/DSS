---
epic: EP-0011-short-fixes
story: EP-0011-US-0003-tactical-map-palette
ticket: EP-0011-US-0003-TK-0005-labels-and-grid
title: Подписи, HP и сетка
stage: approved
layer: client
depends_on: [EP-0011-US-0003-TK-0002-object-and-combat-palette, EP-0011-US-0004-TK-0001-background-layer-routing]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
created: 2026-10-18T10:15:17.842699+00:00
revision: 1
---

# Подписи, HP и сетка

## Why
Реализовать соответствующие роли полного продуктового контракта US-0003/TK-0001. Таблицы RGB/opacity и A-P01–A-P04 каноничны; HSL S повторно не применять.

## Decisions / Assumptions
D-P03: пользователь: «Да, подготовь исполнительные тикеты в Board и сразу выполни их с проверками. Без commit/push и запуска игры.» Источник: текущий чат, 2026-10-18; created — время записи.
A-P05: безопасные derived shadow/highlight допустимы, старые насыщенные подслои удаляются. Существующий alpha fade подписей умножается на заданную непрозрачность. Все входы известны после dependency.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `src/DeepSpaceSaga.Client/UI/GridRenderer.cs` | src/DeepSpaceSaga.Client/UI/GridRenderer.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs` | tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `tests/DeepSpaceSaga.Client.Tests/GridRendererTests.cs` | tests/DeepSpaceSaga.Client.Tests/GridRendererTests.cs:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |
| `DB/Documentation/03-Design/TacticalMapSpecification.md` | DB/Documentation/03-Design/TacticalMapSpecification.md:1; существующий palette/render/test seam | Только palette/routing/регрессии в рамках Why |

Разрешён только этот набор implementation files; Board execution metadata разрешены отдельным запросом пользователя. Matching project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.

## Public API after the change
ObjectLabelRenderer.DrawPlaques(...,CameraState,SolarSystemMapSnapshot?=null) resolves known planet accent by Kind; requires US-0004/TK-0001. GridRenderer.LevelColor preserves existing premixing/spacing.

ObjectLabelRenderer.Draw: название #DCE6EE 95%, фон #15202B 92%, accent semantic RGB; GridRenderer.LevelColor: #12171C..#232C34 без повторных границ. Синхронизировать палитру design spec.
Engine/Contracts/Motion/save API — без изменений.

## Implementation steps
1. Применить role→RGB/alpha продуктовой таблицы к перечисленным render seams и runtime/defaults.
2. Добавить meaningful routing/raster/boundary регрессии в перечисленные tests; обновить прежние palette expectations.
3. Выполнить focused/full Client tests, Client build, scoped format и diff check. Обновить evidence в Board.

## Out of scope / Invariants
Нет изменений simulation/AI/RNG/timing, прогнозной математики, geometry/visibility, неизвестных объектов, layout и чужих изменений. Торпедный history сохранён и скрыт. Не запускать игру, не commit/push.

## Tests
Named suites: `ObjectLabelTests|FullyQualifiedName~GridRendererTests`. Проверяются фактические resolver/settings/render paths, rgba/default fallback, alpha endpoints и сохранение существующих регрессий по роли тикета.
```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~ObjectLabelTests|FullyQualifiedName~GridRendererTests"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -m:1
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs src/DeepSpaceSaga.Client/UI/GridRenderer.cs tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs tests/DeepSpaceSaga.Client.Tests/GridRendererTests.cs DB/Documentation/03-Design/TacticalMapSpecification.md
git diff --check
```

## Definition of Done
Implementation + automated PASS при реальном успешном выводе; AC-06 и native/product gates остаются OPEN по запрету запуска. Совпадение цвета проверяется автоматикой; читаемость/FPS не объявляются доказанными.

## Self-containment check
Точный allowlist <=5 файлов, один owning Client layer, matching tests; зависимость задаёт базовые semantic roles. Полный контракт находится в US-0003/TK-0001, дополнительных продуктовых решений не требуется.

## Evidence
Implementation DONE; focused/full Client tests PASS (175/175 and 2085/2085); Client build PASS (0 warnings/errors); native/product OPEN.

Validation details: [общий execution evidence](../../ImplementationStatus.md#palette-execution--2026-10-18). Scoped format остальных файлов PASS. Для GridRendererTests.cs общий scoped format имеет 9 pre-existing WHITESPACE diagnostics на строках22/23/60, подтверждено git show HEAD. Новые строки без diagnostics. diff check PASS. Commit/push/игра NOT RUN по D-P03.
