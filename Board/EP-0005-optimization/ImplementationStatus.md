# EP-0005 execution registry

Authorized 2026-10-09 under EpicExecutionPrompt.md. Isolated checkout D:/DeepSpaceSaga/DSS-EP-0005, branch codex/ep-0005-optimization, base origin/base-fight 4233f52346b7d77edc4e04d677f52d7328353fda. Original checkout and unpublished EP-0008 changes preserved.

The table below records progress for the original 20 tickets. Final documentation/Graphify story adds 2 tickets. Execute US1 TK1..15, then US2 TK1..5, then US3 TK1..2; review each story and epic. Publish each ticket before starting the next.

Preparation: Documentation and epic contracts read; existing Graphify queried as navigation. NuGet restore succeeded on host. Branch created on origin; per-ticket publication recorded below. Final automated Client 1827/1827; Release/format/diff pass. Native scripted matrix 16 cases/80 actions completed; performance 80 FPS FAILED/OPEN, human manual NOT RUN. See PerformanceEvidence.md.

Draft assumptions accepted for this execution: 2000 ms real-time stale limit, deterministic bounded label fallback. Shared Engine/Motion/save contracts remain unchanged unless a confirmed dependency requires an explicit scope entry.

| Ticket | Implementation / checks | Publication |
|---|---|---|
| EP-0005-US-0001-TK-0001-paused-authoritative-rebase | implemented; Client 1736/1736, Release/format/diff pass; native OPEN | 4f30eb63f839dd65e2186a8531860011e4ad0fec; push and remote SHA verified |
| EP-0005-US-0001-TK-0002-visible-object-hit-testing | implemented; full 1743 pass plus repaired fixture/new cases 9/9; Release/format/diff pass; native OPEN | 48d1274b732ad85d04482c713b6a3d922ddef414; push and remote SHA verified |
| EP-0005-US-0001-TK-0003-cluster-click-priority | implemented; Client 1747/1747, Release/diff pass, baseline format debt; native OPEN | 4a2f9852a4bbfa96d2bf8b290f3e7e8846236fe3; push and remote SHA verified |
| EP-0005-US-0001-TK-0004-important-label-placement | implemented; Client 1751/1751, Release/format/diff pass; native OPEN | d81d58c50fdf3de7f1c20695abc369e9bdfdb270; push and remote SHA verified |
| EP-0005-US-0001-TK-0005-bounded-offscreen-work | implemented; Client 1755/1755, Release/format/diff pass; native OPEN | 7ab6af2af3eab4c5d0ec7d4866a54184bfc3018e; push and remote SHA verified |
| EP-0005-US-0001-TK-0006-paused-geometry-invalidation | implemented; Client 1757/1757, Release/format/diff pass; native OPEN | 415660a95b491ee0c89c201394671b09455696f5; push and remote SHA verified |
| EP-0005-US-0001-TK-0007-coherent-frame-diagnostics | implemented; Client 1759/1759, Release/format/diff pass; native OPEN | cd33d91c3920648dbfe03b6f9b0dc1572a102899; push and remote SHA verified |
| EP-0005-US-0001-TK-0008-layout-before-map | implemented; Client 1763/1763, Release/format/diff pass; native OPEN | f23224d8c0181870d766a750f9698719b4a9a7bc; push and remote SHA verified |
| EP-0005-US-0001-TK-0009-nonblocking-render-io | implemented; Client 1767/1767 plus final async 5/5, Release/format/diff pass; native OPEN | eef73de431413f43ccd469ab73838f2d11610e6d; push and remote SHA verified |
| EP-0005-US-0001-TK-0010-deterministic-resource-lifetime | implemented; Client 1774/1774, 100 native-handle cycles, Release/format/diff pass; native window OPEN | d1f6ee45791857f8b3ecfa845b51b8f8d4b69a71; push and remote SHA verified |
| EP-0005-US-0001-TK-0011-map-localization | implemented; Client 1777/1777, Release/format/diff pass; native OPEN | 0a20825b06e3520e4498bf77a9901dc94cc02169; push and remote SHA verified |
| EP-0005-US-0001-TK-0012-stable-cluster-membership | implemented; reproduced 2 failures, Client 1783/1783, Release/format/diff pass; native OPEN | 355a332a6250bb1b8034eeeda4f29ed8156ccc84; push and remote SHA verified |
| EP-0005-US-0001-TK-0013-reconciled-route-join | implemented; reproduced 2 failures, Client 1787/1787, Release/format/diff pass; native OPEN | 9887b14d64d5106b529c1a8660cd99331d567fac; push and remote SHA verified |
| EP-0005-US-0001-TK-0014-stale-snapshot-policy | implemented; reproduced 4 failures, Client 1795/1795, Release/format/diff pass; native OPEN | fd46544e140bd72e2d8a96e4e27c099862124afd; push and remote SHA verified |
| EP-0005-US-0001-TK-0015-free-viewport-cost | implemented; Client 1800/1800 plus final geometry 6/6, p99 32 obstacles 143.16 -> 0.97ms; native OPEN | 91b2feca58456d9d5f83b5a593b047a57493b5f1; push and remote SHA verified |
| EP-0005-US-0002-TK-0001-frame-state-update | implemented; Client 1804/1804, Release/format/diff pass; native OPEN | 3d82b3a7220f7b3224a6e9043cd1db2ace525a34; push and remote SHA verified |
| EP-0005-US-0002-TK-0002-scene-geometry-prepare | implemented shadow seam; Client 1807/1807, Release/format/diff pass; native OPEN | 782ad02412cb86e0639c661fa563fd6467310d93; push and remote SHA verified |
| EP-0005-US-0002-TK-0003-read-only-map-painter | implemented; Client 1810/1810, raster replay/layers/native lifetime pass, Release/format/diff pass; native OPEN | 789c08ac0a9787ab66b2d6aae57a79c0de97e231; push and remote SHA verified |
| EP-0005-US-0002-TK-0004-revision-driven-scene-cache | implemented; Client 1822/1822, revision/cache/spatial cases 12/12, Release/format/diff pass; native OPEN | 35081a109fa747dee6bbdbdd523da1022ba2a946; push and remote SHA verified |
| EP-0005-US-0002-TK-0005-frame-consumers-and-performance | implemented; Client 1826/1826, Release/format/diff pass; 20 raster + 16 native cases, 80 scripted actions pass; 80 FPS FAILED, manual OPEN | 10b6414b9226956f4eb99b89741176d102ab2102; push and remote SHA verified |
| EP-0005-US-0003-TK-0001-documentation-sync | implemented; repository Markdown inventory, canonical/status sync, links/IDs/DAG/diff checked | 2de330c78b1e3f1ec2154001d35c75a300d2fd6d; push and remote SHA verified |
| EP-0005-US-0003-TK-0002-graph-rebuild | delivered graph/corpus/hash/health/query evidence: GraphRebuild.md; native gates unchanged | own commit and exact remote SHA verified after creation; see Git/final execution reply |

Review follow-up: US1 TK3 additional Ctrl-cluster expansion case; 29/29 focused and 1827/1827 full Client pass. Commit `cc736b7a2c4ecce314ae3a844aa88a079b6301c8`, immediate push and exact remote SHA verified. Production binary/evidence unchanged.

Final delivery: all 22 ticket implementations and the TK3 review follow-up are recorded separately. Documentation/navigation story artifacts do not close 80 FPS FAILED/OPEN or human manual NOT RUN. See GraphRebuild.md and US-0003-Review.md for final derived validation.
