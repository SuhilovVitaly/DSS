# EP-0003 implementation and review evidence

Execution authorized on 2026-10-07 for all twenty tickets, story reviews and epic review, with separate ticket commits and pushes to the existing `base-fight` branch. Existing untracked `Board/EP-0001-trading-system/ImplementationStatus.md` is excluded.

Earlier story notes below record the evidence available at their commits. Final US-0008/native and epic regression results supersede their historical NOT RUN notes. Unrelated `Board/EpicExecutionPrompt.md` is also excluded.

## US-0001 — Local trading cluster

Delivered TK-0001 `305f90e`, TK-0002 `5ac5ae0`, TK-0003 `c170b53`, TK-0004 `17c7196`; each pushed separately to origin/base-fight.

- Contracts: 159/159 tests; compatible legacy JSON, default arrays, explicit camelCase membership and cargo links.
- Orbital dependency checks: 11/11.
- LocalClusterGenerationTests: 9/9, including six scenario starts, preserved inventory/IDs/distances, immutable seed replay, rigid movement and the preserved short-neighbour exception.
- Existing solar correctness corpus: 4800 worlds, all 58 test cases in the combined local/corpus run passed (`ep3-local-corpus.trx`).
- Engine full regression: 1675/1675 (`ep3-local-engine-fixed.trx`).
- Client full regression: 1713/1713 (`ep3-local-client-fixed.trx`).
- LocalClusterMapTests: 4/4 after strengthening with actual clicks on every station in the fitted cluster.
- Engine/Client builds: zero errors and warnings. Scoped format verification and `git diff --check` passed.

Commands: `dotnet test <matching csproj> --no-restore`, the focused class filters above, `dotnet build <affected csproj> --no-restore`, and `dotnet format <affected csproj> --verify-no-changes --no-restore --include <ticket files>`.

Story review inspected the complete change from `818cc1a` through `17c7196` against AC-0001–0003, production authority, membership, cargo semantics, rigid orbit periods, legacy mode and remote-trade gating. Confirmed issues found during validation were repaired: generated-station clearance near original scenario objects, fallback orbital period respecting the generated system, and startup tests that still required five stations in the newly enabled mode. Transit directions do not claim cargo production. No unresolved defect found in this story's delivered contribution.

The cluster snapshot is intentionally not yet persisted: US-0006 owns that extension. This is not evidence of epic save/load completion. Native interactive UI acceptance is NOT RUN; raster tests are automated UI evidence only. Full-scale/native evidence belongs to US-0008.

## US-0002 — Distinct orbital districts

Delivered TK-0001 `0ca294a`, TK-0002 `262971a`, TK-0003 `76ab1fc`; each pushed separately. Engine full regression 1679/1679 (`ep3-full-network-engine.trx`); Client full regression 1718/1718 (`ep3-multi-client.trx`). Boundary content covers all six scenarios, 2/5 belts, 3/5 clusters, 10/12 stations and seeds 1, 2, 42. Repeatability, 365-day rigid-distance checks, actual cluster framing and moving bounds passed. Swept radial envelopes are disjoint, proving separation at conjunction and opposition as well as sampled epochs. Group periods differ with radius. Initial nearest-cluster distances are validated from actual centroids against configured 15–35 days.

Review compared all three tickets and AC-0001–0003: source stations remain in the home quota, uninhabited belts are allowed, profiles and resource ownership are not inferred from camera aggregation, and intercluster links contain resolved endpoints and cargo candidates without permanent ETAs. No unresolved defect found in this story's contribution. Native acceptance remains NOT RUN until US-0008 evidence.

## US-0003 — Resource surroundings

Delivered TK-0001 `5f7b9df`, TK-0002 `14e3a7a`, TK-0003 `33f7499`; each pushed separately. Contracts 161/161; Engine 1682/1682 (`ep3-resource-engine.trx`, eight minutes); Client 1720/1720 (`ep3-resource-client.trx`). Scoped format verification and diff checks passed. Build was included in the matching full test runs with no warnings or errors.

Story review checked all three tickets and their source-of-truth invariant. Every station receives the existing EP-0001 role-specific asteroid counts and composition, canonical asteroid IDs serve as resource binding IDs, and no duplicate field inventory is introduced. Resource surveys are already known only in the new cluster mode. Legacy resource tests retain unknown composition and scanning. Relative distances are preserved at 1/7/30/100/365 days, deterministic generation and manifest JSON continuation pass, and a control world without fields has identical authoritative market diagnostics after one day. Resources do not produce market stock. The Client renders composition, resolved cluster and anchor IDs from the same snapshot. No unresolved defect found in this story's contribution.

