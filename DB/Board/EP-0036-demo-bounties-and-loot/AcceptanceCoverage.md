# Acceptance coverage — EP-0036

All 12 stories / 48 tickets implemented. Automated/source evidence supports implementation; native input, real window/GPU, gameplay balance, visual/product acceptance remain NOT RUN/OPEN for owner inspection. Each ticket serves the story criteria listed in its canonical card.

| Story | Automated and integration coverage |
|---|---|
| US-0001 | Tracking/terms/actual target identity and availability; Engine/Contracts/Client US1 tests; independent review corrected region label. |
| US-0002 | Own victory including unaccepted/cancelled; exact issuer; overflow; one posting; duplicate/Load; due vs cash; US2 tests. |
| US-0003 | Actual remaining stacks, per-wreck scrap, independent destruction, capacity before RNG, legacy catalogs, partial collect and locale; US3 tests. |
| US-0004 | Accepted bounty → victory → partial collect/module/arrest → JSON Load → remaining → close → issuer payment; malformed cross-battle atomic rejection; latest report; actual Load router/F5/F9; US4 tests. |
| US-0005 | 31 current docs plus graph guides; current source inventory, links, Board ID/dependency validation, ticket SHA mapping; independent docs review. |
| US-0006 | Real layout module instances, chance factors, failed terminal attempts, no cargo/no roll, intact sale-only goods, trade names; US6 tests. |
| US-0007 | Per-carrier self-destruct, unique settlement, frozen phase, disposal/checkpoint retry, real screen callbacks and source selection; US7 tests. |
| US-0008 | Persisted named-RNG engineer skill, no legacy grant, ordinary salary/food/cabin, actual crew UI; US8 tests. |
| US-0009 | Arrest identity/provenance, no cabin/no mutation, food not wages, persistent separate roster, actual click, packaged exact-identity portraits and fallback; US9 + EP34 capacity tests. |
| US-0010 | SelfDestructed log enum compatibility, visible journal, source-kind labels, confirmed jettison cost/identity/capacity, pending retry intent; US10 tests. |
| US-0011 | Coarse region only while tracking/available, no hidden NPC position, shared camera and no input mutation; US11 tests. |
| US-0012 | Process-only override, validation/no skill mutation, future rolls only, console exclusive/pending guards and read-only state; US12 + DebugConsole tests. |

Detailed command counts, failures and corrected repeats: [ExecutionStatus](ExecutionStatus.md). Per-ticket implementation/corrections: [Tickets](Tickets.md).

## Owner inspection — NOT RUN

1. New demo: engineer visible, 7 crew/12 cabins, passenger acceptance and separate prisoners count.
2. Accept/cancel a bounty; search region does not track unseen moving pirates. Win without tracking, then claim at exact issuer; repeat fails.
3. Victory: map stays frozen, journal includes per-carrier self-destruct; select Disabled/wreck, scroll RU/EN at UI scales 0.8/1/1.2/1.5.
4. Collect some ammo, try a module/scrap; fill cargo, confirm jettison, retry the same uncertain operation if storage fails.
5. Arrest a pilot; verify same identity/portrait or neutral fallback, prepared kneeling art where exact asset exists, cabin occupancy and rations without prisoner wages.
6. F5 in open loot, F9/Continue: same sources/attempts/prisoners, no combat replay; pending/discard confirmation blocks hotkeys.
7. Leave with leftovers → warning → cancel/confirm; reopen latest report in Contracts after payment and a later non-victory.
8. Console engineer_bonus / loot_force / loot_state; restart process resets overrides.

Prepared art covers four exact female IDs across seven outfits. Missing/male identity uses same source portrait or neutral card; no artwork/native acceptance is claimed. Full economic balance/repair profitability remains EP-0037.
