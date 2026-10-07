# EP-0003 implementation and review evidence

Execution authorized on 2026-10-07 for all twenty tickets, story reviews and epic review, with separate ticket commits and pushes to the existing `base-fight` branch. Existing untracked `Board/EP-0001-trading-system/ImplementationStatus.md` is excluded.

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

## Remaining work

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

Actual Release CLI: `dotnet run --project tools/DeepSpaceSaga.EconomyBalance -c Release --no-restore -- <root> <output.json> --cluster-matrix tools/DeepSpaceSaga.EconomyBalance/cluster-matrix.json`. All six cases cover seeds 1, 2, 42 × starter/cargo-upgrade, three real local cycles, hourly authoritative stocks/targets/maxima/budgets/events, and an outbound in-transit hold until 100 days before Approach. Finite initial credits remain 2000. Four cases complete the intercluster return at about 148–150 days. Both seed-2 cases report `insufficient_voyage_fuel` on the return at 135.7 days after the local cycles. They remain incomplete evidence, not successful economic acceptance. Ledger losses remain visible; no cluster profitability threshold is invented. Correctness violations: zero; continuation hashes match in all six cases. See `EP-0003-EconomySummary.json` for source commit, raw-report SHA256, per-case clocks, capacities, outcomes, balances and ledger amounts.

Story review repaired Windows path normalization in the output guard and the full-report string-buffer limit. Output now serializes atomically to a stream; static cargo directions are retained once and hourly route samples identify the exercised local/intercluster directions. Every station's economy remains sampled hourly. Production-shortage diagnostics compare remaining stock to the profile's declared base hourly inputs; they do not claim an unavailable per-factory execution trace. Failed/incomplete strategies and missing product thresholds are explicit findings owned by EP-0001. The fuel-shortfall observation is product economy follow-up, not a reason to falsify the result or alter EP-0001 formulas.

US-0008 and final epic review remain open. No completion is inferred from planning stage fields.
