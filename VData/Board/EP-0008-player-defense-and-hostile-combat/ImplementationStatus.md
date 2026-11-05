# EP-0008 — execution evidence

## Актуализация EP-0008 — 2026-10-12

Текущий опубликованный baseline: `904a21f1602358c8754d746f092858c920413b6f` (US-0013-TK-0001).44/45 исходных тикетов и коррекция A09 опубликованы. Финальный шаг EP-0008 — US-0013-TK-0002 аудит ссылок/Board; Graphify SKIPPED по указанию пользователя. После проверенной публикации этого шага начинается EP-0009. Полный Debug4314/4314 до коррекции; свежий Release4201/4201 после неё, без114 неизменённых corpus cases. Native13/14, FPS/human OPEN; новые игровые/native запуски запрещены текущим указанием пользователя. Записи ниже — хронологическая история, старые blockers/next-ticket не текущий статус.


## Resumption — 2026-10-11

The user explicitly authorized completing EP-0008 and then EP-0009 autonomously,
with a separate commit and immediate verified push for every ticket. The active
branch remains `base-fight`, upstream `origin/base-fight`, remote
`https://github.com/SuhilovVitaly/DSS`.

The old TK-0002 publication blocker below is historical and **resolved**:
`git ls-remote --heads origin` and `git fetch origin` confirmed remote
`base-fight` at `99e15a2414b753d91eca9a2cbbe0a1309cb23c80`;
`git merge-base --is-ancestor 02c89d4a51687ed291a11285a8b704991b42cf20 origin/base-fight`
returned 0. Both existing contract tickets are published. No duplicate
implementation or empty ticket commit is needed.

Next implementation ticket: **EP-0008-US-0001-TK-0003-weapon-content-schema**.
The remaining 42 tickets, story/epic reviews and native acceptance remain open.
US-0013 already supplies the mandatory final documentation and Graphify story,
with dependencies on all preceding stories. EP-0009 starts only after the
EP-0008 dependency is satisfied.

Initial unrelated/uncommitted input is the untracked `DB/Board/EP-0009-battle/`
folder (user specification and baseline report); it is excluded from this
EP-0008 resumption commit. The historical documentation-reading/test evidence
below is not a claim that the current revision has passed those checks.

This resumption change only corrects publication evidence. Production and test
sources have not changed; current validation is recorded separately below.

Execution started 2026-10-07 under [EpicExecutionPrompt](../EpicExecutionPrompt.md).

## Baseline and authorization

- Initial HEAD: `6907c58bb7e86dc6cc00122567bd4329ba852626`.
- Branch/upstream: `base-fight` / `origin/base-fight`.
- Verified remote: `https://github.com/SuhilovVitaly/DSS`; `git ls-remote origin refs/heads/base-fight` matched initial HEAD.
- Pre-existing untracked files: the target EP-0008 folder, `DB/Board/EpicExecutionPrompt.md`, and unrelated `DB/Board/EP-0001-trading-system/ImplementationStatus.md`.
- The request authorizes implementing draft tickets, necessary scope updates, and one commit/push per completed ticket. Unrelated files remain excluded.
- Current source save version is **15**; EP-0008 plans version **16**. Older documentation referring to 11/12 is historical evidence.

## Preparation

- Root/process guides and execution prompt read.
- Documentation and epic inventory: all 131 Markdown files read completely in six semantic batches; reading evidence is in `D:/DeepSpaceSaga/ep8-validation/`. Truncated output was reread in smaller batches.
- Board preflight: 13 stories, 44 tickets, 60 Markdown files; dependency DAG is acyclic, no missing dependencies or broken relative links after Windows path normalization.
- US-0013 already covers the final project-wide documentation audit and depends on all functional/acceptance stories.
- Existing Graphify output lives in `src/graphify-out/`; the final tooling ticket must use this actual path. Graph output is navigation evidence, not runtime proof.
- Windows sandbox command startup fails with `helper_unknown_error: setup refresh had errors`; approved elevated execution succeeds.

## Baseline checks

