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

US-0004–0008 and final epic review remain open. No completion is inferred from the planning documents' stage fields.
