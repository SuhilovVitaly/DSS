# US20 execution — 2026-10-21

All six technical fixes are implemented and published. Final project documentation is US15-TK0006, executed last. Overall acceptance remains OPEN: native scenario31/33, physical input/hearing and human visual acceptance are not established.

Branch: `codex/ep-0012-combat-rebuild`; remote: `origin` (`https://github.com/SuhilovVitaly/DSS`). Upstream and each published SHA verified using `git ls-remote`; no force push/history rewrite. The initial user-authored requirements and six draft tickets were preserved and published as the task baseline.

## Delivered behavior

1. Selected Orbit path follows shared steering and arrival to the selected point, then extends to the viewport edge; no invented straight turn.
2. Right ship card restores image/fields. Left groups show all own module health/disable/critical/destruction/hit details, with wrapping/scrolling; related Approach and short-window regressions corrected.
3. Foreign missiles have only marker, bounded speed-scaled rear tail and dark display-admitted past trail; no forward geometry/labels.
4. Successful torpedo send immediately exits aim; pending/error still lives in its weapon panel, invalid/friendly-confirmation input retains aim.
5. Authoritative reload deadline produces one journal fact and reliable report, scene-admitted once-only readiness cue; old enum IDs/save18 continuation retained and NPC reload hidden.
6. Combat offers pause/1/2/5/10; exact physical multipliers, allowed-set validation, Save/Load, scene controls and ETA covered.

## Publication

| Ticket | Initial implementation | Additional fixes/diagnostics (oldest to newest) | Push |
|---|---|---|---|
| EP-0012-US-0020-TK-0001 | `faafbf0a8664bdee976e491c2eee110a4f8199c0` |  | verified origin upstream |
| EP-0012-US-0020-TK-0002 | `ad6df12f51991ab6d8e2416f254dba11d9d00ac2` | `10bac8e00ade8bec857c2244665809c7cc265e3e`, `34ba1d41e00c4c76a93de3611d0fe0028b4117f2`, `b99e6e8f5bc7fa4d173be809b6546dc7f49bdd3f` | verified origin upstream |
| EP-0012-US-0020-TK-0003 | `7c4720d5cd9e8a686cc0f35d7fc24eada9da7282` |  | verified origin upstream |
| EP-0012-US-0020-TK-0004 | `03632713178269bcc0c86a27535f4f831719ecbb` |  | verified origin upstream |
| EP-0012-US-0020-TK-0005 | `1686d59522f4dc7b0ec5ffa13e712c5696c96fa0` | `747ea814f2bfa296393e1262c23179fa8f963020`, `5bef37c6ed4c7696aea62814b447b40a3026fa25` | verified origin upstream |
| EP-0012-US-0020-TK-0006 | `e134a2ca0f14ccc90da454a1273bc4465ed7562f` |  | verified origin upstream |

US15-TK0006 final documentation SHA is resolved after creating its commit: `git log --format="%H %s" --grep="EP-0012-US-0015-TK-0006"`. A commit cannot contain its own SHA. The final chat reports the actual published SHA. Full functional hash/file registry: [publication-and-scope.json](evidence/publication-and-scope.json).

## Automated validation

Every dotnet process had a300000ms process-tree timeout. Executed from DSS with `--no-restore`, sandbox-restricted .NET checks rerun outside sandbox.

| Project/check | Actual final result |
|---|---|
| Full Client |2256/2256 PASS,87s|
| Full Contracts |261/261 PASS|
| Engine point/clock/report/save regression (balance excluded) |404/404 PASS|
| TK1 navigation/viewport regression |41/41 PASS|
| TK2 panels/localization and later regression fixes |19/19,13/13,35/35,36/36 PASS at their respective checkpoints|
| TK3 map/threat regression |87/87 PASS|
| TK4 launch/complete UI flow regression |56/56 PASS|
| TK5 Engine + Save18/report |52/52 PASS|
| TK5 Client + reports/audio/localization |90/90 PASS|
| TK6 clock/encounter/named speed |59/59 PASS|
| TK6 Client speed/localization |30/30 PASS|
| Owning projects/dependencies |built successfully by tests|
| Scoped production dotnet format + diff check |PASS|

