# EP-0002 implementation evidence

User authorization: implement every story and ticket, fix issues found by story/epic reviews, change project files and tests as needed, commit and push every ticket. Destination explicitly confirmed: origin (https://github.com/SuhilovVitaly/DSS), base-fight.

Final outcome: all 21 tickets and eight story reviews are delivered. Final solution test run: 3113/3113 passed; 4800-world correctness corpus; 18 native host cases / 10800 measured frames, all passing the 80 FPS p99 criterion. Reviews were performed by the implementing agent. Story notes below are chronological; deferred native checks and subsequent integration corrections are resolved in US-0008 and the final epic review.

## US-0001 — seeded system start

| Ticket | Commit | Observed validation |
|---|---|---|
| TK-0001 | 09d55c7 | Contracts: 105/105; scoped format; diff check |
| TK-0002 | 902c571 | Engine: 1155/1155; strict config and real loader roundtrip |
| TK-0003 | 3c1b395 | Engine: 1161/1161; final focused bootstrap: 7/7; 100 seeds, independently calculated RNG vector, Vmax/docking offsets, atomic failure, map save roundtrip |
| TK-0004 | 7f4d590 | Client: 1570/1570; MasterSeed: 9/9 after adapting legacy fixture cardinality; explicit legacy manifests removed only in synthetic fixtures |
| TK-0005 | 93976e0 | Client: 1574/1574; final focused view: 5/5; 1280x720 and 1920x1080 fits and selection; real Default raster visually inspected |

Commands: `dotnet test <matching-project> --no-restore`, focused filters matching ticket test classes, `dotnet format <matching-project> --verify-no-changes --no-restore --include <changed-files>`, `git diff --check`. Tests build affected projects and references with warnings as errors. All five ticket commits pushed.

Story review (same implementing agent, not independent): compared contracts, generation, configuration, scenario integration, save projection and rendering against all three story acceptance criteria. Found P2: pre-existing Sun/Planet in a source scenario could exceed the generated celestial counts. Fixed by rejecting these inputs before placement; regression checks preserve the previous live save state on failure. No remaining confirmed US-0001 findings after the correction. Orbital runtime, other scenarios and playable belt asteroids remain the subsequent stories' work.

The screenshot was produced by the real screen's Skia raster rendering path, not a native presented frame. Native/GPU and final performance evidence remain US-0008; no 80 FPS claim is made.

## US-0002 — coherent orbital world

| Ticket | Commit | Observed validation |
|---|---|---|
| TK-0001 | 73f39d1 | Motion: 141/141; independent ellipse quarter positions/derivatives, both directions, Int64 epochs, non-divisible periods, compact phase, split time, linear/Approach regression |
| TK-0002 | d628830 | Engine: 1169/1169; focused runtime 7/7; Speed0-4, independent calendar hour, docking offsets, free flight, split advance and actual save-loader continuation |
| TK-0003 | 392ae4c | Client: 1582/1582; render poses, pause/resume, docked prediction, real Default raster and epoch-zero trail regression; scoped format and diff checks |

Story review (same agent): checked clock domains, Int128 remainder before floating-point conversion, derivative units/headings, all orbit prediction paths, immutable snapshot metadata, docking binding and release, persistence and client visual reconciliation. The full Client run exposed pre-zero trail bootstrap; TK-0003 fixes it only for orbital history. No remaining confirmed US-0002 findings. Current scenario stations acquire generated orbital elements in US-0004; runtime support is already exercised by station fixtures. No native/GPU evidence is claimed.

## US-0003 — playable asteroid belts

| Ticket | Commit | Observed validation |
|---|---|---|
| TK-0001 | 91e9639 | Engine 1172/1172; deterministic grouped belt entities, annulus containment including very large epochs, unchanged temporary asteroids and independent decoration settings |
| TK-0002 | 4d683dc | Full solution passed: Contracts 105, Motion 141, Engine 1172, Client 1584; cached detail/LOD/selection and cache invalidation; scoped format and diff clean |

Resolved planning gap: DecorationSamplesPerBelt existed in generation config but the planned snapshot could not carry it to Client. Added optional camelCase `BeltMapData.decorationSamples` with compatible default 2048. Changing this field changes only decorative sampling; tests compare unchanged authoritative objects, orbits and decoration seeds. Rendering caps samples at 65536 per belt and uses one quarter at overview LOD. Cache keys include full belt geometry, seed, count and LOD; obsolete map entries are removed. No decorative snapshot entities are created.

Story review (same agent): inspected stream isolation, IDs/case collisions, bounded placement/clearance, masses/compositions, orbital containment, temporary-object preservation, geometry bounds, rendering order, selection and cache lifetime. No confirmed outstanding story findings. Native presentation/performance remains US-0008.
## US-0004 — preserved scenario starts

| Ticket | Commit | Observed validation |
|---|---|---|
| TK-0001 | 2d885d8 | Scenario translation matrix 10/10, including real resource-world save/load after motion; Engine suite before final regression 1181/1181 |
| TK-0002 | de29f4a | Engine 1182/1182, Client 1586/1586, Contracts 105/105, Motion 141/141; format and diff checks |

Current scenario discovery includes PlayerShipOnly, added since planning; it is covered alongside the five planned starts. Raw stationary templates remain explicit fixtures for legacy economy/dialogue tests. Production New Game tests exercise generated orbital starts and compare docked velocity with the parent's authoritative tangent.

Story review (same agent): inspected immutable translation, docking offset normalization, relative geometry, configured start radius, resource-field rigid rotation, save placement validation at original epoch, all-scenario discovery and legacy Load isolation. Confirmed that runtime movers are excluded only from saved initial trading placement checks; resource/market manifests still validate. No outstanding confirmed US-0004 findings. Visiting a moving station remains US-0005.
## US-0005 — orbital station visit

| Ticket | Commit | Observed validation |
|---|---|---|
| TK-0001 | 0d3a984 | Engine 1187/1187; seeds 1, 2, 42 reach dock range within max(2 * straight time, 10 calendar days), constant Approach speed, live tangent completion, cancel/repeat and unreachable fallback |
| TK-0002 | dc7de93 | Engine 1192/1192 before added abort regression; final focused docking 6/6 including abort and duplicate dialogue commands; scoped formatting and diff checks |

A real command-driven visit (no position reset during the journey) runs Approach, separate speed/course synchronization, the paid docking dialogue and Undock on all three seeds. Docked motion preserves parent offset; release preserves exact pose, speed and heading and continues linearly. Station travel retains its existing one calendar hour / 12 physical seconds advance.

Confirmed defects fixed: completed MaintainCourse left its cycle permanently busy; first free-flight docking never initialized the docked voyage state, hiding departure options. Added regression coverage for both. Dock validation now uses circular heading difference with the existing epsilon. The detached dialogue transaction installs orbital binding with docking and still rolls back fees on validation failure.

Story review (same agent): checked legacy captured-target semantics, unchanged ApproachRoute/planner version/speed, completion timestamps, cancellation, heading seam, transactional revalidation, dialogue replay/abort, docked travel and release. No outstanding confirmed US-0005 findings. No new orbital interception API was introduced.
## US-0006 — known system map

| Ticket | Commit | Observed validation |
|---|---|---|
| TK-0001 | 39eee6a | Engine 1196/1196; spatial identities exposed without mutating IsKnown, detailed/private fields masked, remote quote rejected, resource composition remains unknown, legacy masking retained |
| TK-0002 | afa7152 | Client 1591/1591; all six starts at 1280x720 and 1920x1080, UI 100/120/150%, free-viewport system/belt bounds, orbit toggle and protected selected labels |

Added localized Orbits and Next belt actions to the existing toolbar. Orbit visibility is Client-local; snapshots and world distances remain unchanged. Active, selected and player objects are excluded from clustering and retain priority rendering.

Story review (same agent): inspected disclosure boundaries, scan/market independence, snapshot-only rendering, orbit toggle, belt cycling, missing belt handling, viewport fitting, UI scaling, input routing and important marker/label order. No outstanding confirmed US-0006 findings. Native presented-frame validation remains US-0008.
## US-0007 — resume generated system

| Ticket | Commit | Observed validation |
|---|---|---|
| TK-0001 | 4bf4006 | Engine 1206/1206; six scenarios, independent calendar/motion epochs, active Approach, real docking, legacy v11, atomic malformed-map rejection |
| TK-0002 | 8c3ec6a | Local file roundtrip 9/9; full solution Contracts 105, Motion 141, Engine 1215, Client 1591 all passed; format and diff checks |

Save writer version is 12 (branch previously 11). Parent station orbital elements are saved; a docked ship's fixed offset binding is reconstructed from its validated parent instead of introducing a duplicate map orbit. Loader additionally rejects invalid counts, missing Sun/planet/station orbit, noncentral offsets, overlapping radial corridors, bad references and out-of-bounds planet extents.

Transport tests use the real SaveAsync file and production CreateFromSaveFile factory. A controlled clock prevents wall-time races; first paused and continued snapshots preserve all identities, orbits, geometry and motion. Invalid version, parent and JSON leave the original bytes untouched.

Story review (same agent): checked reader/writer version gate, optional legacy fields, no generation on Load, all preflight validation before live mutation, both time domains, parent binding reconstruction, active-route continuation, file atomicity and paused transport behavior. No outstanding confirmed US-0007 findings.

## US-0008 — correctness and performance evidence

TK-0001: b31b1c2. Boundary corpus covers 4800 worlds: seeds 1..100, all six scenarios, 3/7 planets, 2/5 belts and 50/75 starting days. All 49 test rows passed. Full solution after fixes: Contracts 105, Motion 141, Engine 1264, Client 1591. Corpus exposed max-density seed 36 exhaustion: placement now retries each corridor before restarting the entire attempt, retaining bounded budgets. Configuration permits finite subranges within 50..75 to make the required boundary corpus possible; shipped 50..75 remains unchanged. Generated save data is resolved, so previously saved worlds do not regenerate.

TK-0002 initial measurement commands (Release, sequential runs, source b31b1c2; repository cwd, before the asset-directory correction; superseded below):

    dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/solar-performance-min.json --solar-map --seeds 1:100 --scenarios all --config min
    dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/solar-performance-max.json --solar-map --seeds 1:100 --scenarios all --config max

Both measurement reports completed successfully, 600 generated worlds each. First seed per scenario is rendered separately in System and belt views, 120 warmup / 600 measured frames at 1920x1080. CPU/Skia raster maximum p99 across views: min 6.3785 ms, max 6.6625 ms. Largest saves: 950025 / 1132976 UTF-8 bytes. Maximum generation time: 67.0542 / 67.1579 ms (includes initial JIT); maximum snapshot sample: 15.8739 / 11.5505 ms. These are absolute observations, not comparisons against a historical baseline. GPU/presentation status remains not-measured here.

Tooling tests: ReportHasReproductionAndBackend and InvalidConfigReturnsFailure passed 2/2; the latter runs the CLI process and verifies exit code 1. The new test project is included in the solution.

TK-0003 native protocol: Release production SkiaWindow with live LocalGameSessionConnection, Default_500 seed 1, min=3 planets/2 belts/50 days, max=7/5/75, 2048 decoration samples and 24 playable asteroids per belt. System fit, first-belt fit and selected-station view at UI 100/120/150%; 100% uses 1280x720, 120/150% use 1920x1080. Warmup 120 frames, measurement 600 frames per case. Actual UI speed buttons exercise Speed1 -> Speed0 -> Speed1 before measurement. The script drives the native window; the agent inspects captured frames. This is scripted native acceptance with visual inspection, not a human-operated play session.

Hardware: Intel Arc 140V (reported 16GB), OpenGL 3.3.0 driver build 32.0.101.8860; monitor reports 100 Hz, VSync on. CPU: Intel Core Ultra 7 258V (host Win32_Processor), Intel64 Family 6 Model 189 Stepping 1, 8 logical processors; .NET 8.0.26, Windows 10.0.26200. Native reports include assembly module ID and source commit (8e952b6 plus TK-0003 diagnostics). Swap completion intervals measure driver/display wait as part of presentation; they are not GPU execution or physical scanout measurements.

Initial exploratory launches used the repository working directory and missed relative UI assets. They were discarded as acceptance evidence. The native runner now sets the client asset directory and loads production map/combat settings before constructing the screen.

Restricted-environment diagnostic results (superseded by host acceptance below): milliseconds; every case reports targetVerdict=failed. All reports observed both Speed0 and Speed1, and retained SPC-0002 selection.

| Configuration / view / UI | Frame p50 | p95 | p99 | CPU p99 | Swap p99 | Mean FPS |
|---|---:|---:|---:|---:|---:|---:|
| max-belt-1.2 | 38.54 | 46.23 | 49.98 | 5.91 | 45.76 | 26.21 |
| max-belt-1.5 | 39.53 | 47.99 | 50.30 | 6.89 | 45.74 | 25.50 |
| max-belt-1 | 37.84 | 46.18 | 50.00 | 6.60 | 45.75 | 26.36 |
| max-selected-1.2 | 38.91 | 48.22 | 52.16 | 9.55 | 44.77 | 25.78 |
| max-selected-1.5 | 39.89 | 50.49 | 53.44 | 11.17 | 43.96 | 24.97 |
| max-selected-1 | 38.70 | 46.09 | 50.02 | 6.23 | 45.12 | 26.18 |
| max-system-1.2 | 39.38 | 48.69 | 50.49 | 9.91 | 44.37 | 25.70 |
| max-system-1.5 | 35.88 | 47.46 | 50.10 | 6.32 | 45.31 | 26.74 |
| max-system-1 | 39.12 | 48.03 | 50.19 | 7.16 | 45.46 | 25.76 |
| min-belt-1.2 | 39.07 | 47.70 | 50.15 | 7.03 | 45.65 | 25.71 |
| min-belt-1.5 | 36.66 | 48.11 | 50.10 | 7.44 | 45.83 | 26.18 |
| min-belt-1 | 36.35 | 46.91 | 49.86 | 6.84 | 45.54 | 26.51 |
| min-selected-1.2 | 39.86 | 49.90 | 51.89 | 11.60 | 44.70 | 25.10 |
| min-selected-1.5 | 39.84 | 50.75 | 54.12 | 11.47 | 44.75 | 25.16 |
| min-selected-1 | 38.93 | 47.85 | 50.26 | 7.04 | 45.54 | 25.83 |
| min-system-1.2 | 39.02 | 46.17 | 50.17 | 7.40 | 44.74 | 25.92 |
| min-system-1.5 | 38.37 | 47.96 | 49.93 | 6.96 | 45.40 | 26.05 |
| min-system-1 | 36.07 | 47.58 | 48.89 | 5.76 | 45.16 | 26.76 |

The restricted-environment timing was isolated with fresh sequential empty-window controls on the same GPU, VSync enabled, 1280x720, 12 seconds including 3 seconds warmup. Restricted: 248 frames, p50/p95/p99 35.30/44.40/45.89 ms, maximum swap 49.93 ms. Host (outside restricted execution): 901 frames, 9.96/10.66/11.13 ms, maximum swap 11.50 ms. Therefore final acceptance uses host runs; the restricted values above are retained as diagnostic evidence, not as the game's standalone cadence.

Visual inspection exposed clusters painting over selected plaques at 1280x720/max. Moved clusters before individual markers/plaques. A real generated-world raster regression compared plaque pixels after the label pass with the end of map drawing: it failed before the fix. Source assets, celestial geometry, orbit/belt layers, station selection and pause/resume were inspected in representative min/max captures at all three UI scales.

Final host native acceptance, after the cluster ordering fix (same hardware/window/seed/settings; TK-0003 plus final integration fixes):

| Configuration / view / UI | Frame p50 | p95 | p99 | CPU p99 | Swap p99 | Mean FPS |
|---|---:|---:|---:|---:|---:|---:|
| max-belt-1.2 | 9.98 | 10.44 | 10.61 | 6.29 | 7.10 | 100.06 |
| max-belt-1.5 | 9.97 | 10.45 | 10.62 | 4.28 | 7.25 | 100.11 |
| max-belt-1 | 10.00 | 10.55 | 11.31 | 4.26 | 8.11 | 100.09 |
| max-selected-1.2 | 10.02 | 10.35 | 10.46 | 7.34 | 4.32 | 99.96 |
| max-selected-1.5 | 10.00 | 10.38 | 10.49 | 9.83 | 2.76 | 99.71 |
| max-selected-1 | 10.02 | 10.44 | 10.54 | 5.02 | 6.73 | 99.83 |
| max-system-1.2 | 10.00 | 10.44 | 10.58 | 7.42 | 5.94 | 100.09 |
| max-system-1.5 | 9.99 | 10.46 | 10.68 | 4.99 | 6.71 | 99.89 |
| max-system-1 | 9.99 | 10.46 | 10.63 | 4.50 | 7.20 | 100.05 |
| min-belt-1.2 | 10.00 | 10.42 | 10.62 | 7.40 | 6.67 | 99.78 |
| min-belt-1.5 | 10.02 | 10.47 | 10.76 | 6.52 | 6.86 | 99.93 |
| min-belt-1 | 10.02 | 10.44 | 10.57 | 4.77 | 6.95 | 100.10 |
| min-selected-1.2 | 10.01 | 10.37 | 10.52 | 7.14 | 4.64 | 99.50 |
| min-selected-1.5 | 10.02 | 10.38 | 10.53 | 9.01 | 2.98 | 99.54 |
| min-selected-1 | 10.03 | 10.44 | 10.64 | 4.64 | 7.02 | 99.83 |
| min-system-1.2 | 10.02 | 10.40 | 10.54 | 7.58 | 6.43 | 99.78 |
| min-system-1.5 | 10.01 | 10.46 | 10.80 | 4.24 | 7.30 | 99.97 |
| min-system-1 | 9.97 | 10.92 | 11.38 | 4.15 | 8.39 | 100.02 |

All 18 cases passed the p99 <= 12.5 ms criterion: 10800 measured native frames in total, maximum case p99 11.379 ms. This supports the 80 FPS target on this hardware in the tested host conditions. It does not promise the same cadence inside restricted execution or on different hardware. Final host max/System/100% capture confirmed selected and player plaques remain visible above clusters.

TK-0003 validation: Client 1596/1596 passed after the cluster regression and one-ULP pirate continuation fixture correction. PresentedFrameEvidenceTests covers ideal 80 FPS, dropped frames, display-limited refresh and disabled diagnostics. Release Client/performance build: zero warnings/errors. Scoped whitespace verification for Client/window/collector, tooling and Engine changes passed; git diff --check passed.

US-0008 review: TK-0003 is bf517f6. Checked opt-in/no-IO behavior, 120/600 sample boundaries, percentile indexing, CPU versus swap separation, native context metadata, display-limit verdicts, fixed-seed reproduction, invalid CLI exit codes and visual priority. Corrected both native and raster runners' relative asset directory; raster Run restores the caller directory and reports its asset root. Tooling regressions passed 2/2 in Release after this review fix.

Fresh final CPU/raster run: same Release production code as final native acceptance, host execution, working directory src/DeepSpaceSaga.Client (now selected automatically by the CLI), seeds 1:100, all six scenarios, min and max, sequential with no concurrent builds/tests. The explicit working directory already supplied the asset behavior in these measurements; the later runner change makes that behavior independent of the caller's cwd.

| Configuration | Worlds | Maximum generation ms | Snapshot ms | Save capture + serialization ms | Largest save bytes | Worst raster p99 ms |
|---|---:|---:|---:|---:|---:|---:|
| min | 600 | 68.3373 | 13.2382 | 32.8908 | 950025 | 6.7381 |
| max | 600 | 68.9454 | 14.3107 | 32.7044 | 1132976 | 6.6684 |

CPU/raster percentiles below are milliseconds, seed 1, UI 100%, 1920x1080, 120 warmup / 600 measured frames per view. Generation/save rows use all 100 seeds per scenario. These are absolute measurements, not speedup claims against the preliminary run with different asset conditions.

| Scenario / view | Min p50 | Min p95 | Min p99 | Max p50 | Max p95 | Max p99 |
|---|---:|---:|---:|---:|---:|---:|
| Default/system | 3.907 | 5.190 | 5.566 | 4.992 | 6.170 | 6.668 |
| Default/belt | 5.012 | 6.122 | 6.432 | 2.658 | 3.959 | 5.820 |
| Default_500/system | 3.419 | 4.604 | 5.079 | 5.000 | 5.808 | 6.219 |
| Default_500/belt | 4.426 | 5.693 | 6.061 | 3.161 | 4.159 | 4.649 |
| Docked/system | 3.728 | 5.127 | 5.558 | 4.925 | 5.821 | 6.448 |
| Docked/belt | 5.253 | 6.331 | 6.727 | 2.829 | 3.867 | 4.349 |
| MarketProfiles/system | 2.768 | 3.983 | 4.484 | 4.325 | 5.582 | 6.106 |
| MarketProfiles/belt | 3.910 | 5.322 | 5.899 | 2.497 | 3.664 | 4.219 |
| PlayerShipOnly/system | 2.818 | 4.005 | 4.457 | 4.301 | 5.491 | 5.935 |
| PlayerShipOnly/belt | 3.692 | 5.338 | 5.921 | 2.431 | 3.690 | 4.412 |
| Undocked/system | 3.469 | 4.820 | 5.160 | 4.938 | 5.824 | 6.130 |
| Undocked/belt | 5.059 | 6.177 | 6.738 | 2.697 | 3.744 | 4.330 |

No outstanding confirmed US-0008 findings remain. Native target passes in host execution; restricted execution adds substantial swap delay even for an empty window. Full epic integration findings are recorded separately below.


## Epic integration review

Same-agent review rechecked New Game versus Load, normalized references, save format 12, absolute motion epochs, generated geometry and scenario preservation, command-driven station visits, presentation order and actual window evidence.

Two additional defects were reproduced before fixing:
- Orbital docking rejected even five seconds after successful synchronization (also at x100), because the continuously changing tangent immediately exceeded the legacy angular epsilon. Orbital targets now use the existing 1e-6 km/s budget for the full relative velocity vector; the scalar speed check and legacy angular epsilon remain unchanged. This is an explicit orbital docking validation extension: no automatic steering, speed change, teleport, Approach change or new holding state. Existing revalidation/fee rollback still rejects a one-degree heading mismatch. Delayed input is tested both uninterrupted and across save/load.
- Case-insensitive saved solar-map references passed validation but were published with noncanonical spelling, so exact Client lookups could miss planet styling. Load now normalizes map planet/orbit references to the owning object IDs, matching other scenario references.

The final Client run also exposed a one-ULP coordinate difference in the existing pirate save/load test after world translation: -203887.2149070323 versus -203887.21490703232. Its position assertions now use the established 1e-6 world-unit continuation tolerance (0.1 mm), while every other snapshot field remains an exact comparison.

Final validation commands/results:
- dotnet test DeepSpaceSaga.sln --no-restore: Contracts 105, Motion 141, Engine 1269, Client 1596, Performance 2; total 3113 passed, zero failures/skips.
- After the raster asset-directory review change, dotnet test tools/DeepSpaceSaga.Performance.Tests -c Release --no-restore: 2/2 passed.
- Release Client/performance build succeeded with zero warnings/errors. Scoped dotnet format whitespace --verify-no-changes --no-restore passed for the changed production files; changed test files were formatted and verified. git diff --check passed.
- Epic regression tests include five cases: delayed docking at x1/x100, uninterrupted/save-load, and canonical solar-map references. Before fixes the original two delays and case-reference test all failed; after fixes these and the existing dock revalidation/rollback tests pass.
- Native images and transient JSON measurements were inspected and removed after their textual evidence was recorded, as required by TK-0003.

Final review outcome: no outstanding confirmed implementation findings. E2-AC-01..08 are covered by the story evidence, full test run, real-file continuation tests and native host acceptance. Human-operated playthrough was not performed; native input was scripted and captured frames were visually inspected. The restricted-execution presentation limit remains an environment-specific observation, not an unresolved code-performance finding.

## Актуализация EP-0004 — 2026-10-08

Результаты выше — датированная поставка EP-0002. EP-0004 добавляет AI/поля/POI и строгую persistence/selection интеграцию; её текущая native80FPS acceptance OPEN. Старые FPS/счётчики EP-0002 не являются повторным измерением полной карты.

[Текущий технический контракт и evidence](AiMapEnvironment.md).
