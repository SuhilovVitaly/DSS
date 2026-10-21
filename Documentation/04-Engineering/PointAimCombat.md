# Point-aim combat — EP-0012

Implemented contract, 2026-10-21. [Authoritative requirements](../01-Requirements/EngineRequirements.md), [complete decisions and event ordering](../../Board/EP-0012-combat-rebuild/Documentation.md), [73-ticket execution/publication ledger](../../Board/EP-0012-combat-rebuild/ImplementationStatus.md). This replaces EP6–EP9 guided contact/roll/mode/automatic-PRO combat. Module armor/structure, mass-weighted hit location, critical roles, clocks, economic systems and EP10 report delivery remain applicable.

**Implementation and acceptance are separate.** Production balance is outside the dodge corridor; physical input/audio/accessibility and several native variants remain OPEN. Neither unit tests nor graph navigation establishes product acceptance.

## Ownership and commands

Engine owns motion, launch validation, authoritative projectiles, scatter/dodge RNG, damage, AI, encounter and persistence. Motion provides pure geometric calculations. Contracts carries immutable own/observed DTOs and receipts; LocalClient transports them; Client renders admitted public state and submits commands.

`PlayerCommand` retains ObjectId/ModuleId/CommandId/ClientSequence and explicit world-coordinate fields TargetWorldX/TargetWorldY; ProfileId selects a launch profile. Commands are `torpedo.launch`, `defense.launch`, `torpedo.detonate` (own ProjectileId binding through command target), `maneuver.evade` (ThreatProjectileId/EvadeSide), `engine.turnHold` (TurnDirection/Active), `combat.acknowledgeResults`. CommandId retry is idempotent. World units are100m; range/speed profile values are km and km/s. No implicit selected target or aim clamp. Invalid/nonfinite/missing payload, range, profile, module readiness, ownership and session state produce authoritative reason codes; modern UI localizes them.

Legacy `torpedo.fire/selfDestruct/setMode` and `defense.fire/enable/disable`, guided route solvers, contact chance, automatic interception/burst/overheat are retired. Reserved identifiers and source-only compatibility members remain for historical consumers. Legacy motion/roll/automatic-defense members are excluded from the public JSON contract even when populated; they are not an executable compatibility mode.

Sources: [PlayerCommand](../../src/DeepSpaceSaga.Contracts/PlayerCommand.cs), [point runtime](../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs), [Motion](../../src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj). No `CombatSim` code is executed or modified for EP12 production evidence.

## Profiles and physical outcomes

| Projectile/profile | Speed km/s | Radius km | Aim range km | Reload physical s |
|---|---:|---:|---:|---:|
| Torpedo slow |6|9|70|10 Tetrarch /7.25 Mukha|
| Torpedo medium |10|5|70|same|
| Torpedo fast |16|2.5|70|same|
| Countermissile wide |15|1|30|12.5 per launcher|
| Countermissile fast |25|0.5|30|same|

Straight flight starts at actual ship position and ends at a captured scattered point. Deadline is launch time +distance/speed, including fractional physical milliseconds. Range checks the intended aim point before scatter. Reload begins at launch; an outstanding flight does not forbid a ready launcher firing again. Zero-length shots are resolved by the deterministic scheduler. Own manual torpedo detonation uses its current position and the same area rules.

Torpedo damage200, armor piercing20%; factor k=1 at distance<=0.3R, linearly falls to0 atR. Positive scaled damage rounds away from zero; a zero-damage boundary is not a damaging hit. All ships in radius, including allies and self, can be hit. One module per affected ship is chosen by mass. Surviving positive hits disable it for8000physicalms. Armor none0/light60; module structure150 in canonical loadout. Critical bridge/engine destruction or configured destroyed fraction0.3333 ends a ship. No hull HP pool or contact success roll.

Gaussian independent x/y scatter sigma =baseScatter×aimDistance×max(0.3,1−(accuracy−15)×0.0333). Base torpedo0.02, CM0.08. Captured operator parameters and named RNG states/counters survive Save/Load. A CM blast removes every torpedo/CM in its radius, any side; it does not damage ships. Removed missiles never chain-explode. Same-time CM resolution precedes torpedo blasts.

