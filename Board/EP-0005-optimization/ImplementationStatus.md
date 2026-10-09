# EP-0005 execution registry

Authorized 2026-10-09 under EpicExecutionPrompt.md. Isolated checkout D:/DeepSpaceSaga/DSS-EP-0005, branch codex/ep-0005-optimization, base origin/base-fight 4233f52346b7d77edc4e04d677f52d7328353fda. Original checkout and unpublished EP-0008 changes preserved.

The table below records progress for the original 20 tickets. Final documentation/Graphify story adds 2 tickets. Execute US1 TK1..15, then US2 TK1..5, then US3 TK1..2; review each story and epic. Publish each ticket before starting the next.

Preparation: Documentation and epic contracts read; existing Graphify queried as navigation. NuGet restore succeeded on host. Branch created on origin; per-ticket publication recorded below. Native/manual acceptance NOT RUN. Build/test evidence pending.

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
| EP-0005-US-0001-TK-0015-free-viewport-cost | implemented; Client 1800/1800 plus final geometry 6/6, p99 32 obstacles 143.16 -> 0.97ms; native OPEN | awaiting commit and immediate push |
| EP-0005-US-0002-TK-0001-frame-state-update | pending | pending |
| EP-0005-US-0002-TK-0002-scene-geometry-prepare | pending | pending |
| EP-0005-US-0002-TK-0003-read-only-map-painter | pending | pending |
| EP-0005-US-0002-TK-0004-revision-driven-scene-cache | pending | pending |
| EP-0005-US-0002-TK-0005-frame-consumers-and-performance | pending | pending |
| EP-0005-US-0003-TK-0001-documentation-sync | pending | pending |
| EP-0005-US-0003-TK-0002-graph-rebuild | pending | pending |
