# EP-0035 execution

Started 2026-11-17 under the owner's explicit whole-epic execution request.
Branch: `codex/ep-0035-demo-pirate-encounters`; remote: `origin` (`https://github.com/SuhilovVitaly/DSS`).
Base: `0c06524b5e1c9fb631ff6c6dc49ca2da48717a23`, fetched main; local main fast-forwarded.
Initial working tree was clean. No game launch is authorized or performed.

## Execution order and gates

Start with US0017-TK0001 central dispatch, then sensors (US0007), visible groups
(US0001), operator (US0009), signatures (US0011), squadrons (US0012), reports
(US0010), fog (US0008), pursuit (US0002), world battle (US0003), continuation
(US0004), squad assembly/rules/results (US0013–0015), world-step integration
(US0017-TK0002), durable recovery (US0016). Actual ticket prerequisites take
precedence over this summary. US0005 additionally requires EP0034 passenger contracts.
US0006 already covers project documentation and depends on all functional stories;
it remains the last story, followed by the complete headless suite and final review.

Tests run only at story/epic boundaries. Builds, scoped format and diff checks
run at ticket boundaries. Each ticket is committed and pushed before the next.
Native/product acceptance: NOT RUN — owner launches the game.

## External dependency

At base, EP0034 passenger runtime is absent from production and its canonical
Board remains not-started on main. Published prerequisite work exists in origin/feature/ep-0034-demo-transport-contracts (US0002 validation 7d8c3bae); integration is pending compatibility review. US0005 cannot be accepted against placeholder transport
DTOs. Other independent work continues; this dependency is not silently implemented
as part of EP0035 or merged from an unreviewed branch.

## Current work

US0017-TK0001: central command, validation and calendar tick dispatch; existing
feature bodies preserved as named helpers. Release Engine.Tests build passed (0 warnings/errors); scoped whitespace verification and git diff --check passed.
Tests NOT RUN — scheduled with US0017 completion, including the existing EP0033
repair, supply, replacement, recruitment and upkeep regression classes and
`Ep0035Us0017Tk0001Tests`. No story is accepted yet.

Evidence root: `D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-execution/`.

## Published tickets and sensor boundary
| Ticket | SHA | Push |
|---|---|---|
| US0017-TK0001 | 879f49a837214ee0318c8c3a0c2e24343defe270 | verified |
| US0007-TK0001 | 34bf0dc412e8c81c532dc5a1b6ac2474c4e4c9af | verified |
| US0007-TK0002 | 35cd9400ab637046525a446411e20fab82571227 | verified |
| US0007-TK0003 | 422410e560747758e3b5fd96f1599b5a3d393d23 | verified |
| US0007-TK0004 | 642e72baae5a2dbb07ff6b22f9b47c11b275afb6 | verified |

US0007 boundary: filter FullyQualifiedName~DemoEp0035Us0007; Engine 18 PASS, Contracts 2 PASS, 0 failures/skips. Commands used Release/no-build/no-restore, explicit artifacts-path/results-directory and blame-hang-timeout 5m; actual processes finished in under 6 seconds. Logs sensor-engine-tests.log and sensor-contracts-tests.log under the evidence root. Full suite NOT RUN: remaining stories are incomplete. Operator modifiers and pirate-group integration are still required; these tests do not establish the complete encounter loop. Independent implementation review requested.
