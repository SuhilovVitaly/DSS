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