| Check | Observed result |
|---|---|
| Contracts project, `dotnet test ... --no-restore` | PASS: 161/161 |
| Solution baseline test run | PASS 3859/3859: Contracts 161, Motion 141, Engine 1760, Client 1735, EconomyBalance 58, Performance 4 |
| Contracts after TK-0001 / TK-0002 | PASS 165/165 / 170/170 |
| Whole solution build after both contract tickets | PASS, 0 warnings/errors |
| Native acceptance | NOT RUN |

The solution run uses `dotnet test DeepSpaceSaga.sln --no-restore --logger "trx;LogFilePrefix=ep8-baseline" --results-directory D:/DeepSpaceSaga/ep8-validation/baseline-tests`.

## Ticket publication

Preparation commit `198d20a76e8812a0a2c1e268b5613daa2ad1284d`: pushed and verified on origin/base-fight.

| Ticket | Commit | Push |
|---|---|---|
| EP-0008-US-0001-TK-0001 | `4233f52346b7d77edc4e04d677f52d7328353fda` | Verified on origin/base-fight |
| EP-0008-US-0001-TK-0002 | `02c89d4a51687ed291a11285a8b704991b42cf20` | BLOCKED: four GitHub receive-side Internal Server Error responses |

TK-0002 defense-contract is implemented and validated but not published. No story is complete. Runtime formulas and the remaining 42 implementation tickets, native acceptance and final documentation/graph work remain open. Planning/readiness checks do not establish gameplay acceptance.

## Publication blocker — 2026-10-07 18:12 Asia/Jerusalem

Four pushes of TK-0002 to the verified `origin/base-fight` failed with remote `Internal Server Error`, including a retry after independent solution verification and one using `git -c http.version=HTTP/1.1 push origin HEAD:refs/heads/base-fight`. The remote remains `4233f52346b7d77edc4e04d677f52d7328353fda`; the local TK-0002 commit is intact. No force push, remote change, rollback or published-history rewrite was attempted.

Server request IDs: `E852:267251:1924F4:20D0A1:6AC660A9`, `D415:40AB9:18F7FF:20A2B6:6AC660D1`, `F50A:1D648:195FA3:2123D9:6AC6612E`, `CD42:14BCD9:1977BF:214355:6AC6615B`. Final responses saved in `D:/DeepSpaceSaga/ep8-validation/tk0002-push-retry.log` and `tk0002-push-http11.log`.

EpicExecutionPrompt requires successful publication before proceeding to the next ticket. Therefore TK-0003 has not been started. The next action is to retry the existing TK-0002 commit's push, verify the remote SHA, then continue the dependency sequence. This blocker note and the matching ticket note are local uncommitted evidence; production/test changes are committed. The epic is **PARTIAL / BLOCKED ON PUBLICATION**, not complete.

## Execution scope notes

- Required new weapon schema fields need matching real catalogs and affected fixtures in the same validated change. Record exact ownership/scope in the applicable tickets before implementation; do not publish a loader that rejects the shipped catalogs.
- Reviews performed by the implementing agent are self-review, not independent review or user APPROVED.

## EP-0008-US-0001-TK-0003 — 2026-10-11

Schema and strict validation implemented; see the ticket for exact scope. Focused 83/83 and Engine non-corpus regression 1721/1721 PASS; solution build, scoped format, diff and Board links PASS. Initial stale copied-content failure resolved by rebuilding Client. Unrelated long map corpuses were explicitly deferred to final epic regression, not declared passed. Native remains NOT RUN. Current Contracts recheck: 178/178 PASS.

Preparation/evidence correction `db78b314a7cc25dc77a17411a676253c95674881` pushed and live remote SHA verified. TK-0003 SHA is resolved after commit; proceed to TK-0004 only after successful push and remote verification.

Continuation reused the prior complete documentation/Board reading at preparation `198d20a` and reviewed the 34 changed/new Markdown documents since then; the current Documentation inventory is 78 Markdown files. New evidence is separated from the historical tests above. Graphify query of ModuleTypeDefinition/CountermeasureContentLoaderTests confirmed registry and loader navigation; the 1200-token graph traversal was truncated and used only to locate primary files, which were inspected directly.

## EP-0008-US-0001-TK-0004 — 2026-10-11

Catalog source/output and shared-owner checks implemented. Full Client **1761/1761 PASS**, Client build (0 warnings/errors), scoped formatting and diff checks PASS. The JSON fields were necessarily delivered with strict schema TK-0003; this ticket separately validates real shipped catalog behavior.

