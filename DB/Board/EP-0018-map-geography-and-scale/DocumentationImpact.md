# Влияние EP-0018 на документацию

Инвентаризация по итоговому source diff c43e8af5..HEAD и tracked Markdown всего
репозитория (2026-11-08).59current Documentation,897canonical Board,850VData copies,
10other tracked Markdown. Числа отражают checkout до добавления этого inventory.
VData — неканонические копии, не источник задач; изменения Board только в DB/Board.
Существующего root README нет. История приёмки — Board/artifacts; curated docs Markdown-only.

## Current Documentation: полный перечень

| Документ (от DB/Documentation) | Изменение/основание | Состояние и проверка |
|---|---|---|
| `00-Process/AGENTS.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `00-Process/CLAUDE.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `00-Process/DocumentationSystem.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `01-Requirements/EngineRequirements.md` | Engine-owned frozen geography/min/nearest; units/clock unchanged | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `01-Requirements/FirstReleaseRequirements.md` | v2 world/current v1Load summary | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `02-FirstRelease/Mechanics/CargoHold.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/CommandPanels.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/CrewAndHabitation.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/Docking.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/ElectricityAndEnergyCells.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/Fuel.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/IceMining.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/Money.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/SolarSystemMapConcept.md` | new scale/geometry/legacy contract and navigation | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `02-FirstRelease/Mechanics/StationInventory.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md` | camera levels, Home/End/local action | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `02-FirstRelease/Mechanics/TetrarchClass.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Mechanics/Trading.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Contracts.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Dialog.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Finance.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/GameMenu.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/GameSession.md` | navigation + frozen ruler/current ETA | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `02-FirstRelease/Screens/Hire.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Load.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/MainMenu.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Save.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/ScenarioSelect.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/ScreenCatalog.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Settings.md` | renderer UI2 versus unchanged menu; fallback settings | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `02-FirstRelease/Screens/Ship.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Station.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/TacticalBattle.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/TacticalBattleSetup.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `02-FirstRelease/Screens/Trade.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `03-Design/DialoguePortraits.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `03-Design/PortraitGenerator.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `03-Design/PortraitGeneratorM4.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `03-Design/PortraitGeneratorW4.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `03-Design/TacticalButtons/STYLE.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `03-Design/TacticalMapSpecification.md` | Local constraints/detail/hysteresis/orbits/labels/navigation/fit limitations | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `04-Engineering/AiMapEnvironment.md` | placement after final station world; separate AI horizon | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `04-Engineering/ApproachRoutes.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/DebugTuning.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/DebugTuningPanel.md` | current Vmax does not change frozen world/ruler | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `04-Engineering/ItemCatalogSchema.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/ManualManeuver.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/Modding.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/NpcObservedTrajectories.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/SaveFormat.md` | Save20 v1/v2 coherence/materializedLoad/atomic rejection | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `04-Engineering/TacticalBattle.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/TacticalBattleSkins.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `04-Engineering/TradingAndSolarSystem.md` | owning v2 pipeline/formula/graph/resources/starts/AI horizon | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `06-Tooling/CarrierWingSim.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `06-Tooling/DeepSpaceSaga.PortraitAssets.md` | No owning contract change: different feature/process or existing invariant preserved | reviewed; unchanged; no new runtime/acceptance promise |
| `06-Tooling/GameSessionScreenUI.md` | map settings/localization/source ownership | updated TK9.1; source/contracts review; contracts-audit.json links PASS |
| `06-Tooling/Graphify.md` | offline graph scope and current Board | pending TK9.3; final links/graph audit |
| `06-Tooling/GraphifyRebuild.md` | reproducible EP18 scope/output/semantic/hash workflow | pending TK9.3; final links/graph audit |
| `README.md` | navigation to current geography/scale/tool guide | pending TK9.3; final links/graph audit |

## Другие tracked Markdown и meaningful comments

| Документ/группа | Связь с diff | Решение/проверка |
|---|---|---|
| AGENTS.md, CLAUDE.md |Routing/ownership/process unchanged |Reviewed, no replacement workflow created |
| src/DeepSpaceSaga.Client/Images/Persons/W4/Portraits/character-generator/SKILL.md; references/assembly-contract.md |Portrait asset geometry unrelated |Unchanged |
| src/DeepSpaceSaga.Client/Skins/README.md |Tactical battle skins unaffected by world map |Unchanged |
| tests/DeepSpaceSaga.Engine.Tests/Fixtures/Saves/README.md |Historical Save18/19 fixture provenance unchanged |Reviewed; v1/v2 live-file tests use own slots, no fixture rewrite |
| tools/DeepSpaceSaga.Performance/README.md |New strict v2 flags, evidence schema/matrix/native/cleanup |Pending TK9.3 exact commands and versioned legacy modes |
| tools/DeepSpaceSaga.Performance/EP0013.md |Historical debug tuning native workflow |Unchanged; no historical outcome overwritten |
| tools/HexWingSimulator/README.md; tools/tactical-battle/README.md |Separate simulator/combat tools |Unchanged |
| GameSessionUiScaleTests.cs summary comment |Now accepts UI2; old100/120/150 list incomplete |Pending TK9.3 documentation-only comment update |
| Other touched production/test comments |Frozen scale/version/lane/resource ownership descriptions match final implementation |Reviewed; no contradictory current comment found |

## Board/history/navigation

| Документ/группа | Изменение | Проверка |
|---|---|---|
| EP18 Documentation.md/ImplementationContract.md/Tickets.md + all canonical cards |Current statuses, explicit planning history, actual deviations, named tests and DAG |29tickets/38nodes;74named method references resolve; no acceptance invented |
| EP18 ImplementationStatus.md |Single current ledger: all ticket SHA/evidence/criteria/gates |27published SHA ancestors verified; own/final doc SHA deferred to actual receipt |
| EP18 DocumentationImpact.md |This whole-repository inventory |Current owning docs and outside-doc guides included |
| EP0002/EP0003/EP0004 Documentation.md |Dated supersession/integration links to EP18; historical results preserved |Pending TK9.3 |
| Other canonical Board records |Unrelated or historical acceptance; not current geography spec |Unchanged |
| VData/Board and VData/Documentation850tracked copies |Not canonical; supplied process forbids treating copies as tasks |Unchanged, not used as authority |

## Source-to-document mapping

Contracts StationClusterMap + Scenario version/placement/validation/trading/resource/voyage changes
→ EngineRequirements/TradingAndSolarSystem/SaveFormat/AiMapEnvironment and solar concept.
Client camera/policy/detail/labels/orbits/navigation/estimates/settings/locale changes
→ TacticalMapSpecification/GameSession/TacticalMapAndManeuvering/Settings/GameSessionScreenUI/DebugTuningPanel.
Corpus and raster/native tools → tools Performance README + Board evidence/coverage;
no JSON/TRX/history in Documentation. Motion guard findings are unchanged tactical source,
recorded in Board/artifacts instead of changing unrelated combat contracts.

Offline graph must be rebuilt after final current text/source: canonical EP18 included,
fresh semantic source hashes, finite source locations and explicit coverage limits.
Historical src/graphify-out is not overwritten; graph/cache/semantic outputs only in
D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261108-graph/.
Final graph/links/cleanup/publication checks belong to TK9.3 and its actual receipt.
