---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0004-point-aim-countermissiles"
ticket: "EP-0012-US-0004-TK-0003-remove-auto-pd"
title: "remove-auto-pd"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests"]
files_touched: 20
serves: ["US4-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 4
---

# EP-0012-US-0004-TK-0003-remove-auto-pd

## Why

[Противоракеты](../EP-0012-US-0004-point-aim-countermissiles.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: remove-auto-pd; доказать US4-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (20 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs:8; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs` (retired by US4-TK3) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs:7; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs` (removed by US3-TK5) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Production reference found during retirement audit | Remove scheduled automatic launches, burst/intercept/expiry/coast branches and legacy counter prediction; retain point detonation, NPC dodge/launch ordering and reload completion. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Production reference found during retirement audit | Remove old defense dispatch/preview/guidance calls and plan-state reset; globally reject retired defense command IDs. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.DefensePreview.cs` | Production reference found during retirement audit | Delete target/chance/intercept preview; point previews use current public Client observations/shared geometry. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs` | Production reference found during retirement audit | Delete retired frozen-chance counter event writer; point facts remain authoritative. |

| `src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs` | Production reference found during retirement audit | Reject active legacy counter projectile/burst/auto/guiding continuations while preserving current point launcher/profile/reload validation. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs` | Production reference found during retirement audit | Remove guidance target-plan state capture/restore; current point flights remain in PointCombatState, legacy projectile envelope stays empty. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs` | Production reference found during retirement audit | Remove obsolete target-plan cleanup from point flight removal; retain IDs, scatter, profile, deadlines and readiness. The retained receipt-eviction test reproduced a missing processed-launch-ID guard in TryStartPointLaunch; add the existing durable guard before any payload-dependent launch staging, after owner validation. |

| `tests/DeepSpaceSaga.Engine.Tests/AutomaticDefenseTests.cs` | Matching consumer found in the1292-test probe | Replace obsolete auto/chase/range-entry/chance cases with no-auto incoming flight and explicit captured area geometry; temporary static legacy helper references remain until their consumers are migrated. |

| `tests/DeepSpaceSaga.Engine.Tests/ManualCountermeasureFireTests.cs` | Matching consumer found in the1292-test probe | Migrate every old command assertion to point ownership/readiness/range/idempotency/replay/all-sides/orphan/no-roll equivalents; do not retain meaningless rejection of an unsupported old command as coverage. |

| `tests/DeepSpaceSaga.Engine.Tests/DefensePreviewProjectionTests.cs` | Matching consumer found in the1292-test probe | Replace server chance/target-lock preview with absent old preview, independent current operator scatter/profile/readiness and immutable point capture. |

| `tests/DeepSpaceSaga.Engine.Tests/CountermeasureBootstrapTests.cs` | Matching consumer found in the1292-test probe | Use actual source/deployed point catalog, independent operators/reloads, no auto, generated loadout and missing-operator/foreign-authority checks. |

| `tests/DeepSpaceSaga.Engine.Tests/PointCounterFixture.cs` | Matching consumer found in the1292-test probe | NEW: actual source catalog with explicitly zero scatter only for deterministic geometry; current raw scenario and controlled stationary poses/power. No legacy catalog or substitute simulator. |

| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveSchemaTests.cs` | Legacy defense continuation schema consumer | Migrate malformed captured fields, case normalization and continuation to real point flights; retain independent hostile-AI validation. Reject probability metadata injected into current point journals. |

| `tests/DeepSpaceSaga.Engine.Tests/CountermeasureLifetimeTests.cs` | Retired defense consumer | Replace guidance, missed-coast, auto retry and chance facts with point deadline, independent reload, physical-clock, file continuation and authoritative area-journal invariants; retain unrelated AI and economy checks. |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeJournalTests.cs` | Retired defense consumer | Replace guidance, missed-coast, auto retry and chance facts with point deadline, independent reload, physical-clock, file continuation and authoritative area-journal invariants; retain unrelated AI and economy checks. |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveContinuationTests.cs` | Retired defense consumer | Replace guidance, missed-coast, auto retry and chance facts with point deadline, independent reload, physical-clock, file continuation and authoritative area-journal invariants; retain unrelated AI and economy checks. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Удалить auto-intercept arbitration, burst, intercept roll и coasting из runtime; старые defense fire/enable/disable не принимаются. Остаточные helpers и legacy test migration — US18.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **No_automatic_defense_burst_or_intercept_roll**: без defense.launch не появляется ПРО; две установки перезаряжаются независимо.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0004TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0004TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US4-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: implementation и self-review выполнены. PASS 8 named / 1255 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0004-tk0003-results.json).

Removed old automatic counter arbitration, burst/intercept/chase/coast runtime and old defense command dispatch/server preview. Migrated eight matching test classes to real point catalog/fuse/area/no-roll/save invariants; exact old/new method mapping is in evidence. Fixed reproduced durable launch replay and injected probability journal acceptance. Current wider Engine probe: hard timeout300103ms with56 observed old-consumer failures. Rebuilt Client:2349passed/16failed. These are explicit OPEN integration gates assigned to remaining US18-TK1/operator and US3-TK5/all legacy consumers; no full-suite or complete US4-AC3 test-retirement claim yet. The1255 PASS scope excludes22 staged legacy classes. Native/product/balance remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.

2026-10-20 consumer prerequisite correction: point consumers precede production data activation; actual integration gates remain OPEN until activation/Save18. See [DAG](../../Tickets.md#consumer-prerequisite-correction).

2026-10-21 bounded retirement plan: remove functions and callers rather than returning fake null/empty gameplay results. Keep shared projectile identity counters, processed physical clock, point profile state and reload completion. The unused counter RNG envelope remains unchanged until final Save18/legacy cleanup; no interception draws remain. Legacy capability metadata may still deserialize for the temporary historical torpedo fixtures, but no automatic/manual old counter command is executable and no old active counter continuation is accepted. Matching old defense tests/consumers will be scoped before migration, with retained invariant mapping to new point cases. Full integration is not claimed until US18-TK5.

Consumer probe:70failed/1222passed in1292; all306point cases pass. Expand matching tests before edits; map retired probability/guidance/auto-only contracts to explicit area behavior, preserve all still-applicable validation, identities, ownership, chronology, durability and no-side-effect assertions. No test is skipped or made vacuously true by continuing to send retired commands.

Retained-invariant defect reproduction: new ManualCountermeasureFireTests durable replay case spawned a second real counter after receipt eviction and Save18 reload with a changed module. Preserve and fix this failure, do not delete/relax its no-spawn/no-RNG/readiness assertions. The wrong-family launch rejection uses unknown_command_type by the current point category contract, rather than the old dictionary unknown_module reason.