TK-0003: `f5285d87466a08bf369d706badda98b0de4ea757`, push verified on origin/base-fight. Next after TK-0004 publication: TK-0005 chance resolution. Native and final map corpus gates remain open.

## EP-0008-US-0001-TK-0005 — 2026-10-11

TK-0004 `08abe0a2ef55ab381f8cd0a53003c86c9e7d533b` pushed and live SHA verified.
TK-0005 implements the formula and immutable launch captures. Engine non-corpus
1728/1728, final focused19/19, Client1761/1761, build/format/diff PASS.
See ticket for initial failures and fixes. Publication must be verified before US-0002.

US-0001 self-review: schema, shared shipped catalog, DTO roundtrip and actual
Fire -> interception -> real-file Save/Load cover AC-0001/0002/0003 at this layer.
Runtime range/manual Fire/lifetime and current-format-only guards remain assigned
to their downstream tickets; native and final corpus gates remain OPEN.

## EP-0008-US-0002-TK-0001 — 2026-10-11

Previous TK-0005 published as `70ddbc40b20d3209ccfc56b5481997f5677d08dc`; live SHA verified. Dual-defense/three-cabin player data implemented. Client1762/1762 and non-corpus Engine1728/1728 PASS; solution build/format/diff PASS. Details and corrected obsolete fixtures in ticket. Native/final corpus OPEN. Next: publish this ticket, then dual-bootstrap TK-0002.

## EP-0008-US-0002-TK-0002 — 2026-10-11

TK-0001 `42425af97885d10763d39eb3ef469e55d407316c` published and remote verified. TK-0002 production behavior confirmed by new tests: focused17/17, non-corpus Engine1731/1731, build/format/diff PASS. US-0002 self-review complete; native/final corpus OPEN. Next after publication: US-0003 shared-launch validation.

## EP-0008-US-0003-TK-0001 — 2026-10-11

Previous TK-0002 `ec24573d5ae1b7aa5d6e937f19e7f7cbdc33de5e` published/remote verified. Shared validated launch and range implemented, public ownership retained, no AI player receipts. Engine1750/1750 PASS; Client full1761/1762 followed by final affected14/14 PASS after correcting selection fixture. Build/format/diff PASS. Initial fixture failures and exact scopes documented in ticket. Native/final corpus OPEN. Next after publication: hostile-fire scheduler.

## EP-0008-US-0003-TK-0002 — 2026-10-11

TK-0001 `ccf67af0f8019d42ccadefe576397a898a9d852e` published/remote verified. Hostile fire scheduler implemented; full non-corpus Engine1762/1762 PASS, final review70/70 PASS after exhausted-ID fix, affected Client30/30 PASS. Full Client initial13 failures resolved in affected regression; no clean repeated full Client claim. Solution build/format/diff PASS. Next after publication: range feedback UI. Native/final corpus OPEN.

## EP-0008-US-0003-TK-0003 — 2026-10-11

TK-0002 `e33ba130e62ae8dd84439274207b5a48d1ed316d` published/remote verified. Range-feedback UI and shared Motion range math implemented. Full Client1770/1770, Motion9/9, Engine32/32 PASS; build0 warnings/errors, scoped format/diff PASS. US-0003 self-review complete. Next after publication: US-0004 hostile-motion contract. Native/final corpus OPEN.

## EP-0008-US-0004-TK-0001 — 2026-10-11

US-0003 TK-0003 `d21057c0d26b3f4b3e372117ec74e6310cc36789` published/remote verified. Hostile motion DTO implemented; Contracts182/182, build/format/diff PASS. Next after publication: relative-orbit math. Native/final corpus OPEN.

## EP-0008-US-0004-TK-0002 — 2026-10-11

TK-0001 `9424ad264b60bab32eabbef9f1f41b3fb6cde9fc` published/remote verified. Shared relative-orbit math implemented; full Motion162/162, expanded review14/14, build/format/diff PASS. Next after publication: authoritative controller. Native/final corpus OPEN.

## EP-0008-US-0004-TK-0003 — 2026-10-11

