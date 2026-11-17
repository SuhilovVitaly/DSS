# EP-0035 review evidence

Independent reviewer: `/root/dependency_review`. Preparatory review read all150 Markdown documents in the EP35/current-documentation snapshot and inspected code/fixtures. This document records findings and source comparisons; final post-suite review remains a separate gate.

## Confirmed EP35 corrections

| Finding | Correction / evidence |
|---|---|
| P1: every shipped demo station chord was inside START8000; ordinary routes could never encounter pirates | a4afd175 moves outer New Game stations16–44k wu, keeps safe mining introduction and captured save geometry. Real passenger/cargo direct-capture versus detour regression172/172 expanded story pass. |
| Pause discarded fractional motion remainder | fdcaa6c4; quarter-remainder theories and US17 Engine185/185. |
| Saved observations varied with snapshot partition | d76bf50f commits observations only at authoritative motion boundaries; US4 Engine96/Client25. |
| Route startup/ETA and blind acceleration mismatch | 61aebacf/3377ff1e use immediate steering and exact undock epoch; real voyage/fuel/load checks. |
| External build output broke source-backed fixtures | 44ab260b adds source-anchored test helper; deployed content remains separately checked through actual Client output. Reviewer caught and corrected accidental configuration-path substitution before publication. |
| Relocated historical fixture consumers derived wrong repository root | 26fe6a56 replaces parent walking in ConnectionApi/LocalSessionBattle/WorldGuard with source root. Focused integration checks run in final-oracles-Engine. |
| Existing starter count/footprint expectations omitted new sensor | 1b00a0e1/054a9401/32ac7b3d include literal sensor cell(3,4), retain hull/nonoverlap/crew/cargo assertions. |
| Old loss oracle omitted surviving drones disabled after final carrier destruction | d94234c1 expects4/6 structural losses and explicitly checks Disabled, Speed0, positive hull. |

## Existing source/test mismatches found by full suite

These are source comparisons with base `0c06524b5e1c9fb631ff6c6dc49ca2da48717a23`, not an executed baseline suite. They are not silently repaired or reported passed.

- WeaponOperatorSchema expects skill31 rejection, while the base loader already accepts0..100.
- Motion ForbiddenCalls applies a historical arithmetic ban to all tactical Client UI; base UI already uses Math.Max/Min.
- SystemCorrectnessCorpus changes generator version to1 but retains V2 distanceScale32.607749; both fixture/config and rejection rule predate EP35. V2 boundary oracles also omit that scale.
- V4 Markdown Data are absent in base and current tree. ContentSync/export requires those sources; shipped JSON cannot substitute for an independent table oracle.
- Old scenario/default/picker/market expectations predate Demo default and current torpedo assortment; their owning source/config is unchanged.
- Portrait canvas/alpha failures concern assets/repository logic unchanged from base.
- Owned battle fixtures already remapped legacy pilot aliases to persistent drone IDs; old BuildCard/FirePanel/MapFire/Hover probes still look up literal old names. BattleEnd enumerates default Loadout even though base owned setup already omitted it.
- DecoyState expected mass excludes12 torpedoes already included by base state/assembly.
- Some manual-save/retry fixtures expect no prebattle continuation directory; base LocalClient already writes that checkpoint.

## Unresolved runtime attribution

PhaseOutcome diagnostic reports victory by NoDrones at turn18, shots29, hits17, losses0:6, carrier hulls356/400 and148/400. Its ancient oracle expects carrier destruction and21/40/20/1:5. The reviewer compared singleton normalization, ordering, RNG, owned composition/skills, AI target selection and characteristics: no pre-carrier-death behavior change was identified. Both surviving carriers rule out the new last-carrier disable as a direct cause. This supports a stale owned-battle oracle but does not prove runtime baseline parity. Numbers/PNGs were not replaced to force green results.

Other scene/raster numeric expectations and second Tutorial/Skin startup have not all been reproduced on base. Ordinary voyage fuel failures are baseline-supported by unchanged code/fixtures but likewise lack an executed baseline comparison. These limitations remain visible in full-suite results and final review.

## Review boundary

No game/window was launched. No independent native, audio, physical-input or product acceptance is claimed. Final epic review must assess the complete base-to-final diff after all authorized automated runs and record its own verdict here or in FinalValidation.
