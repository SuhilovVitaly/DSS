---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0009-scenarios-and-saves"
ticket: "EP-0012-US-0009-TK-0003-save-format-18"
title: "save-format-18"
stage: "implemented"
layer: "contracts-engine"
depends_on: ["EP-0012-US-0016-TK-0001-combat-localization"]
files_touched: 16
serves: ["US9-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0009-TK-0003-save-format-18

## Why

[Сценарии и сохранения](../EP-0012-US-0009-scenarios-and-saves.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: save-format-18; доказать US9-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (16 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:7; public static class SaveFormat | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs:9; public sealed record CombatStateData( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs:8; public sealed record CountermeasureStateData( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs:8; public sealed record HostileCombatSaveData( | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/Scenario/PointCombatSaveData.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |
| `tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveSchemaTests.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |
| `tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveVersionTests.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |
| `tests/DeepSpaceSaga.Engine.Tests/TradingEconomySaveSchemaTests.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |
| `tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs` | Existing or bounded new schema | Point continuation schema/validation and current-version expectations; preserve old-version atomic rejection and independent behavior assertions. |

| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Existing captured flight DTO | Require serialized captured numeric fields so missing AP/core/time cannot silently become valid zero defaults in Save18. |

| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeSessionRoundtripTests.cs` | Existing | Preserve malformed current-save generic-error and retry test by emitting the actual current version, not historical17. |

| `tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs` | Existing | Historical migration fixture must explicitly use historical hostile2 rather than retaining captured hostile3 under an older root envelope. |

## Public API after the change

SaveFormat.CurrentSaveFormatVersion=18; HostileCombatSaveData.CurrentVersion=3. GameStateData adds optional PointCombatState carrying versioned captured projectiles, evasion/base course, named RNG, NPC dodge and encounter/statistics records. OwnProjectileSnapshot captured numeric JSON members become required. Runtime capture/restore is the next ticket; schema-only evidence is not whole point Save/Load acceptance.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Поднять root save18; описать версионированный point-projectile/evasion/encounter payload и hostile3. Это schema ticket; capture/restore/validation подключаются следующими тикетами.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Save18_schema_preserves_captured_projectiles_and_rng**: сохранены own+enemy+countermissile, evasion/base course/encounter/stats и counters.
- **Save18_schema_rejects_invalid_deadline_profile_or_rng**: невалидный payload не публикует частичное состояние.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0009TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0009TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US9-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 19 named / 809 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0009-tk0003-results.json).

Root Save18/hostile3 and versioned point continuation schema PASS; 256 Contracts and 2330 non-budget Client PASS. Old17 rejected before body parsing, file/world unchanged. Captured numeric JSON members required; acknowledged Idle keeps final stats. This is schema-only: actual point capture/restore and consistency remain next two tickets. Full Engine, strict forecast budgets and native gates OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

Save18 prerequisite correction: see [execution DAG](../../Tickets.md#save18-prerequisite-correction).

Scope correction before code: add a distinct versioned PointCombatStateData payload containing captured own/enemy/counter flights, named RNG, evasion/base state, NPC dodge and encounter/stats continuation. Add structural validation at the existing loader boundary. Root version becomes18 and old17 is rejected before body parsing. Existing current-save assertions migrate to18; malformed future-version test moves18 to19. Runtime capture/restore and cross-payload consistency remain the following two tickets, not schema evidence.