TK-0002 `96e7113ded5f2bb1b88458ec543237dac87f73d9` published/remote verified. Authoritative hostile movement wired; final affected Engine281/281, Client14/14, solution build/format/diff PASS after documented initial fixture failures. Next after publication: swept combat integration. Native/final corpus/current-format hostile persistence OPEN.

## EP-0008-US-0004-TK-0004 — 2026-10-11

TK-0003 `6658a78c7f28c286f919740ec21b03056d064939` published/remote verified. Swept motion/guidance/PR/death integration verified: focused5/5, full non-corpus Engine1779/1779, full Client1770/1770, build/format/diff PASS. Review added missing selected-info TK-0005, now45 tickets and0 broken normalized Board links. Next after publication: TK-0005, then US-0005. Native/final corpus/current-format hostile persistence OPEN.

## EP-0008-US-0004-TK-0005 — 2026-10-11

TK-0004 `56f5b53c1425e0fc603ad2d2e2a19dd8aea627dc` published/remote verified. Missing selected-info delivered; affected Client58/58, expanded10/10, build/format/diff PASS. US-0004 automated implementation and self-review complete. Next after publication: US-0005 manual countermeasure guidance. Native/final corpus/current-format hostile persistence OPEN.

## EP-0008-US-0005-TK-0001 — 2026-10-11

US-0004 TK-0005 `9548761b9084755ccefc8f791edf762ac61190e9` published/remote verified. Manual geometry and bounded no-solution pursuit implemented; full Motion170/170, build/format/diff PASS. Next after publication: manual Engine command. Native/final corpus OPEN.

## EP-0008-US-0005-TK-0002 — 2026-10-11

TK-0001 `6b0d1f29d74c78c19a721df8e081466f25102333` published/remote verified. Manual Engine fire implemented; full Engine1799/1799, final manual21/21, affected Client20/20, solution build/format/diff PASS. Next after publication: command catalog, then authoritative preview. Lifetime scheduler/new-format validation/native remain downstream.

## EP-0008-US-0005-TK-0003 — 2026-10-11

TK-0002 `83166f856df987c718d97a6eec5de4f92ef197a3` published/remote verified. Manual Fire catalog and category binding implemented; Client1780/1780, affected Engine93/93, build/format/diff PASS. Next after publication: authoritative preview. Native/final corpus OPEN.

## EP-0008-US-0005-TK-0004 — 2026-10-11

TK-0003 `a4091e3f72ed06d0c89766352fb840a2f05b0d92` published/remote verified. Authoritative per-module selected-target preview implemented; Engine1808/1808, Client1780/1780, expanded review10/10, build/format/diff PASS. US-0005 self-review complete. Next after publication: US-0006 global lifetime. Native/final corpus OPEN.

## EP-0008-US-0006-TK-0001 — 2026-10-11

US-0005 TK-0004 `04357623f643e10c798bf5a348e8b46353e8aded` published/remote verified. Global flight lifetime and capped miss coast implemented; Engine1819/1819, affected Client68/68, final review10/10, build/format/diff PASS. Next after publication: auto launcher arbitration. Native/final corpus OPEN.

## EP-0008-US-0006-TK-0002 — 2026-10-11

TK-0001 `862fb97810c54f255a2ed874af7ca0f763dea1b6` published/remote verified. Auto launcher arbitration implemented; Engine1832/1832, affected Client68/68, build/format/diff PASS. Next after publication: journal facts. Native/final corpus OPEN.

## EP-0008-US-0006-TK-0003 — 2026-10-11

TK-0002 `e606480fbc22f726b65f102746cb08186a012388` published/remote verified. Journal facts implemented; initial full Engine1836/1837 corrected obsolete event expectation, final affected131/131 and Client68/68 PASS, build/format/diff PASS. US-0006 self-review complete. Next after publication: US-0007 independent module panels. Native/final corpus OPEN.

## EP-0008-US-0007-TK-0001 — 2026-10-11

US-0006 TK-0003 `9d6a26ecf0be0e9c84d9831450775f44f9878f96` published/remote verified. Instance-panel API/layout implemented; Client1786/1786, final affected125/125, build/format/diff PASS. Actual screen owner/callback wiring is next TK-0002. Native/final corpus OPEN.