Native acceptance remains NOT RUN. Full cluster-map save persistence belongs to US-0006; the manifest round trip above does not claim that future contribution is complete.

## US-0004 — Current travel estimates

TK-0001 delivered and pushed separately. Client full regression 1723/1723 (`ep3-travel-client.trx`). Focused travel plus existing Approach projection tests 34/34 after the numeric-boundary repair. Build, scoped format and diff checks passed.

Story review checked AC-0001–0003: distance uses the displayed predicted poses, speed comes from the authoritative installed-engine maximum, days use the world-to-km and calendar ratio 300, and the epoch is physical simulation time. Missing, nonpositive, nonfinite speed or numeric overflow produces an unavailable estimate. Camera fitting and viewport changes preserve distance/estimate; moving districts change estimates, paused replay preserves them. Selected-station potential directions are dashed and contain no rendezvous marker; the existing confirmed Approach projection still passes its 31 tests. Stale/unavailable market observations are retained without refreshing a remote quote. No unresolved defect found in this story's contribution.

## US-0005 — Real trading voyages

Delivered TK-0001 `b27bcea`, TK-0002 `e6cbfe1`; each committed and pushed separately. Engine full regression 1689/1689 (`ep3-voyage-engine-fixed.trx`); Client 1728/1728 (`ep3-voyage-client.trx`). Follow-up voyage, fuel, route and save/resume checks 97/97 (`ep3-save-fixed-focus.trx`); strengthened Client command-path assertions 5/5.

Review covered real A→B→A and A→C→A commands for seeds 1, 2, 42, quotes, receipts, finite station/player budgets, docking dialogues, replay idempotency, installed-speed Approach and ledger-owned profit. Current orbital distances feed the existing EP-0001 route owner. Repeated route evaluation during snapshot construction was removed; no distance cache is persisted. Generated stations now inherit the scenario port fee so their docking dialogue can execute.

The shipped starter engine efficiency changed from 10 to 100 km/kg: the existing 1000 kg tank could not support the required 15–35 day return voyage at the former content value. Engine fuel formulas, speed and capacity are unchanged. Numeric conservation tests explicitly retain their 10 km/kg fixture. Optional cluster-map save wiring landed here to keep newly generated route-event endpoints valid in existing save tests; full validation/continuation is delivered in US-0006. Review found and repaired departure-distance restoration: absolute orbit epochs must be used after save rebasing. Native acceptance remains NOT RUN.

## US-0006 — Save and local resume

Both tickets committed and pushed separately. ClusterJsonRoundTripAndContinuation covers seeds 1, 2, 42, an active Approach, docked continuation, 100 days of bounded economy and the return trade. NoMarketResetOnLoad verifies canonical resources and market diagnostics. Invalid membership/profile/belt/link/resource/null references and a removed return path reject atomically. LocalClient saves to actual files, reloads through Settings and completes the same return commands; replayed Buy has exactly one durable receipt and no second posting.

Full Engine run: 1706 passed, two pre-existing catalog-diagnostic assertions failed because new cluster preflight ran before inventory validation (`ep3-save-engine.trx`). Preflight now runs after candidate inventory construction and before publication. The repaired complete non-corpus regression passes 1661/1661 (`ep3-save-regression-fixed.trx`), including the two catalog cases, new reverse-connectivity rejection and active fuel restoration after an earlier completed voyage. The 4800-world corpus passed in the preceding full run. Focused long-run cap save also passes.

Story review found and repaired missing cluster context in fuel validation invoked for historical settlements while another voyage remains active. Quote IDs issued after load are transient session capabilities; comparison excludes those only, retaining exact economic amounts, revisions, command IDs and persisted receipts. Save format remains the existing version with an optional additive clusterMap. Legacy saves remain loadable. Native Client interaction is still NOT RUN; LocalClient file/transport evidence above is automated, not manual UI acceptance.

## US-0007 — Long-voyage economy evidence

TK-0001 `1002066`, TK-0002 `a6a0723`, both pushed separately. Tooling regression 57/57 (`ep3-long-diagnostics-regression.trx`); focused long-run 3/3 and diagnostics 3/3. The default legacy runner explicitly keeps its five-station mode, despite cluster-enabled Client settings. Cargo-upgrade is an actual doubled authoritative capacity, not a tool-computed capacity assertion.