Catalog sources: [combat rules](../../src/DeepSpaceSaga.Client/Data/Combat/combat-rules.json), [module catalog](../../src/DeepSpaceSaga.Client/Data/Modules/), [ship classes](../../src/DeepSpaceSaga.Client/Data/Ships/ship-classes.json). Profiles live in module launch specifications, not fire-modes.json (deleted).

## Scheduling, time, maneuver and AI

Motion tick100ms bounds integration; analytical deadlines remain exact inside it. At a shared time: CM blasts ordinal, torpedo blasts ordinal, deaths/encounter transition, queued player launch/detonate/evade, NPC dodge then launch, motion update. Intermediate integration boundaries must not drain endpoint player commands early. Player actions at the actual endpoint precede NPC decisions; scheduled lethal blasts precede both. Defeat freezes physics/AI and rejects new actions/save.

x1 means one physical second per four real seconds. Calendar, motion and presentation remain separate; pause advances none of combat/AI/reload. Active combat permits only paused/x1 speeds, with higher-speed requests rejected. Encounter Idle/Active/Won/Lost is separate from SessionOutcome Running/Defeated. Won uses nonmodal statistics and explicit acknowledgement; Lost routes to DefeatScreen. Duration statistics store physical time; real x1 display multiplies by4.

Evade selects an observed projectile and left/right relative to its flight vector. Engine steers toward perpendicular escape and full speed until closest-approach time+0.5 physical s, respecting captured angular-speed and acceleration limits. It then steers and decelerates back to the base course under the same limits. EvasionSteeringSnapshot records both the escape deadline and return completion; save/load and conditional Motion sampling share this continuation. Held arrow rotation and Shift acceleration are explicit commands. Disabled/destroyed engines cannot maneuver.

Tetrarch dedicated engine: max5km/s, acceleration600m/s², turn20°/s, cruise3km/s. Strizh:8km/s,1000m/s²,24°/s, cruise6km/s. Shared basic stays6km/s,400m/s²,4°/s. Mukha orbit25±8km, flank−35/0/+35. Reaction=max(200,1400−30×pilotSkill)ms physical; dodge chance from combat-rules.json npc.dodgeChance (default0.1), once per threat; launch chooses expected blast coverage>=0.3 and friendly clearanceR+1km. AI may read authoritative world; player forecast may not.

## Privacy and presentation

Own projectile DTO contains captured endpoint/deadline/radius; foreign observation contains position/vector/speed/profile only, with no future route, endpoint/deadline, target, damage or RNG. Enemy ships expose public condition badges, never module health/skills. Reports, journal, save response, metrics and forecasts honor the same boundary. CombatMetrics schema2 records own/observed point facts and no legacy chance/hidden target fields.

Display-admitted frames alone feed observed NPC motion history: two equal physical one-second turns confirm a trend, otherwise project the current course straight. Pause adds no observation. Do not expose raw AI turn controls or an undisplayed future snapshot. Own5s movement forecast and launch previews are conditional, not predictions of future AI decisions. Foreign extended flight trace does not imply knowledge of detonation.

Client uses data-driven combat visuals/input and RU/EN Localization. Own flight shows trace, true physical R and0.3R core, real countdown; foreign shows extended trace/profile. A minimum6px marker is independent of the physical radius. Persistent70/30km rings have per-module session toggles. F fits combat. Q/W or module buttons aim;1–3/wheel choose profile without changing speed, click launches, Esc/right click cancels; reload permits preview only. Torpedo preview shows scatter/real ETA/observed linear target shadows and friendly confirmation. CM preview snaps within12px to observed traces; Alt frees aim; danger/frontier/arrival/margin/late/unreachable are geometric estimates. A/D preview/release evasion, Tab selects threat; threat panel uses time-to-boundary and R+3km filter.

Player module tiles show armor/structure/disabled/destroyed/critical state and N/3 losses; enemy numeric damage is hidden. Current weapon panels show operator/accuracy/profile/scatter/reload and manual detonation. Journal/reports use launch/blast/interception/hit facts, not chance or roll. Native fixes keep visible projectile labels within viewport and choose a sufficiently tall free region for Won summary. At1280×720/1.5 the map can be cramped; usability is not accepted from screenshots.

