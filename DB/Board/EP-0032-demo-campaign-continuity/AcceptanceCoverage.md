# EP-0032 acceptance coverage

Implementation is complete; manual/owner acceptance remains OPEN by explicit user instruction. Automated checks do not close native, audio, balance or independent-review gates.

| Story | Implemented boundary and automated evidence | Remaining acceptance |
|---|---|---|
| US-0007 | Immutable contracts; Engine receipts/revisions; Save22 migrations/catalog validation; LocalClient and stale-session lifecycle. Contracts3, Engine10, Client4 at story end | Real user interactions in downstream features |
| US-0001 | Actual owned-wing roundtrip/second sortie and rearm; one settlement; Ship summary. Engine3, Contracts1, Client4 + ShipScreen6 | Play two sorties and inspect losses/rearm |
| US-0002 | Permanent pilot loss, disabled survival, cabin release, memorial load validation, client memorial. Cumulative Engine15, Contracts5, Client10 | Portrait readability, scrolling and pilot loss during actual play |
| US-0003 | Outcome projection, carrier terminal vs no-ready-wing recovery; three injected write-failure stages preserve prior bytes and retry without settlement. Engine20, Contracts6, Client17 | Terminal/recovery flow and disk failure presentation |
| US-0004 | Real Demo scenario five fixed stations, shared starter manifest, tutorial exit/save/load identity/positions, public/development selection. Engine22, Contracts7, Client14 | Demo geography/travel and onboarding playtest |
| US-0005 | Concurrent durable autosave, active-battle rejection, manual slot preservation, corrupt-latest fallback; client adoption/status/failure/retry, menu and terminal input. Engine23, Client126 | Docking/closing/reopening and actual filesystem failure UI |
| US-0006 | Current descriptions, impact inventory, Board IDs/links, publication ledger and graph | Owner acceptance not implied by documentation |

## Manual checklist for the owner

1. New demo campaign: tutorial owns the actual starter wing; exit returns to the five-station world.
2. Lose one drone/pilot, exit, inspect Ship memorial and free cabin, save/load, then start another sortie without resurrecting the pilot or refilling ammo.
3. Surviving carrier without ready wing stays in the world with recovery guidance. Carrier destruction blocks world actions and offers checkpoint/new game/manual Load/menu.
4. Dock and commit outcomes, observe Saved, close/reopen and Continue; corrupt a disposable newest slot and verify fallback. Retry a failed write with F6 without reapplying gameplay.
5. Check RU/EN, UI scale, memorial scrolling and terminal buttons at supported viewport sizes.

A limited native observation before the user's stop instruction verified the main menu and the no-compatible-save message at 1920x1079 on the smaller monitor. No native campaign progression or full acceptance is claimed. The process was stopped; no launches after the instruction.

Final cross-story regression: 105 Engine tests (including all DemoEp0032 and existing save/catalog checks) PASS; Client126 PASS after Continue error correction. Logs in 20261117-regression/after2.log and 20261117-final/client.log under the evidence root.