## EP-0008-US-0007-TK-0002 — 2026-10-11

TK-0001 `5dfef548496649749f274cc884dff0db7e5a51c7` published/remote verified. Real per-module manual/toggle command routing implemented; initial Client1793/1796 fixed obsolete singleton lookup, final affected152/152, build/format/diff PASS. Real paused LocalSession two-launch flow passes100%/150% scale. Next after publication: combat info. Native/final corpus OPEN.

## EP-0008-US-0007-TK-0003 — 2026-10-11

TK-0002 `300a4e561be2fe53513ed558c060a0c7d3aee0b2` published/remote verified. Per-module combat info and selected PR captured facts/physical lifetime implemented; Client1808/1808, build/format/diff PASS. Next after publication: localization. Native/final corpus OPEN.

## EP-0008-US-0007-TK-0004 — 2026-10-11

TK-0003 `df38090d8cdb31bcc3d5dc54638a0c92ceaf4de3` published/remote verified. Localization and actual UI consumers implemented; Client1811/1811, Russian23/23, build/format/diff PASS. US-0007 self-review complete. Next after publication: US-0008 manual preview geometry. Native/final corpus OPEN.

## EP-0008-US-0008-TK-0001 — 2026-10-11

US-0007 TK-0004 `74248c9f51fc1641548cb2f741a5c658f451e4a7` published/remote verified. Manual hover geometry implemented on the real render path; Client1818/1818, Engine1837/1837, final affected38/38, build/format/diff PASS. Next after publication: hover ranges and physical map countdown. Native/final corpus OPEN.

## EP-0008-US-0008-TK-0002 — 2026-10-11

TK-0001 `fcbb0811eb67fa36fb58591690731aee10004675` published/remote verified. Hover-only weapon rings and physical map flight countdown implemented; initial Client1822/1825 fixed new zoom-boundary test assumptions, final affected32/32, build/format/diffPASS. Next after publication: journal presentation. Native/final corpus OPEN.

## EP-0008-US-0008-TK-0003 — 2026-10-11

TK-0002 `499074810ba3e29e122ed2b55fe4c5410348fe5b` published/remote verified. Complete journal facts/scrolling and real-file expiry history replay guard verified; Client1832/1832, build/format/diffPASS. US-0008 self-review complete. Next after publication: US-0009 authoritative defeat. Native/final corpus OPEN.

## EP-0008-US-0009-TK-0001 — 2026-10-11

US-0008 TK-0003 `f22c723d92061e7f1acc6124b6940dd66eeee1a0` published/remote verified. Terminal outcome/save-permission contract implemented; Contracts185/185, solution build/format/diffPASS. Next after publication: actual Engine defeat transition, then interval/clock cutoff. Native/final corpus OPEN.

## EP-0008-US-0009-TK-0002 — 2026-10-11

TK-0001 `a4b8a71b1b80be55f0ad948e9dccd4d47ab4d0b2` published/remote verified. Real Engine defeat transition and central gameplay/save guards implemented; Engine1842/1842, affected Client130/130, build/format/diffPASS. Next after publication: dedicated clock terminal latch and complete time-boundary regressions. Native/final corpus OPEN.

## EP-0008-US-0009-TK-0003 — 2026-10-11

TK-0002 `f7b8ecd753e17f5c762ffbea086bb8033becd944` published/remote verified. Terminal clock latch and large/split interval proof implemented; initial Engine1844/1845 corrected new internal-array test serialization, final affected113/113 PASS, solution build/format/diffPASS. US-0009 self-review complete. Next after publication: US-0010 file-save transport guard. Native/final corpus OPEN.

## EP-0008-US-0010-TK-0001 — 2026-10-11

US-0009 TK-0003 `bd10a68f565846c57a973c9d4bf07517a970f78c` published/remote verified. Actual saveGate/filesystem guard and fresh-session recovery verified; affected Engine17/17, build/format/diffPASS. Existing production ordering retained. Next after publication: defeat screen. Native/final corpus OPEN.

## EP-0008-US-0010-TK-0002 — 2026-10-11

TK-0001 `63eb4d35c2c3b9ea9f9da56bb55dfdc36a96b406` published/remote verified. Defeat screen/layout implemented with exactly two actions and actual UI scaling; Client1837/1837, final affected12/12, build/format/diffPASS. Next after publication: actual terminal navigation/keyboard/save guards. Native/final corpus OPEN.

