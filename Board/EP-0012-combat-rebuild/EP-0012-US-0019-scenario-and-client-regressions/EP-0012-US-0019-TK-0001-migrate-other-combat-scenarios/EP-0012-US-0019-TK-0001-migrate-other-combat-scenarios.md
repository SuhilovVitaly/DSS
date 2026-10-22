---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0019-scenario-and-client-regressions"
ticket: "EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios"
title: "migrate-other-combat-scenarios"
stage: "implemented"
layer: "content-data-and-tooling-consumer"
depends_on: ["EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios"]
files_touched: 37
serves: ["US19-AC1", "US9-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 5
---

# EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios

## Why

[Совместимость сценариев и старых клиентских поверхностей](../EP-0012-US-0019-scenario-and-client-regressions.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: migrate-other-combat-scenarios; доказать US19-AC1, US9-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (37 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/Scenarios/PlayerShipOnly/scenario.json](../../../../src/DeepSpaceSaga.Client/Scenarios/PlayerShipOnly/scenario.json) | Существует: src/DeepSpaceSaga.Client/Scenarios/PlayerShipOnly/scenario.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Scenarios/CombatMukhaSingle/scenario.json](../../../../src/DeepSpaceSaga.Client/Scenarios/CombatMukhaSingle/scenario.json) | Существует: src/DeepSpaceSaga.Client/Scenarios/CombatMukhaSingle/scenario.json:1; { | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json` | Remaining retired autoDefenseEnabled data flags | Remove only obsolete automatic-PD flags; retain all trading/economy/loadout/physics data. |

| `src/DeepSpaceSaga.Client/Scenarios/Default_500/scenario.json` | Remaining retired autoDefenseEnabled data flags | Remove only obsolete automatic-PD flags; retain all trading/economy/loadout/physics data. |

| `src/DeepSpaceSaga.Client/Scenarios/Docked/scenario.json` | Remaining retired autoDefenseEnabled data flags | Remove only obsolete automatic-PD flags; retain all trading/economy/loadout/physics data. |

| `src/DeepSpaceSaga.Client/Scenarios/Undocked/scenario.json` | Remaining retired autoDefenseEnabled data flags | Remove only obsolete automatic-PD flags; retain all trading/economy/loadout/physics data. |

| `tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/CombatSessionRoundtripTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/DefeatNavigationTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/FullMapRenderEvidenceTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/ManualDefensePreviewTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/ManualDefenseUiFlowTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatUiFlowTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeJournalUiTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeSessionRoundtripTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/StationResourceFieldContentTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Client.Tests/TorpedoPreviewTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/CountermeasureBootstrapTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/DefeatedSessionSaveTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeCombatEndToEndTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveVersionTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0001Tests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/Support/LegacyCombatCatalog.cs` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/Support/LegacyCombatCatalog.json` | Reproduced old fixture dependency or intentionally changed current-source expectation | Preserve calibrated Basic characterization/component templates explicitly; current shipped acceptance keeps actual v3 data. Update actual Tetrarch inertia/AI/loadout and retired-flag hash expectations; preserve continuation, masks, failures and noncombat data. |

| `tests/DeepSpaceSaga.Engine.Tests/TorpedoLaunchTests.cs` | Repaired regression still has direct old calibrated scenario readers | Read the explicit historical Basic template; preserve all command rejection, damage/RNG and continuation assertions. |

| `tests/DeepSpaceSaga.Engine.Tests/ModuleDamageTests.cs` | Repaired regression still has direct old calibrated scenario readers | Read the explicit historical Basic template; preserve all command rejection, damage/RNG and continuation assertions. |

| `tests/DeepSpaceSaga.Engine.Tests/BasicCombatEndToEndTests.cs` | Remaining3 failures share nested historical CombatRun source reader | Use explicit pre-activation Basic scenario; retain live clock, command, hit/RNG, damage and continuation assertions. |

| `tools/DeepSpaceSaga.EconomyBalance/ClusterBalanceRun.cs` | Final gate13FAIL: cargo registry copy drops CombatRules | Preserve actual production point rules in cargo-configured registry; no balance/catalog retuning. |
| `tests/DeepSpaceSaga.EconomyBalance.Tests/LongVoyageRunnerTests.cs` | Owning59 tests include13 load failures | Add starter/upgrade actual point-scene Save18 reload regression, then rerun owning suite bounded. |

## Public API after the change

No public API change. All eight shipped scenarios use current point data without serialized automatic-PD flags; Pirate uses dedicated Tetrarch engines and explicit AI. Single-Mukha remains one. Historical Basic specimens are explicit test-only calibration, not default-game acceptance.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Перевести PlayerShipOnly и CombatMukhaSingle на новые engine/launcher/AI ids; сохранить число NPC и предназначение каждого сценария; баланс 1×1 не включать.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: content-data; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **All_shipped_combat_scenarios_load_and_single_stays_single**: production registry и deployed output, counts сохранены.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0019TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0019TK0001Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US19-AC1, US9-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 2 named / 2360 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0019-tk0001-results.json).

Actual source/deployed8 scenarios load with unchanged counts and no auto-PD flags. Pirate Tetrarch engines execute20deg/s; single-Mukha remains one on v3 data. Independent Basic characterization templates preserve prior assertions.851 bounded Engine PASS; source Default_500 bytes unchanged except retired flag.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 consumer prerequisite correction: point consumers precede production data activation; actual integration gates remain OPEN until activation/Save18. See [DAG](../../Tickets.md#consumer-prerequisite-correction).

Save18 prerequisite correction: see [execution DAG](../../Tickets.md#save18-prerequisite-correction).

Before implementation: PlayerShipOnly retains original identities, counts, positions, initial motion, crew and module geometry; both Tetrarch-class ships use dedicated Tetrarch engine, basic point launchers remain appropriate for their class, NPC gets explicit25/8/35 AI. CombatMukhaSingle retains metadata/id and one SPC-MUKHA-01 using the matching v3 CombatMukha player/enemy data; no new1v1 balance gate. Remove stale automatic-PD fields from every remaining shipped scenario while preserving all unrelated data. Current default source/deployed acceptance uses actual registry; calibrated component/historical fixtures may require bounded explicit migration after reproduced failures.

Before fixture changes: actual default source/deployed scenario2/2PASS; broad reproduction Client78failed/2282passed and Engine346failed/505passed. Historical registries lack new Tetrarch IDs; low-level Basic6km/s/4deg/s tests must retain their independently calibrated scenario. Add explicit historical PlayerShipOnly snapshot to existing test-only catalog, never use it for current scenario acceptance. Current Tetrarch scenario tests instead assert new20deg/s/AI25/8/35. Fix an existing Pirate test that requests a past1000ms save after advancing2000ms: compare both uninterrupted/restored module states at the same2000ms; no runtime rewind support is added. Protect Default_500 with an independently derived new LF-normalized hash after removing only its retired flag. Single-Mukha pursuit test now uses actual approved spawn and real5/8km/s registry, removing the obsolete300km-to100km synthetic scaling.


2026-10-21 corrective scope BEFORE edits: final all-project gate found13/59 EconomyBalance failures, Point launcher requires current combatRules. ConfigureCargo copied modules and catalogs but omitted CombatRules. Fix only this consumer registry copy and meaningful actual scene/load regression. Documentation/graph finalization resumes after separate repair publication.

Corrective result: actual focused2FAIL before→2PASS after; whole61/61 exact disjoint case IDs/discovery multiset PASS in bounded partitions. Initial13FAIL/46PASS and full300s timeout preserved, not averaged/relabelled. Solution build/scoped formatter/diff PASS. [Evidence](../../evidence/economy-consumer-repair-results.json). Separate corrective commit/push follows; final documentation/graph resumes afterward.