Final Release CLI: `dotnet run --project tools/DeepSpaceSaga.EconomyBalance -c Release --no-restore -- <root> <output.json> --cluster-matrix tools/DeepSpaceSaga.EconomyBalance/cluster-matrix.json`. All six cases cover seeds 1, 2, 42 × starter/cargo-upgrade, three real local cycles, hourly authoritative stocks/targets/maxima/budgets/events, and an outbound in-transit hold until 100 days before Approach. Finite initial credits remain 2000. All six complete the intercluster return: seed 1 at 150.002 days, seed 2 at 152.644 days, seed 42 at 148.394 days. Final credits are 1214, 1222 and 1233 respectively for both ship configurations under the explicitly selected unit-batch strategy. Correctness violations: zero; continuation hashes match in all six cases. Ledger losses remain visible; no cluster profitability threshold is invented. See `EP-0003-EconomySummary.json` for source commit `ecb6344`, raw-report SHA256, per-case clocks, actual capacities, receipt-backed leg ledgers and representative diagnostics with owners/reproduction paths.

Story review repaired Windows path normalization in the output guard and the full-report string-buffer limit. Output now serializes atomically to a stream; static cargo directions are retained once and hourly route samples identify the exercised local/intercluster directions. Every station's economy remains sampled hourly. Production-shortage diagnostics compare remaining stock to the profile's declared base hourly inputs; they do not claim an unavailable per-factory execution trace. Failed/incomplete strategies and missing product thresholds are explicit findings owned by EP-0001.

Integration review reproduced the seed-2 return fuel shortfall in the initial report. The shipped starter efficiency is now 200 km/kg, with unchanged fuel formulas, maximum speed, tank size and initial credits. The runner chooses the preserved original mining station when available and the nearest initially reachable industrial destination, with stable ID tie-breaking; departure distances are still sampled from current authoritative poses. These content/strategy corrections were pushed in `454430c`, then the entire matrix was rerun. Final review also repaired a double analytical cargo multiplier: the cluster registry already installs doubled capacity, so analytical availability must equal authoritative availability (`ecb6344`). The regression explicitly asserts 200000 kg installed capacity and equality on all voyage legs. Full economy tooling regression: 58/58 (`ep3-final-economy.trx`). The initial incomplete 100 km/kg result is superseded by the linked final report, not relabelled as successful.

## US-0008 — Complete network evidence and story review

Delivered TK-0001 `cbe143e`, TK-0002 `3077cb1`, TK-0003 `d168fa9`; each pushed separately. The focused correctness corpus passed 50/50 (`ep3-cluster-corpus-fixed.trx`): six scenario starts × 100 seeds × 3/5 clusters × 10/12 stations × 2/5 belts = 4800 generated worlds. It independently checks global IDs, five profiles, producer/consumer compatibility, all-station directed return reachability, compact rigid groups at 0/1/7/30/100/365 days, analytic conjunctions and disjoint station/resource swept annuli. Each world also undergoes real save serialization/load and later orbital continuation. Impossible placement leaves the running world intact.

Actual Release performance CLI measured a fresh 600-world legacy baseline and the complete 4800-world cluster boundary corpus on the same host/runtime, serially without other heavy test runs. For each of the six scenarios it draws maximum 5×12×5 networks in system, belt and cluster views, with 120 warmup and 600 measured raster frames per view. Generation/snapshot/save source commit is `ecb6344`; after the Client-only final review repairs, all eighteen render cases are measured again at seed 1, with their separate `renderingCommit` and raw SHA256 recorded. Full configuration, per-scenario/boundary metrics, rendering measurements and baseline comparison are in `EP-0003-PerformanceSummary.json`. Aggregate cluster medians: generation 15.544 ms / 7,160,128 allocated bytes; snapshot 0.754 ms / 967,924 bytes; save serialization 4.459 ms / 5,848,560 bytes; save payload 2,431,114 bytes. The fresh five-station baseline medians are 1.902 / 0.124 / 1.068 ms respectively. No generation/snapshot/save threshold was specified, so these are measured costs rather than invented pass budgets.

Final CPU/raster p99 ranges from 6.242 to 12.746 ms across the eighteen sampled views at `59b8643`. This is explicitly separate from real native frame evidence and does not claim GPU FPS. Economic references match both scenario and seed; only the Default cases for seeds 1, 2, 42 link to the independent economy ship/config runs. The remaining scenario/seed rows explicitly say missing evidence rather than inheriting Default's economic result. Tooling regression passes 4/4 (`ep3-final-performance.trx`).