Reproduce full Client/Contracts with `dotnet test tests/<project>/<project>.csproj --no-restore --filter FullyQualifiedName~<project>`. Engine filter: `(FullyQualifiedName~PointCombat&FullyQualifiedName!~US0014)|FullyQualifiedName~SimulationClockTests|FullyQualifiedName~CombatReport|FullyQualifiedName~PhaseThreeSaveContinuationTests`. Ticket-specific commands/results are recorded in each card. No full Engine or balance rerun was substituted for these checks.

Earlier failed full Client runs were not PASS: first2254/2256 (Approach access/report timing), then2254/2256 (short-panel fallback), then2255/2256 (reload delivery timeout). Full final rerun2256/2256 passed after retained diagnostic capture. An intermittent reload integration timeout was observed; a production report-loss defect was not established. Diagnostic assertions preserve actual engine/admitted state for any recurrence. [Structured validation](evidence/validation-summary.json).

## Native and remaining acceptance

Actual Intel Arc140V OpenGL3.3,1920×1080/RU managed callback scenario executed. New US20 checks for ship card/left modules, torpedo aim exit, no forward foreign geometry and combat allowed speeds passed. Overall31/33: `actualCountermissileConfirmed` and `counterDetonationActualJournal` failed; actual preview marks this test aim unreachable. Production range/readiness checks were not weakened. Initial raw report and final report/captures retained. [Initial report](evidence/native-us20.json) · [final31/33 report](evidence/native-us20-admitted.json) · [ship/modules sample](evidence/native-us20-admitted.confirmed-flights.png).

Physical keyboard/mouse, complete native US20 maneuver/zoom/resize matrix, actual2x/10x native timing/input, reload audible playback/mute and human UI acceptance: OPEN/NOT RUN. Managed callbacks and headless sound enqueue are not these gates. The native failure remains explicitly unresolved; current changes do not close historical production balance/FPS/accessibility/transport gates.

## Documentation and graph

Current contracts and related entry points updated across Documentation and Board; tools README updated. No root README exists. Root AGENTS/CLAUDE remain navigation shims. [90-document impact inventory](evidence/documentation-impact.json) · [current contract](../../../Documentation/04-Engineering/PointAimCombat.md) · [project documentation inventory](../../../Documentation/04-Engineering/PointCombatDocumentation.md).

Reproducible graph builder `tools/graphify/rebuild.py` uses installed graphifyy offline, fresh code AST/Markdown structure plus selective14-node US20 semantics (13source files; one host analysis, tokens unmeasured), retaining explicit EP10 mechanisms. Older EP12 semantic fragment was replaced rather than rehashed as though re-extracted. Graph output is local/ignored under src/graphify-out; committed semantic input and integrity evidence make scope explicit. Graph navigation is not runtime/product acceptance.

Fresh offline US20 graph: 806code +319Markdown = 1125 hashed source files; 16275nodes/51378edges/529communities. All input hashes and 21 artifact hashes verified. Missing sources/invalid line locations/dangling edges/self-loops:0. Explicit limitations: 1144 unresolved AST edges excluded, 2926 reference-only nodes, 808 canonical self-loops audited/pruned. Selective US20 semantics14nodes/7edges in13source files; tokens unmeasured. Full JSON and aggregated HTML remain local ignored outputs; committed builder and semantic fragment reproduce them.

Documentation link audit1435paths,0missing (fragment anchors not validated). Board20stories/80tickets; all80ticket dependencies acyclic. `git diff --check` passed. [Graph integrity](evidence/graph-verification.json) · [links](evidence/link-check.json) · [Board structure](evidence/board-check.json).
