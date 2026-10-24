# EP-0012 production balance — 2026-10-21

Status: **OUTSIDE CORRIDOR / author balance decision OPEN**. Implementation and protocol checks pass; this is not balance acceptance.

| Policy | Seeds | Wins | Required wins | Median real min | P90 real min | Required P90 | Result |
|---|---:|---:|---|---:|---:|---|---|
| dodge | 3001–4000 (1000) | 96.6% | 72–88% | 4.826667 | 7.493333 | ≤5.5min | FAIL win and duration corridors |
| static | 3001–4000 (1000) | 5.9% | ≤10% | 1.760890 | 2.296505 | ≤5.5min | PASS |

All 2000 battles finished. All 37593 bot commands executed; no rejected or unconfirmed commands and no legacy chance/roll facts. The actual shipped CombatMukha scenario, production registry and crew ratings 20 were used without calibration. Engine owns every flight, fuse, damage, NPC decision, named RNG and outcome.

Fixed policy public-observation-v1:100ms physical observations;500ms physical reaction after first observation; fast torpedo profile; arc aim only after two equal observed one-second turns, otherwise current-course straight; wide counter profile at an interception within own range; dodge once per dangerous observed projectile, when current-course closest approach is within its public blast radius and ≤2physical seconds away, left side, only outside an existing evade. Static retains torpedo/PRO actions and never evades. No private endpoint/deadline/AI controls are read. Counter points use the observed vector, not a hidden future position. Horizon600physical seconds; x1 real=physical×4; victory sampling resolution≤0.1physical second; P90 is nearest-rank over all 1000 outcomes per policy.

Compared with the supplied reference, dodge averages7.952 torpedoes vs5.4,9.538 counter rounds vs5.3,18.167 hostile torpedoes vs15.0,1.922 player contacts vs3.8 and13.25 evades vs7.1. These are measured differences, not a proven causal diagnosis. Known model differences include production HostileOrbitMath and the newly written public-observation input policy. No reference command tape was supplied; bitwise simulator equivalence is unproven. The policy was fixed before measurement; seed3001/3002 integrity probes checked command execution and determinism, not a desired win outcome. No catalog, scenario, AI, damage or policy retuning was performed after seeing the corridor results.

[Complete summary, commands and per-process evidence](us0014-tk0001-balance-summary.json) · [All 2000 raw seed outcomes and command tapes, JSONL gzip](us0014-tk0001-production-balance.jsonl.gz) · [Build, regressions and source-verified incremental Engine coverage](us0014-tk0001-results.json).

Raw corpus protocol:40 distinct theory cases, each50 seeds and a hard300000ms process-tree timeout; all passed. Owning regression2195 passed; focused3 passed; source-verified incremental Engine coverage2374 cases combines2236 current executions with 138 unchanged long cases from ticket65. The 138 were not rerun here. Balance acceptance remains OPEN for the author. Continue analyzer/native/documentation work without changing balance automatically.
