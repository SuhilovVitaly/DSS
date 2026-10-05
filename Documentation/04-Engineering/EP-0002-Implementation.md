# EP-0002 implementation evidence

User authorization: implement every story and ticket, fix issues found by story/epic reviews, change project files and tests as needed, commit and push every ticket. Destination explicitly confirmed: origin (https://github.com/SuhilovVitaly/DSS), base-fight.

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

## US-0008 — correctness and performance evidence (in progress)

TK-0001: b31b1c2. Boundary corpus covers 4800 worlds: seeds 1..100, all six scenarios, 3/7 planets, 2/5 belts and 50/75 starting days. All 49 test rows passed. Full solution after fixes: Contracts 105, Motion 141, Engine 1264, Client 1591. Corpus exposed max-density seed 36 exhaustion: placement now retries each corridor before restarting the entire attempt, retaining bounded budgets. Configuration permits finite subranges within 50..75 to make the required boundary corpus possible; shipped 50..75 remains unchanged. Generated save data is resolved, so previously saved worlds do not regenerate.

TK-0002 measurement commands (Release, sequential runs, source b31b1c2):

    dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/solar-performance-min.json --solar-map --seeds 1:100 --scenarios all --config min
    dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/solar-performance-max.json --solar-map --seeds 1:100 --scenarios all --config max

Both measurement reports completed successfully, 600 generated worlds each. First seed per scenario is rendered separately in System and belt views, 120 warmup / 600 measured frames at 1920x1080. CPU/Skia raster maximum p99 across views: min 6.3785 ms, max 6.6625 ms. Largest saves: 950025 / 1132976 UTF-8 bytes. Maximum generation time: 67.0542 / 67.1579 ms (includes initial JIT); maximum snapshot sample: 15.8739 / 11.5505 ms. These are absolute observations, not comparisons against a historical baseline. GPU/presentation status remains not-measured here.

Tooling tests: ReportHasReproductionAndBackend and InvalidConfigReturnsFailure passed 2/2; the latter runs the CLI process and verifies exit code 1. The new test project is included in the solution.