## EP-0008-US-0010-TK-0003 — 2026-10-11

TK-0002 `197077f03454d00a063f1b6f228c02da4c9ea316` published/remote verified. Actual window terminal/modal/Load/MainMenu/save/keyboard integration implemented; Client1843/1843, solution build/format/diffPASS. US-0010 self-review complete. Next after publication: US-0011 version16 and strict combat continuation. Native/final corpus OPEN.

## EP-0008-US-0011-TK-0001 — 2026-10-11

US-0010 TK-0003 `332f0c788ee2767df472e2c440f918c7a259fe49` published/remote verified. Version16/current-only real save gate and hostile payload DTO introduced; initial Engine1845/1871 obsolete version expectations repaired, final affected233/233; full Client1843/1843, build/format/diffPASS. Next after publication: strict combat/hostile payload validation. Native/final corpus OPEN.

## EP-0008-US-0011-TK-0002 — 2026-10-11

TK-0001 `857a251ccd05239fb2fd9c6f0769ef7df1bf662d` published/remote verified. Strict defense/hostile capture-validation and staged restore implemented; initial Engine1884/1903 and1902/1903 regressions repaired, final affected202/202; full Client1843/1843, solution build/format/diffPASS. Next after publication: real-file multi-phase continuation. Native/final corpus OPEN.

## EP-0008-US-0011-TK-0003 — 2026-10-11

TK-0002 `af510f1800b10e4c9376c262cddd75c0c6550eba` published/remote verified. Eleven actual-file continuation cases implemented; initial Engine1915/1916 corrected new fractional-contact test assumption, final affected254/254 PASS; build/format/diffPASS. Production atomic restore already delivered with prerequisite schema. Next after publication: localized recoverable Load errors and client roundtrip. Native/final corpus OPEN.

## EP-0008-US-0011-TK-0004 — 2026-10-11

TK-0003 `c195d6ac4f91cc536e6a0dd2e54739b0bbaca8bd` published/remote verified. Localized recoverable Load/F9 errors and six real-session/file/UI continuation tests implemented. Client1849/1849, Russian16/16, solution build/format/diffPASS. US-0011 self-review complete. Next after publication: US-0012 compatibility fixtures and complete validation. Native/final corpus OPEN.

## EP-0008-US-0012-TK-0001 — 2026-10-11

US-0011 TK-0004 `222e5bb45063c60e5a689ee5e854f2e9c8867ab6` published/remote verified. Five remaining formula/phase fixtures aligned without weakening motion/UI/serializer assertions. Full solution4302/4302 with all Engine corpus (2030/2030), skipped0; final Contracts185/185, Motion170/170 and affected Client3/3, solution build/format/diffPASS. TRX and log at `D:/DeepSpaceSaga/ep8-us12tk1-results` and `D:/DeepSpaceSaga/ep8-us12tk1-solution.log`. Next after publication: full generated-scenario battle acceptance. Native OPEN.

## EP-0008-US-0012-TK-0002 — 2026-10-11

TK-0001 `0cdcc7af7b774096a307fc5520e92f4ac12acffc` published/remote verified. Generated PlayerShipOnly actual-session battle, real-file continuation/recovery, three-seed pause/x100 partition determinism and two-hostile0/100% custom-weapon matrix implemented. New6/6 and Engine1922/1922 PASS; scoped format/diffPASS. Next after separate publication: native UI matrix. Native OPEN.

## Publication blocked after US-0012-TK-0002 — 2026-10-11

Local commit `9c93e1d227ac32eb8319ce28dc9eb7c68cbea73c` completed but both push attempts were rejected by automatic approval review, including after authenticated GitHub owner/public/push permission verification. Live remote remains `0cdcc7af7b774096a307fc5520e92f4ac12acffc`. Explicit human destination authorization is required by that reviewer. Existing user instruction authorizes autonomous per-ticket pushes, but the reviewer did not accept it without the URL. No bypass attempted.

