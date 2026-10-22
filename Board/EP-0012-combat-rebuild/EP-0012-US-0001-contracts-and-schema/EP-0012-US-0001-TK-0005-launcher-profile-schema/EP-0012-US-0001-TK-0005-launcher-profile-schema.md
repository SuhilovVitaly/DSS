---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0001-contracts-and-schema"
ticket: "EP-0012-US-0001-TK-0005-launcher-profile-schema"
title: "launcher-profile-schema"
stage: "implemented"
layer: "engine-and-contracts"
depends_on: ["EP-0012-US-0001-TK-0004-combat-rules-schema"]
files_touched: 7
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

Актуальный allowlist (7 файлов, включая matching tests двух слоёв); расширение обосновано ниже. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs](../../../../src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs) | Существует: src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs:5; internal sealed record ModuleTypeDefinition( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs](../../../../src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs) | Существует: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:8; public static class EngineContentLoader | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs](../../../../src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs) | Существует: src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:9; internal sealed class GameDataRegistry | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0005Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Existing launcher DTO | Shared immutable LaunchProfileSnapshot and own launcher profile/scatter metadata. |
| `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs` | Existing defense DTO | Own defense profile/scatter metadata. |
| `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0005ContractTests.cs` | **NEW** | Profile metadata roundtrip and missing legacy capabilities. |

## Public API after the change

Proposed: profiles[{id,speedKmS,blastRadiusKm}], rangeKm, reloadMs, scatterPercent; torpedo damage/armorPiercingPercent. Все числа конечные, profile IDs непустые и уникальные; reloadMs >= 0. JSON DTO и mapping располагаются в существующем EngineContentLoader.

### Уточнение исполнения — 2026-10-20

Dependency TK-0004 опубликован: `c19dafdf185ac279133cb78afcdef795969912d5`, remote SHA проверен. Engine schema и общий DTO выполняются одним функциональным шагом по явно разрешённому расширению scope: Client не имеет доступа к internal ModuleTypeDefinition и должен получать profile choices от Engine, а не дублировать конфигурацию. Добавляется `LaunchProfileSnapshot(ProfileId, SpeedKmS, BlastRadiusKm)`; launcher/defense снапшоты получают immutable Profiles и nullable ScatterPercent. Engine content mapping использует этот чистый Contracts value type. Producer заполняется в runtime тикетах; отсутствие метаданных у legacy не заменяется кодовыми профилями. Новых зависимостей между проектами нет. Прежнее правило одного production layer ниже заменено этим явно ограниченным Engine/Contracts scope; архитектурное разделение сохраняется.

Новые поля обязательны вместе для point launcher. Старые module fixtures временно загружаются прежней веткой до cleanup. Новый набор отвергает legacy guidance/chance/burst параметры. JSON сохраняет действующие имена `torpedoDamage`, `armorPiercingPercent`, `armorDamagePercent`; у ПРО damage/AP запрещены. На данном шаге не меняются production JSON и правила исполнения оружия. Общий Engine gate после предыдущего hard timeout остаётся OPEN; вместо повторения известного тяжёлого прогона выполняется bounded content/consumer regression, а весь корпус проверяется на интегрированной реализации.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. **Задача.**
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

План проверок; фактические результаты исполнения приведены в Status / Evidence.

- **Launcher_profiles_require_unique_ids_positive_speed_radius_and_range**: пустые/дублированные профили и NaN/Infinity отвергаются.
- **Launcher_profile_schema_distinguishes_damage_from_countermissile**: у торпеды damage/AP, у ПРО нет урона кораблю.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0001TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

Дополнительный matching project для общего DTO: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`, class `PointCombatUS0001TK0005ContractTests`: captured profile roundtrip, отсутствие ship damage у ПРО, отсутствие выдуманных capabilities в legacy.

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

2026-10-20: implementation и self-review выполнены. Engine named tests PASS 39/39; bounded content/save/consumer regression PASS 387/387; Contracts named PASS 3/3, полный Contracts PASS 239/239; owning builds и итоговая сборка всего решения 0 warnings/errors; scoped whitespace/full formatter двух слоёв и diff check PASS. Все процессы hard cap 300000 ms.

[Engine команды/stdout и solution build](../../evidence/tk0005-results.json) · [Contracts команды/stdout](../../evidence/tk0005-contracts-results.json). В full Contracts сначала воспроизведён дефект совместимости: Defender_status_available_on_npc_snapshot, 238 PASS / 1 FAIL, default ImmutableArray отличался от Empty после JSON. Исправлен production DTO: Profiles nullable, отсутствующий legacy capability остаётся null; исходный assert не менялся. Затем полный Contracts 239/239 PASS.

Self-review проверил atomic validation до публикации registry, полный набор required полей, null/empty/duplicates, ordinal IDs, finite speed/radius/range/scatter, zero reload/scatter, неверную category, missing/nonfinite file+field diagnostics, damage/AP только торпедам и запрет смешанных legacy fields. Это review исполнителя, не независимое review. Producer Profiles заполняется в runtime тикетах; типизация не подменяет интеграцию. Общий Engine gate OPEN после TK-0004 timeout; native/product/balance OPEN. Отдельный commit/push разрешён текущим поручением; SHA проверяется после коммита.