## Scenarios and save contract

CombatMukha is the canonical Tetrarch vs3Mukha case: player51t, each Mukha7t, crew skills20. Mathematical input (x,y,angle) is converted to world (10x,-10y,(90-angle) mod360): player course0�, enemies at(-400,-750)/(0,-850)/(400,-750), course180�, with flank offsets-35/0/+35. Strizh mass is5000kg. CombatMukhaSingle is the same scenario retaining SPC-MUKHA-01 only. CombatShmel is removed from source/discovery/deployment. PlayerShipOnly retains generated-map geometry/crew and adopts current point launcher/Tetrarch engine with AI25/8/35. Default500 normalized noncombat content hash remains unchanged (only the retired autoDefenseEnabled flag was removed); production deployment/discovery is tested separately from source presence.

SaveFormatVersion18 writes versioned point flights, captured parameters, scatter/dodge RNG/counters, launcher reload/identities, evasion/AI/encounter/statistics and fractional clock remainder. Real Save17 files are rejected atomically without migration or current-world/file replacement; NewGame scenario version0 is a separate input. Full validation precedes installation. Restored reports keep delivery baseline without replaying old UI history. Defeated sessions cannot overwrite a save slot. [Save format source](../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs).

Review corrections exclude the balance policy and its rerun. The balance measurements below are historical, before the corrected geometry and bounded evasion; they do not establish current balance acceptance. Generated src/graphify-out artifacts stay local and ignored by Git.

## Reproduction and measured gates

Use .NET SDK8.0.100 or a later installed stable SDK (global.json latestMajor roll-forward), targetnet8.0. Build `dotnet build DeepSpaceSaga.sln --no-restore`. Focused correctness tests and each corpus process are hard-capped at300000ms with process-tree termination. Whole-suite Engine discovery is partitioned by exact test identity, not display-name sets (four collisions exist).

| Gate | Evidence/result |
|---|---|
| Complete Engine at scheduler gate |2331/2331 distinct TestIds PASS in25 bounded processes; [exact corpus](../../Board/EP-0012-combat-rebuild/evidence/us0018-tk0005-results.json)|
| Engine after shared balance harness |2374 covered:2236 newly executed +138 source-unchanged long cases; no claim of rerunning138. [Evidence](../../Board/EP-0012-combat-rebuild/evidence/us0014-tk0001-results.json)|
| Client after native fixes |2225/2225 PASS, solution build0warnings/errors; scoped format/diff PASS. [Evidence](../../Board/EP-0012-combat-rebuild/evidence/us0014-tk0003-results.json)|
| Production balance |2000 complete battles,37593 actual commands all Executed. Dodge966/1000=96.6%, median4.8267realmin, P907.4933min: **FAIL corridors72–88% and<=5.5min**. Static59/1000=5.9% meets<=10%. [Protocol/raw traces](../../Board/EP-0012-combat-rebuild/evidence/balance.md)|
| Analyzer |16self-checks, full2000 input consistency and50 fresh production seeds match exact commands/outcomes; exit2 correctly flags outside corridor. [CLI](../../tools/DeepSpaceSaga.CombatAnalyzer/README.md)|
| Native |24/24 matrix +clean-flight actual GPU runs PASS for stated assertions; [every UI criterion and NOT RUN boundary](../../Board/EP-0012-combat-rebuild/evidence/native.md)|

The production input policy is fixed before measurement, observes public state, reacts after500ms, and does not tune catalogs/AI. Reference simulator tapes were not supplied; no bitwise-equivalence or causal proof is claimed. Do not silently retune to force a corridor. Author balance decision and physical input/audio/accessibility/manual gameplay remain OPEN. Historical guided forecast p99 failures (including2.227/2.378ms) remain failures of that workload; current point budgets do not retroactively pass them. Independent review is not implied by implementer self-review. Graphify is navigation evidence only and is rebuilt after final documentation.