Story review covered all three tickets, corpus oracle independence, maximum-resource rendering, the UI command path, freshness and provenance of reports, and EP-0004 handoff completeness. Confirmed repairs: floating comparison used an absolute 1e-6 distance tolerance instead of unstable decimal rounding; the collapsed combat journal is included in map fit obstacles; legacy five-station/save fixtures explicitly disable the new mode rather than altering production behavior. Native inspection found long cargo directions pushing the travel estimate beyond the viewport. `c2c7547` gives info bodies viewport limits, complete wheel scrolling, a scroll indicator and selection reset; estimates precede the long directions. Focused panel regression passes 40/40 and full Client regression passes 1733/1733. No cargo direction is removed from the authoritative model or silently truncated.

Final native measurements use a real Skia/OpenGL window with the existing LocalClient transport and actual screen input handlers. Maximum Default_500, seed 1, seven planets, five belts, five clusters, sixty stations and 1328 total objects are exercised. All five final cases use the clean source commit `59b8643`: system/1920×1080/UI 1, selected target/UI 1.5, belt/UI 1.2, system/1280×720/UI 1, and true cluster zoom/1920×1080/UI 1.5. All sixty stations are selected in each case; pause/resume buttons and real wheel routing are exercised during warmup. Every case passes the target criterion. Actual window monitor refresh is 100 Hz with VSync enabled; GPU is Intel Arc 140V (16GB), OpenGL 3.3 driver 32.0.101.8860. CPU is Intel64 Family 6 Model 189 Stepping 1, eight logical processors; .NET 8.0.26; Windows 10.0.26200. Timing includes SwapBuffers wait, with CPU submit reported separately; GPU execution and physical scanout are not directly measured.

| Native case | Mean FPS | p99 swap interval, ms | Target p99 ≤ 12.5 ms |
|---|---:|---:|---|
| System, 1920×1080, UI 1 | 99.490 | 12.4214 | Passed |
| Selected target, 1920×1080, UI 1.5 | 99.659 | 10.6811 | Passed |
| Belt, 1920×1080, UI 1.2 | 99.729 | 10.6865 | Passed |
| System, 1280×720, UI 1 | 99.619 | 10.7835 | Passed |
| Cluster zoom, 1920×1080, UI 1.5 | 99.538 | 11.8115 | Passed |

System, belt, selected, small-window and true cluster-zoom screenshots were inspected, including the final cluster/small-window renders after the orbit repair. Names, profiles and travel estimates remain visible, selected objects survive LOD/viewport changes, and long directions stay inside the scrolling body. This is scripted automated native acceptance plus visual inspection, not a human manual playthrough. `EP-0003-NativeSummary.json` keeps context, measured distributions, target verdicts, interaction results, module identities, raw hashes and intermediate failed diagnostics. The harness `fce55fe` measures true cluster zoom and checks wheel routing without changing camera zoom. The target is a sampled p99 criterion on the recorded host; occasional longer frames are retained in the distributions.

True cluster zoom initially reproduced 48.163 mean FPS / 30.625 ms p99. A diagnostic with orbits temporarily disabled isolated large projected ellipse submission; the diagnostic switch was removed. `3b22ff2` submits only visible bounded arcs for very large orbits, preserving enabled orbit rendering and raster alignment. The first GPU repair still failed CPU p99. Stage measurements identified repeated object scans in trail filtering and cluster presentation; `f7d0cb8` indexes missile IDs once per frame, and `59b8643` indexes render poses and reuses each info row's wrapped layout. GC and worst-CPU-frame evidence remain in the native summary. Final full Client regression passes 1735/1735, and all five native cases were rerun after these repairs. No final result is substituted for a failed intermediate measurement.

Reproduction commands, run serially in Release from the repository root:

```text
dotnet build tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj -c Release --no-restore
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll <root> <baseline.json> --solar-map --seeds 1:100 --scenarios all --config max
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll <root> <clusters.json> --solar-map --clusters --seeds 1:100 --scenarios all --config max --economy-report <economy-summary.json> --baseline <baseline.json>
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll <root> <native.json> --solar-window max system 1 1920x1080 --clusters
```

Replace the native view/scale/size with the recorded selected/belt/small/cluster cases. Baseline freshness requires matching host/runtime and generation settings and a file age under three hours. Timings sample seed 1 for rendering, not all hundred seeds. Economic profitability, other economy scenario/seed combinations, generation cost limits, physical scanout and a human manual playthrough remain explicitly unassessed; they are not presented as passed checks.

