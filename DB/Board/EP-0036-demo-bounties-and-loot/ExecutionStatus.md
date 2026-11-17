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

Historical preparation baseline (superseded by completion entries below): no implementation was complete at task start.
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

## US6 boundary validation plan

Verified five ticket files; all five published through b8cefc63. Run FullyQualifiedName~DemoEp0036Us0006 in Contracts (identity/chance DTO), Engine (exact chance formula, slots, cargo refusal, terminal attempts, persisted results), Client (mapping/masses, finite market, pending/locale/item naming). Shared US3 loader/presentation behavior changed; include Client US3 filter as targeted regression. Independent review requested; no native game.

## US6 completed implementation boundary

All five tickets published through b8cefc63. Contracts4/4, Engine11/11, Client20/20 (US6+affected US3) PASS. Independent read-only review found no actionable defects in checked salvage/mapping/capacity/market/presentation scope. Map integration remains US7; native acceptance NOT RUN. Evidence under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/: us6-contracts.log, us6-engine.log, us6-client.log and correspondingly named -results/ directories.

## US7 boundary validation plan

Verified all five tickets published through eda4e8df. Engine filter FullyQualifiedName~DemoEp0036Us0007 covers schema, self-destruct ownership, victory/exit/load/pause and real checkpoint failure/roundtrip/autosave. Client filter FullyQualifiedName~DemoEp0036Us0007|FullyQualifiedName~DemoEp0036Us0003Tk0004 covers real screen input/recovery and changed identity-preserving presentation. Then affected bounty tests US1/US2 will verify explicit close-before-world-action lifecycle expectations. Headless only, no game launch. Independent lifecycle/storage/UI review follows.

### US7 completed
All five tickets published. Boundary checks: Engine 13/13 (us7-engine-review.log), Client 14/14 (us7-client-review.log), bounty Engine regression 20/20 (us7-bounty-engine.log), under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/. Independent engine/storage and client reviews found stale victory checkpoint and stale-map recovery defects; both fixed in f6efdaff and reviewers confirmed closure. Native NOT RUN by user instruction. All exact ticket criteria remain subject to final epic regression and owner product acceptance.

## US9 completed implementation boundary
Six tickets published; actual provenance, cabin/ration, arrest and pose paths implemented. Contracts4/4 (us9-contracts.log); Engine6/6 final US9 (us9-engine-final.log), combined affected regression32/33 before corrected explicit-time fixture (us9-engine-repeat.log); Client17/18 before invalid-path correction (us9-client-repeat.log), final9/9 portrait checks (us9-portrait-final.log). Together the corrected affected corpus passes; no whole-suite claim. All evidence under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/. Independent reviews found name/provenance mismatch, packaging omission and per-frame assignment IO; fixes published and both reviewers confirmed closure. Four prepared identities/seven outfits use current repaired assemblies; other missing kneeling assets safely retain original/neutral identity. Native NOT RUN; exact visual acceptance remains owner. Initial failed logs retained honestly.

## US10 completed implementation boundary
Four tickets implemented and published. Engine affected corpus41/42 initially (us10-engine.log), corrected historical enum assertion repeated2/2 PASS (us10-enum-repeat.log). Client22/22 PASS (us10-client.log); independent retry-intent finding fixed ec37b36d and affected retry/lifecycle11/11 PASS (us10-client-review.log). Independent engine review no findings, client reviewer confirmed closure. Evidence in D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/; native NOT RUN.

## US11 completed implementation boundary
Both tickets published. Engine22/22 and Client12/12 PASS including affected bounty regression (us11-engine.log, us11-client.log and matching TRX under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/). Independent Engine and Client reviews found no actionable defect. Static region never reads hidden live coordinates; native visual/readability acceptance NOT RUN.

## US-0012 validation
Engine: 31/31 (us12-engine.log). Client: 77/78 (us12-client.log); existing FinishedTutorialClosesNormally raced asynchronous tutorial journal writer (IOException sharing violation), isolated repeat passed 1/1 (us12-client-repeat.log), no product code change. Both independent reviews found no actionable defects. Native console NOT RUN — owner launch only. Evidence root remains D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/.



## US-0004 and final review

Published 3 tickets and corrections; first Engine boundary 28/28. Client first 9/11: two fixture errors (active-battle save attempt and missing input/render activation); corrected to use real prebattle checkpoint/new candidate session. Follow-up Engine 21/22 and Client 6/7 exposed fixture attempting replacement during captured encounter; corrected fresh-target setup. Final US4 Engine 3/3 (us4-engine-final), Client 4/4 (us4-client-final). Independent whole-epic review found and closed two P2s: conflicting Open A/Pending B Load before publication and stale report after next non-victory. Documentation review exposed F5/F9 loot gate and cabin prisoner summary; fixed in 6291aa73/b6613c23, targeted final checks below.

Scoped dotnet format whitespace ran for changed C# only (format.log/format.err); formatting-only changes do not alter behavior. Current documentation: 31 files, all local links resolve (documentation-links.json), independent wording corrections applied.

## Final thematic validation plan

No full suite. After documentation: Client filter DemoEp0036Us0004|DemoEp0036Us0007|DemoEp0036Us0009|DemoEp0036Us0010|DemoEp0034Us0001|KeysDuringBattleTests checks changed shared loot wrapping/locale/cabin summary and F5/F9 exception without enabling active-battle world keys. These are headless tests (no game window). Contracts filter DemoEp0036Us0003 checks additive loot projection serialization. Engine final US4 3/3 and relevant neighbors above remain valid: no semantic Engine change since pass; no redundant rerun. All processes bounded at 300000ms, outputs in this run root. Final docs/Board/graph static checks and independent final review follow.


## Final thematic result

Final Client: 44/50 initially; six predecessor capacity/locale expectations updated for the new contract, repeat final-capacity-fixed passed 11/11 (five overlaps), covering all 50 original cases successfully. Final Contracts 3/3. Actual headless window F5 writes Open loot and keeps map active; active battle keyboard guards pass. Final scoped format verify succeeds. Both final independent reviewers report no open actionable findings. See FinalValidation.md for exact evidence and remaining owner acceptance. All 48 ticket first implementation SHAs and corrections are mapped in Tickets.md.

## Graph and final static checks

Offline Graphify final build: 33,869 nodes, 86,013 edges, 1,136 communities; 1,275 code and 1,121 Markdown files. All 2,396 input hashes and 6 selected semantic input hashes verified; zero missing sources/out-of-range locations/dangling edges/self-loops. Post-export drift check is empty. One existing parser limitation: ModulePanelLocalizationTests.cs line25 may be partially extracted (not a compiler error). Semantics are selective, token use unmeasured; no image interpretation or external API. HTML aggregates communities instead of rendering 33k individual nodes.

Retained graph evidence: graph/verification.json, graph/input-evidence.json, graph/semantic-coverage.json, graph/ast-warnings.json, graph/GRAPH_REPORT.md, graph/graph.json and graph/graph.html; source hashes include the preserved owner working-tree docs. The graph is navigation, not runtime/native proof. Final static evidence final-static-checks.json verifies current Markdown links and publication ancestry.

## Cleanup and publication verification

Task-owned build outputs, Graphify cache, helper scripts, temporary capacity fixtures, metrics and raster/screenshot outputs were removed. Retained only the referenced log/TRX and graph evidence under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/. Root inspection found no task-created temporary directory in workspace/repository; existing unrelated directories were preserved. No task worktree or running game exists. Final receipt is committed and pushed to the same verified upstream; exact remote HEAD is checked after its commit and reported to the owner.
