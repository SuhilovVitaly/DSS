# EP-0012 project documentation impact — 2026-10-21

[Current point combat](PointAimCombat.md), [canonical requirements](../01-Requirements/EngineRequirements.md#point-aim-combat), [technical execution and open acceptance](../../Board/EP-0012-combat-rebuild/ImplementationStatus.md), [ticket/SHA/push ledger](../../Board/EP-0012-combat-rebuild/PublicationStatus.md).

Inventory combines the full baseline-to-result diff, every canonical Markdown document, root agent/developer shims, adjacent tool READMEs and combat/save/command searches. It covers runtime/contracts/motion, data/config/scenarios, saves, UI/reporting, diagnostics/analyzers and test evidence. No root README exists. Dated reports remain dated; current supersession takes precedence without rewriting old results.

| Area | Disposition |
|---|---|
| Canonical/release requirements | EP12 point/area/privacy/evasion/AI/encounter/Save18 contract replaces conflicting EP6–EP9 combat; unrelated economy/navigation remains |
| Engineering combat/report/forecast notes | New PointAimCombat; old formulas explicitly historical; EP10 delivery and display-admitted NPC model remain, point consumers updated |
| Mechanics/screens/design | Current profiles/rings/input/threats/module UI/Won/Defeat/save/scenarios, no hidden fuse/enemy modules; data-driven styles, RU/EN |
| Tooling/root guidance | SDK10.0.400/net8, analyzer and current native CLI, source comments clarify reserved legacy payloads; root shims retained |
| Cross-project regression | EconomyBalance ConfigureCargo now retains production CombatRules;13FAIL/46PASS then focused2FAIL→2PASS and owning61PASS; no economy or combat tuning |
| Scenarios | CombatMukha+exactSingle, noCombatShmel; PlayerShipOnly geometry/crew retained; Default500 raw diff only retired autoDefenseEnabled removal, normalized noncombat contract unchanged |
| Prior Board/history | EP8/9/10/11 status links to superseding EP12; historical test/FPS/native/balance evidence remains; earlier palette/human gates not closed |
| CombatSim/assets/backlog/economy investigations | CombatSim entirely unchanged underO11 and not production proof; asset workflows, dated unrelated investigations/backlog reviewed, no new scope |
| Current Board |19stories/73tickets/61AC, all technical tickets implemented; US14 balance/human acceptance open; exact per-ticket publication ledger |
| Graphify | Final offline fresh structure and selective semantic extraction; explicit scope/hash/location/edge/token audit; navigation only |

## Per-file inventory

| File | Review/disposition |
|---|---|
| `AGENTS.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `CLAUDE.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/00-Process/AGENTS.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/00-Process/CLAUDE.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/00-Process/DocumentationSystem.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/01-Requirements/EngineRequirements.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/01-Requirements/FirstReleaseRequirements.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Mechanics/CargoHold.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/CommandPanels.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Mechanics/CrewAndHabitation.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Mechanics/CrewDialogues.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Mechanics/Docking.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/ElectricityAndEnergyCells.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Mechanics/Fuel.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/IceMining.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Mechanics/Money.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/PassengerContracts.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/SolarSystemMapConcept.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/StationDialogues.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Mechanics/StationInventory.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Mechanics/TetrarchClass.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Mechanics/Trading.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Screens/Cargo.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/CharacterCommunication.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/Contracts.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/Defeat.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Screens/Dialog.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/Finance.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Screens/GameMenu.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Screens/GameSession.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Screens/Hire.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/Load.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Screens/Loot.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/MainMenu.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/Save.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Screens/ScenarioSelect.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/Screens/ScreenCatalog.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Screens/Settings.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/Ship.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/02-FirstRelease/Screens/Station.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/Screens/Trade.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/TechnicalTasks/CombatPresentationSynchronization.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/TechnicalTasks/CombatTrajectoryForecasting.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/02-FirstRelease/TechnicalTasks/ScreenNamingConventionRefactor.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/TechnicalTasks/StationEconomyProductionAndSizing.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/03-Design/DialoguePortraits.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/03-Design/FemaleFaceProportions.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/03-Design/PortraitGenerator.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/03-Design/PortraitGeneratorM4.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/03-Design/PortraitGeneratorW1.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/03-Design/PortraitGeneratorW2.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/03-Design/PortraitGeneratorW4.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/03-Design/TacticalButtons/STYLE.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/03-Design/TacticalMapSpecification.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/AiMapEnvironment.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/ApproachRoutes.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/BasicTorpedoCombat.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/CodeReview20260912/Fixes.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/CodeReview20260912/README.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/CombatReports.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/CountermeasureCombat.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/EP-0002-Implementation.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/EpicReview20261010/Fixes.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/EpicReview20261010/README.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/EpicReview20261010/Validation.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/ItemCatalogSchema.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/MapScaling20260914/README.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/ModuleCombat.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/ModuleCombatDocumentation.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/NpcObservedTrajectories.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/Performance500/README.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/Performance500/gpu-jitter-2026-09-24.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/Performance500/x100-smoothness-2026-09-16.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/PhaseThreeCombatAcceptance.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/PhaseThreeCombatDocumentation.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/PointAimCombat.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/04-Engineering/TradeUI20260912/README.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/04-Engineering/TradingAndSolarSystem.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/05-Backlog/ImplementationTaskDiffs03-12.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/05-Backlog/TechnicalTasksSmall2026-08-09.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/06-Tooling/AiMapGraphify.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `Documentation/06-Tooling/CountermeasureGraphify.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/06-Tooling/DeepSpaceSaga.PortraitAssets.md` | Reviewed; no changed combat/schema/UI contract or executable workflow impact |
| `Documentation/06-Tooling/GameSessionScreenUI.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/06-Tooling/TradingSolarGraphify.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/README.md` | Updated current contract/navigation or explicit historical supersession |
| `Documentation/Validation/EP-0003-Implementation.md` | Reviewed dated history or unrelated subsystem; measurements retained, no current point contract asserted |
| `src/DeepSpaceSaga.CombatSim/README.md` | Read-only historical independent simulator; explicitly excluded by O11, never production evidence |
| `tools/DeepSpaceSaga.CombatAnalyzer/README.md` | Updated current contract/navigation or explicit historical supersession |
| `tools/DeepSpaceSaga.Performance/README.md` | Updated current contract/navigation or explicit historical supersession |

Machine inventory and exact local-link results: [documentation-inventory.json](../../Board/EP-0012-combat-rebuild/evidence/documentation-inventory.json). Graph counts and source/artifact hash validation are in [verification.json](../../src/graphify-out/verification.json); exclusions prevent circular result hashes. Token usage is null/unmeasured, not zero-cost. Historical guided p99 failures remain failures of their workload; current point budgets/native assertions do not close prior FPS or human gates.


### Final graph integrity — 2026-10-21

Fresh offline export:792code +311Markdown =1103source SHA256 verified;16038nodes/50778edges/514communities. All21artifact hashes verified;0missing sources/invalid line locations/dangling edges/self-loops. Source canonicalization produced794self-loops, explicitly audited/pruned from derived graph;1112unresolved AST edges and2886reference-only symbols remain explicit limitations. Selective EP12 semantics:21inputs/3host-agent chunks,52nodes/58edges, token usage null/unmeasured. Actual CLI OwnProjectileSnapshot query succeeds (131BFS nodes, explicit700token truncation). Canonical/current Board/tool docs:1324local links and65anchors verified. JSON has full graph; HTML is an aggregated view. Generated audit/status/navigation documents are excluded from graph input hashes to avoid circular results; graph navigation does not establish runtime or human acceptance.

## US20 final impact inventory — 2026-10-21

Current package20stories/80tickets. US20 six functional fixes and the last US15-TK6 documentation extension supersede historical19/73 counts above. Updated current requirements, PointAimCombat, session/map/button/report specifications and tooling instructions. All90 canonical Markdown paths scanned for changed topics; unrelated/historical content retained. [Per-file inventory](../../Board/EP-0012-combat-rebuild/EP-0012-US-0020-bugs/evidence/documentation-impact.json) · [execution/publication/native limits](../../Board/EP-0012-combat-rebuild/EP-0012-US-0020-bugs/ExecutionStatus.md). Root README absent; AGENTS/CLAUDE shims unchanged. New offline graph builder and selective US20 semantics are reproducible; generated output is intentionally ignored. Current graph hashes/counts are recorded separately from the earlier integrity snapshot above.

### Current US20 graph integrity — 2026-10-21

Fresh offline US20 graph: 806code +319Markdown = 1125 hashed source files; 16275nodes/51378edges/529communities. All input hashes and 21 artifact hashes verified. Missing sources/invalid line locations/dangling edges/self-loops:0. Explicit limitations: 1144 unresolved AST edges excluded, 2926 reference-only nodes, 808 canonical self-loops audited/pruned. Selective US20 semantics14nodes/7edges in13source files; tokens unmeasured. Full JSON and aggregated HTML remain local ignored outputs; committed builder and semantic fragment reproduce them.