## Final epic review and regression

All twenty tickets and all eight story reviews are complete. Final review compared the combined production/test/tool changes against `Board/EP-0003-station-clusters-and-trade-geography/Documentation.md`, the eight story AC sets and all twenty ticket contracts. It traced Engine ownership of geography/economy/time, Client projection and real command execution, save/load atomicity, deterministic generation and report provenance. Confirmed findings were repaired and published in the review commits recorded above. No unresolved implementation defect was found within EP-0003; the explicitly unassessed product/hardware areas above remain evidence limitations.

| Epic criterion | Final evidence |
|---|---|
| E3-AC-01 | 4800-world fixed corpus: 3/5 clusters, 10/12 stations, five profiles, stable IDs, deterministic geometry/resources; Contracts and Engine regression. |
| E3-AC-02 | Independent corpus checks of directed reachability/return paths, independent local cycles, producer/consumer compatibility and start backhaul; transit does not require production. |
| E3-AC-03 | Rigid distances through 365 days and analytic swept corridor separation; live epoch/speed estimates and save-rebased departure distances. |
| E3-AC-04 | All six scenario starts; original station IDs, references, inventories and station quota; final 50–75-day start radius. Preserved MarketProfiles short-neighbour exception is explicit. |
| E3-AC-05 | Actual local and intercluster buy/undock/Approach/synchronize/dock/sell/return commands, receipts, finite budgets and duplicate-command checks; installed-speed Approach preserved. |
| E3-AC-06 | Native system/belt/cluster/selection views, all sixty station selections in every case, bounded scrolling at both viewport sizes; authoritative profiles/resources/distance/labelled estimates; remote quote freshness unchanged. |
| E3-AC-07 | Actual file/LocalClient resume in Approach and dock, invalid loads reject before world publication, resource/market/voyage continuation, all six long-run save/load hashes match. |
| E3-AC-08 | Six receipt-backed economy cases through 148–153 days, three local cycles and full intercluster return, hourly stock/target/budget/event evidence, explicit ledger losses and EP-0001 diagnostic ownership. |
| E3-AC-09 | Full boundary/scenario/seed corpus, generation/snapshot/save metrics versus fresh baseline, eighteen raster views and five actual native views passing sampled p99 ≤ 12.5 ms on the specified host. |

EP-0004 receives the persisted cluster IDs, belt membership, home district, rigid orbital placement, live station membership/bounds, local links, intercluster directions and resource bindings through the existing `ClusterMap` contract. No AI territories or environment effects are asserted before that epic.

Final matching project regressions:

| Project | Passed / total | TRX under project TestResults |
|---|---:|---|
| `tests/DeepSpaceSaga.Engine.Tests` | 1760 / 1760 | `ep3-final-engine.trx` |
| `tests/DeepSpaceSaga.Client.Tests` | 1735 / 1735 | `ep3-final-client-layout.trx` |
| `tests/DeepSpaceSaga.Contracts.Tests` | 161 / 161 | `ep3-final-contracts.trx` |
| `tests/DeepSpaceSaga.Motion.Tests` | 141 / 141 | `ep3-final-motion.trx` |
| `tests/DeepSpaceSaga.EconomyBalance.Tests` | 58 / 58 | `ep3-final-economy.trx` |
| `tools/DeepSpaceSaga.Performance.Tests` | 4 / 4 | `ep3-final-performance.trx` |
| Total | 3859 / 3859 | Six projects |

Engine Release regression includes the legacy and cluster 4800-world corpuses. Client Debug regression ran after the last Client change; the final Release build and all final native/raster measurements also use `59b8643`. Economy/Performance tooling regressions cover `ecb6344`; later changes affect Client rendering and were verified by the complete Client suite and actual Release CLI runs. Matching builds report zero warnings/errors. Scoped `dotnet format --verify-no-changes --no-restore --include <changed files>` and `git diff --check` pass. Reproduce project regressions with `dotnet test <project.csproj> --no-restore --logger "trx;LogFileName=<report.trx>"`, using Release for Engine and the tools and the recorded project configuration for other suites.

Compact JSON summaries preserve source commits, raw SHA256 hashes, coverage, measured distributions and diagnostic findings. Ignored temporary raw reports, screenshots and the one-off compaction helper are removed after evidence review as required by TK-0003; the commands above regenerate them. TRX reports remain local test evidence. The two unrelated Board files remain untouched and excluded from all commits.
