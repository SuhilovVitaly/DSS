# EP-0036 execution

## Scope and authority

2026-11-17: the owner explicitly requested execution of this entire epic under
[EpicExecutionPrompt](../EpicExecutionPrompt.md). This supersedes planning-only
authorization statements; it does not establish product acceptance.

- Branch: `feature/ep-0036-demo-bounties-and-loot`.
- Remote: `origin`, `https://github.com/SuhilovVitaly/DSS`.
- Fresh main base: `e59d6c3ce5c137dcfd2b170bfa635eefe188c9d5` (EP-0035 merged).
- Current scope: 12 stories, 48 tickets; US-0005 documentation runs last.
- Initial tree contained the owner's expanded EP-0036 plan and kneeling assets.
  These are inputs, not implementation evidence. Unrelated Board edits are retained.
- Game/native/UI execution: NOT RUN, prohibited by the owner.
- Tests run only at completed story boundaries and on the final relevant diff.
  Ticket boundaries use static checks and separate immediate commit/push.

## Order and verification

Bounty: US1 TK1, TK5, TK4, TK2, TK3; then US2 TK1, TK2, TK4, TK3.
Loot follows ticket dependencies: shared contracts before schema/content; engineer
identity before module attempts; module and consumable settlement before the
postbattle lifecycle; custody and presentation before continuation verification.
US5 follows every functional story. Cross-story dependencies do not require an
entire prerequisite story when the ticket explicitly depends on one schema ticket.

Planned story filters are the corresponding `DemoEp0036UsNNNN` classes in the
owning Contracts, Engine or Client test project, with neighboring save/encounter/
transport tests selected from the actual diff. No full project suite is implied.
Each test process is capped at 300000 ms and writes under the sole validation root.

## Publication and acceptance

Preparation in progress. No implementation ticket completed or tested yet.
Ticket SHA/push evidence will be recorded after publication; a commit never embeds
its own predicted SHA. Independent preflight review is in progress.

Owner inspection after implementation must cover bounty tracking/payment at the
issuer, postbattle map selection and exit warning, module/consumable/scrap actions,
cargo jettison, prisoner portraits/cabins/rations, engineer display, and Load during
loot. These scenarios remain NOT RUN until the owner runs the game.

## US1 completion — 2026-11-17

Published tickets: TK1 813fad2d; TK5 13e1fc38; TK4 d280058e; TK2 7b1651ea; TK3 08719981. Independent review found missing search-region text; fixed and pushed e97a9230. Initial thematic tests: Contracts 3/3, Engine 17/17, Client 7/7. After correction: Engine bounty 13/13, Client bounty 7/7; unchanged Contracts/neighbor cases were not repeated. Logs and TRX: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us1-*-tests.log and us1-review-*-tests.log, matching results directories. Native remains NOT RUN. All pushes to origin feature branch confirmed by push results. US2 payment and US11 map geometry remain pending.

## US2 completion — 2026-11-17

Published: TK1 d0c6a409; TK2 f8c687be; TK4 b80b8fb1; TK3 3fa67841. Combined bounty US1/US2 thematic checks PASS: Contracts 5/5, Engine 20/20, Client 11/11. These include live local-session claim/reopen for both unaccepted and cancelled targets, overflow and corrupted posting rejection. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us2-*-tests.log and matching results directories. Independent review of a37646de..3fa67841 found no actionable defects. Native NOT RUN.

## US8 completion — 2026-11-17

Published TK1 893e7ebd, TK2 1a57a0a2, TK3 04e35b79, TK4 edb3c880, TK5 bb04160c. Corrections: test analyzer 3f1b9201; independent review role-parser consistency b3de91b7. Thematic US1/US2/US8 checks: Contracts 8/8, Engine 26/26, Client 16/16 (after analyzer correction). Post-review engineer-only Engine 8/8. Evidence under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/: us8-Contracts-tests.log, us8-Engine-tests.log, us8-Client-repeat.log, us8-review-Engine.log and matching TRX directories. Native NOT RUN. Next: shared loot contracts/schema/content, module salvage and lifecycle.

## US3 boundary validation plan

All four US3 tickets published. Run Contracts/Engine/Client filters FullyQualifiedName~DemoEp0036Us0003 (contracts, additive catalogs/profile migration, loot ledger/cargo, selected panel/pending). No whole-project test execution. Integration of real map input and durable LocalClient checkpoint remains US7/US4 and will receive separate boundary checks. Current Engine test compilation also compiles authored future-story fixtures but does not execute them.

### Validation scheduling correction

The preceding US3 boundary count was incorrect: US3 has five tickets, including TK0005 localized feedback. The Contracts3/Engine9/Client6 checks were therefore run prematurely after four tickets, contrary to the requested story-only schedule. These are actual focused results, not evidence that the whole story was complete. Complete/publish TK0005 before the final story review and newly affected locale check; no further premature test execution.

## US3 completed implementation boundary

All five tickets and corrections published through 050046c3. Contracts3/3 unchanged; final Engine10/10 and Client10/10 PASS. Independent review identified incomplete consumable/module coverage; corrected before boundary closure. Content schema error and test assertion/nullable issues corrected in separate immediately pushed commits. Full map/session-checkpoint integration remains allocated to US7/US4; no native/product acceptance claimed.

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us3-contracts.log; us3-engine-reviewed.log and us3-engine-reviewed-results/; us3-client-complete.log and us3-client-complete-results/. All paths share the same absolute run directory. Prior scheduling deviation recorded above.
