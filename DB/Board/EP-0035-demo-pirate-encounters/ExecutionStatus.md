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

## Current partial result
The epic is NOT complete: 5 of 66 ticket implementations are published; 61 remain, and no complete pirate encounter loop is delivered. US0007's isolated sensor layer is verified, but cross-story operator/group acceptance is open. US0017 has only TK0001, so its tests remain deferred to its story boundary. Full Documentation/epic reading and full-epic review are not complete; dependency review must not be represented as either.

Independent review of 0c06524b..642e72ba found no confirmed production defect and one coverage gap: direct ObserveSensorContact was untested. Follow-up 5d825019 adds the real object/time/pause/serialized-load test. Corrected US0007 boundary: 19 Engine PASS plus 2 Contracts PASS. Current log: D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-execution/sensor-reviewed-tests.log. No native window/game was launched.

Remaining: US0001–0005, US0008–0016, US0017-TK0002, then US0006 documentation; dependency restructuring for early group/profile/member schema is required before US0001. EP34 prerequisite integration is still pending; it is not a blocker to all independent remaining work. Full suite, final Graphify rebuild, final review and owner acceptance are NOT RUN. No merge into main performed.

Build intermediates and disposable TRX are removed after checks. Retained evidence: named build/format/test logs in the run directory. Source and Board changes are preserved in the published branch.

## Resume 2026-11-17: US0009
All five operator tickets published: TK0001 d8da891e, TK0002 e391f6e1, TK0005 bc749bd0, TK0003 5d25f6da, TK0004 3f8af7d3. Independent review identified overflow-before-publication and lost-pilot assignment cleanup; fixed in 2c8e48c4, fixture correction 80998b13. Final Engine story/regression filter passed32, Contracts4, Client1. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/us9-reviewed-final.log, us9-Contracts-tests.log, us9-Client-tests.log. Native NOT RUN. Previous TK4 diff check reported extra EOF blank line; corrected in2c8e48c4 (the original PASS wording was premature).

EP34 dependency review: pinned34825dce has passenger contracts, real boarding/delivery and US5 validation. Integrate this published prerequisite into the EP35 branch, preserving EP33 central dispatch/action commits and projecting both transport and sensors. Carry sensor/Irina assignment into the separate demo-starter-carrier manifest introduced there. Do not merge into main.

Prerequisite merge static checks: Client.Tests and Engine.Tests Release builds PASS (0 warnings/errors). Conflict resolution preserves central guarded dispatch with deferred transport commit, combined state validation and demo manifest sensor. Transport tick validation now receives the same authoritative timestamp as its caller; clock-only duplicate validation was a semantic merge defect. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/dependency-engine-build.log and dependency-client-build.log. Relevant completed prerequisite-story regression tests run after merge publication; no full suite yet.
Dependency review found overlapping right-column content: passenger/sensor rows intersected EP33 service buttons. Split the right column into Crew and Services tabs, with input handlers gated to the displayed tab. No game/window launch; native layout acceptance remains owner-only. Client.Tests Release build PASS; evidence dependency-ui-build.log in the current resume run. Correction published before integration-story regressions.
Dependency Engine regression PASS65. Client regression 87 passed/3 failed because service UI fixtures did not select the new Services tab. Updated both fixtures to navigate through the actual tab hit target before preserving all existing ammunition/equipment assertions; no assertions weakened. Build PASS; correction rerun after publication.
Dependency final Client regression PASS90; Engine PASS65. Logs: dependency-client-final.log and dependency-engine-tests.log in D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume. US1 implementation now in progress.
Dependency ordering refinement: US1 content/runtime requires signature profiles and squad identities, while original US11/12 edges referenced all US1. Resolve the semantic cycle by implementing US11 contracts/schema and US12 member contracts after US1 contracts, before US1 content/runtime. Later US11 generation and US12 aggregation consume the real group state; no placeholder groups or duplicate schema. These early tickets depend on US1-TK1, not completion of the entire US1 story.
US1 first boundary: Engine34 PASS, Client5 PASS, Contracts6 PASS/1 assertion failure because optional default ImmutableArray normalizes to empty on JSON load. Corrected assertion to compare full serialized value, preserving all fields. Independent review identified malformed nested sensor acceptance and overflow-prone corridor distance. Added constrained module validation and normalized segment geometry with regressions. Corrective Engine/Contracts builds PASS; tests rerun after publication. Evidence us1-review-build.log and us1-contract-fix-build.log in current resume run.
US1 corrected boundary PASS: Engine36, Contracts7, Client5. US11 boundary PASS: Engine18, Contracts5; independent review found no new actionable defects. Logs us1-Engine-final.log/us1-Contracts-final.log/us1-Client-tests.log/us11-Engine-tests.log/us11-Contracts-tests.log in current resume run. Cross-story capture/reports/motion remain in progress.
US12 boundary Engine22 PASS. Contracts6 passed/1 assertion failure: ImmutableArray equality compared backing arrays after JSON deserialization. Corrected to element-by-element array assertion, preserving exact member identities/types. Build PASS; corrected Contracts boundary follows publication.