Do not start TK-0003 or EP-0009 until the existing commit is pushed and the remote SHA is verified. Native/final review/project documentation/Graphify remain OPEN. Exact ticket SHA registry and recovery instructions: [PublicationStatus.md](PublicationStatus.md).

## Publication recovery — 2026-10-12

Explicit user confirmation names GitHub SuhilovVitaly/DSS and base-fight. `git push` reports already up to date; live remote SHA `817a856047589f7346c2e280450df3c28372c4b5` includes ticket `9c93e1d`. Publication blocker resolved; continuing US-0012-TK-0003. Existing Data commit preserved.

## EP-0008-US-0012-TK-0003 — 2026-10-12

Implementation and self-review complete, pending separate publication. Real click/session viewport matrix6/6 and full Client1855/1855; solution build0 warnings/errors; scoped format/diff PASS. Native artifacts retained:13/14 interaction cases complete; 1920x1080/UI150 combat incomplete, timing target not accepted. Footer/PR label overlaps fixed. User defers further game/native runs and authorizes continuation through review/docs and EP-0009; native/human acceptance remains OPEN. [Evidence](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/PhaseThreeCombatAcceptance.md).

## US-0009-TK-0003 review correction — 2026-10-12

TK-0012-0003 published as402c3757e354641bdc92d165d5a1d7df8cf7e6fe (remote verified). Final review found terminal integer time rounded before a fractional death, violating A09. Regression reproduced5001 vs expected5002; ceiling correction passes196 affected Engine tests, build/format/diff. Separate correction publication follows. Full solution run begun before correction remains separate evidence; no native/game runs.

## EP-0008-US-0012-TK-0004 — 2026-10-12

Correction9ab6eedcf51aea3515dbf2bab1ea3f4d58786dea published/remote verified. Final self-review complete with Q01–Q51 and all story AC traced to actual code/tests. Full Debug4314/4314 before rounding correction; fresh Release4201/4201 excluding114 unaffected corpus checks after correction; build0 warnings/errors, diff PASS. No unresolved confirmed runtime finding. Native/human gates remain OPEN by user deferral (13/14 native cases, frame-time target not accepted). Documentation/Graphify follow; this review is not user approval. Separate publication follows.

## EP-0008-US-0013-TK-0001 — 2026-10-12

43 Markdown files synchronized across canonical requirements, mechanics/screens, historical reports, related EP6/7 summaries, Board and performance tooling. New Defeat screen contract and durable document inventory. Relative links,13/45 counts, DAG, publication ancestry and diff checked. No runtime code change. Next after publication: Graphify rebuild; native/human acceptance remains OPEN.

## EP-0008-US-0013-TK-0002 — 2026-10-12

Documentation904a21f published/remote verified. User cancelled Graphify rebuilding to avoid token expenditure; semantic agents stopped, existing graph unchanged, .graphifyignore restored. Graph gate SKIPPED by user instruction. Relative links,13 stories/45 tickets and DAG audited. Implementation/documentation chain delivered under revised scope; native13/14, FPS/human remain OPEN. User authorizes EP-0009 continuation without game/native/graph launches. Separate publication follows, own SHA recorded through Git after creation.

## Superseded by EP-0009 — 2026-10-12

EP-0008 implementation and publication evidence above remains historical. Its Hull HP, attack/interception formulas, one-flight/auto-attempt rules and SaveFormatVersion16 were superseded by [EP-0009 §2.1](../EP-0009-battle/EP-0009-module-combat.md#21-переопределяется-правила-ep-0008-перестают-действовать). Current engineering contract: [ModuleCombat](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/ModuleCombat.md). EP-0008's unfinished native/FPS/human gates remain historical OPEN; EP-0009 native checks are separately NOT RUN by user instruction.


## Superseded by EP-0012 — 2026-10-21

Guided/chance/mode/auto/burst combat and Save17/16 rules in this dated report are superseded by [point-aim combat](../../Documentation/04-Engineering/PointAimCombat.md). Module armor/structure, report delivery and observed-motion privacy remain. Historical SHA/test/FPS/native results and original permissions are preserved as evidence of their time, not fresh acceptance. EP12 actual native matrix does not close earlier human/palette/FPS gates; production dodge balance remains outside its required corridors. [Current implementation and publication](../EP-0012-combat-rebuild/ImplementationStatus.md).
