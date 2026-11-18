# Owner review corrections — 2026-11-18

Review baseline: `7dff221f`. Branch: `feature/ep-0036-demo-bounties-and-loot`, remote `origin` (`https://github.com/SuhilovVitaly/DSS`). Original epic execution authorization covers these corrections and per-ticket publication. Unrelated owner changes in EP-0031, EP-0034 and EP-0041 were preserved.

## Disposition of the nine findings

| # | Result | Implementation / evidence |
|---|---|---|
| 1 | Fixed | Closed ledger validates structure and frozen battle provenance without current loot fingerprint/mappings. Open remains strict. Balance-change roundtrip passes; obsolete closed SKU safely projects historical outcomes. |
| 2 | Fixed | Shared cabin reservation calculation includes active unboarded passenger obligations for ship, passenger, hiring and arrest paths. Job acceptance no longer subtracts them twice. Permanent prisoner reserve remains zero. |
| 3 | Fixed | Encounter content without loot is rejected by configuration validation, before combat. Missing-loot configuration rejects atomically. Bounty content fixtures now include required loot.json. |
| 4 | Not reproduced | ResolveMarketProfiles validates current/exact predecessor fingerprints before continuation staging; economic catalog and current event identity are independently checked. Fallback exists for additive pre-loot migrations. Self-consistent foreign profile, event, catalog and missing baseline inventory cases reject atomically; both supported predecessor migrations preserve economics. Production compatibility logic retained. |
| 5 | Fixed | Missing composition/profile/drone/layout references reject with ScenarioException. Source unit lookup remains Single only after exact unique source-set validation. Missing composition regression rejects without replacing world. |
| 6 | Fixed | Save receipts omit derived Rewards; the authoritative loot ledger is stored once. Live receipt behavior remains unchanged. Restored compact receipts return original command result plus current Rewards/revision without effects. 100 rejected requests grow serialized save by less than 100 KB. |
| 7 | Fixed | Projection computes hold capacities/free mass/free cabins once. Item availability uses capacity arithmetic, preserving per-hold fragmentation, without building candidate module/cargo arrays. |
| 8 | Fixed | Encounter result stores PlayerSide, legacy default A. Outcome, enemy composition, prisoner provenance and frozen map use it consistently. Side-B custody roundtrip and mismatched-side rejection pass. |
| 9 | Partly simplified, validation retained | Removed duplicate loot free-mass helper in favor of PlayerFreeHoldKg; one frozen capture feeds loot and encounter settlement. Engineer/content validation remains at independently callable parsing and engine/configuration boundaries, including programmatically constructed inputs; deleting either boundary would remove protection. |

Independent review additionally found cached successful loot receipt replay could checkpoint during the next battle. LocalClient now checkpoints only a newly applied command; Engine reports that fact atomically under its world lock. Failed pending writes still retry before dispatch. Regression covers restored Finish A replay during active B and atomic replay classification.

## Validation

Evidence root: `D:/DeepSpaceSaga/artifacts/validation/ep-0036-review/20261118-corrections/`.

| Check | Result | Evidence |
|---|---|---|
| Initial US4 load regression | 5/5 PASS | `us4.log`, `us4/*.trx` |
| Initial capacity neighbors | 11/14; three obsolete predecessor expectations | `us9.log`; fixed old crew/reserve counts and ImmutableArray reference equality |
| First full thematic correction set | 94/96; two missing-loot fixtures | `regression.log`; fixtures corrected without relaxing assertions |
| Final Engine thematic set | 96/96 PASS | `final-engine.log`, `final-engine/*.trx` |
| Client affected loot/cabins/report set | 25/25 PASS | `final-client.log`, `final-client/*.trx` |
| Post-review atomic replay change | 5/5 PASS | `atomic-replay.log`, `atomic-replay/*.trx` |
| Scoped whitespace verification | PASS after formatting four test files | `format-final.log`, `format-final.err` |
| Native/game/product | NOT RUN — owner only | No game executable/window launched |

Commands run from DSS: `dotnet test <project> --no-restore --filter <filter> --results-directory <absolute evidence subdirectory> --logger trx`. Each process tree was capped at 300000 ms. Engine filter: `FullyQualifiedName~DemoEp0036|FullyQualifiedName~DemoEp0034Us0002|FullyQualifiedName~DemoEp0034Us0006Tk0003`. Client filter: `FullyQualifiedName~DemoEp0036Us0004|FullyQualifiedName~DemoEp0036Us0007|FullyQualifiedName~DemoEp0036Us0009|FullyQualifiedName~DemoEp0036Us0010`. Final atomic filter: `FullyQualifiedName~DemoEp0036Us0007Tk0005Tests`. Builds are included in test invocations. No full solution test suite or performance claim.

## Publication and documentation

All listed commits were pushed immediately to the same origin branch; history was not rewritten.

| Ticket | Functional corrections | Final test formatting |
|---|---|---|
| EP-0036-US-0004-TK-0001 | `0f8e0602`, `c5fefc03` (superseded projection), `22c05506` | `9cbdca3c` |
| EP-0036-US-0009-TK-0003 | `003a7223`, `183888c7` | `280efe7f` |
| EP-0036-US-0009-TK-0002 | `cc2153a6` | `eb8d56a3` |
| EP-0036-US-0007-TK-0005 | `e2b74c63`, `48ffca48`, `1a9f5788`, `7b65d662`, `8ae02667` | — |
| EP-0036-US-0003-TK-0003 | `805b9e50`, `fe3b257d` | — |
| EP-0036-US-0003-TK-0002 | `6db8910c` | `f9cbf543` |

Current contracts updated in [DemoRewards](../../Documentation/04-Engineering/DemoRewards.md), [SaveFormat](../../Documentation/04-Engineering/SaveFormat.md), [mechanics](../../Documentation/02-FirstRelease/Mechanics/DemoBountiesAndLoot.md). Documentation ticket US5 TK1 owns these updates; US5 TK2 owns final publication/graph evidence in [FinalValidation](FinalValidation.md). Earlier execution evidence remains historical and is superseded for these changed paths by this report.
