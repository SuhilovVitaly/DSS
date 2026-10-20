---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0001-contracts-and-schema"
ticket: "EP-0012-US-0001-TK-0005-launcher-profile-schema"
title: "launcher-profile-schema"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0001-TK-0004-combat-rules-schema"]
files_touched: 4
serves: ["US1-AC4"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0001-TK-0005-launcher-profile-schema

## Why

[Контракты и схема данных](../EP-0012-US-0001-contracts-and-schema.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: launcher-profile-schema; доказать US1-AC4 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs](../../../../src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs) | Существует: src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs:5; internal sealed record ModuleTypeDefinition( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs](../../../../src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs) | Существует: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:8; public static class EngineContentLoader | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs](../../../../src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs) | Существует: src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:9; internal sealed class GameDataRegistry | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0005Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

Proposed: profiles[{id,speedKmS,blastRadiusKm}], rangeKm, reloadMs, scatterPercent; torpedo damage/armorPiercingPercent. Все числа конечные, profile IDs непустые и уникальные; reloadMs >= 0. JSON DTO и mapping располагаются в существующем EngineContentLoader.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. **Задача.**
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Launcher_profiles_require_unique_ids_positive_speed_radius_and_range**: пустые/дублированные профили и NaN/Infinity отвергаются.
- **Launcher_profile_schema_distinguishes_damage_from_countermissile**: у торпеды damage/AP, у ПРО нет урона кораблю.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0001TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0001TK0005Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0005Tests.cs
git diff --check
```

## Definition of Done

- Served AC US1-AC4 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
