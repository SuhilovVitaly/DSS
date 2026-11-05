# EP-0015 — numerical platform validation

Validated slice: US-0002 shared Motion functions and Engine Mulberry32, prior to battle rules/UI integration. Reference source overrides D-USER-14/15 apply.

## Independent inputs

- `VData/Planning/V4.0/Reference/geometry.json`: 1088 exact IEEE images exported on Linux / CPython 3.12.3 / glibc 2.39. Includes 14 normalization vectors, 28 sin/cos vectors, 16 signed-axis atan2 vectors and 1030 hypot vectors (normal, extreme and subnormal).
- `trace-seed-1.json`: assembly and 19 turns, rounded to six decimal places. All 34 published shot chances are checked with absolute error at most 0.0000006; no missing IEEE bits or original per-term values are invented.
- Geometry schema-1 file SHA256 during the original platform measurement: `c75405863fd6c93cfde1f30ef35fe114c485365a39f65a1ab40dce6fbeb1518c`.

## Windows

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-limits-01/`: build PASS, full Motion **1376/1376 PASS**. Exact bit assertions cover all 1088 geometry vectors, with zero mismatches. Motion also validates Spec arithmetic examples, damage factors, mass summation, chance terms, source-order guard and movement substeps. SDK/runtime inventory: `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-platform-01/windows-dotnet-info.log`.

## Linux

Geometry run [42088474253](https://github.com/SuhilovVitaly/DSS/actions/runs/42088474253), source `8a43baf53ac0ff3f88f518b769893ef7ec244283`: PASS, including all geometry images.

Limits/chance run [42089031642](https://github.com/SuhilovVitaly/DSS/actions/runs/42089031642), source `fc29503a812f6793779d3564f22884a8cd0eecdb`: PASS, full Motion **1376/1376**, Engine TacticalBattle **96/96**, Client content **2/2**. Log: `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-platform-01/linux.log`. It executes the same Motion tests against tracked independent fixtures; the workflow checks fixtures before any export. Each command has a 300-second process cap and uses an absolute diagnostic directory under `/tmp/artifacts/validation/ep-0015-tactical-carrier-battle/<run-id>`.

## Decision and limits

No .NET/glibc discrepancy was observed in the available vectors on Windows. Keep Math.Sin/Cos/Atan2 and the CPython vector_norm port; neither a speculative trig port nor regenerated battle outcomes are justified by this measurement.

This is not evidence for the absent original arguments of every trig call in 90 battles. Their absence is listed in fixture headers. End-to-end golden battles, deterministic player/AI integration, UI/native input, and performance are later epic gates. New numerical mismatches reopen G-03; expected outcomes must never be rewritten from the new Engine to conceal a defect. Self-review is not independent review.

Schema-2 correction (rearHits field name only, all numeric data unchanged) passed Linux run [42090979851](https://github.com/SuhilovVitaly/DSS/actions/runs/42090979851). Current schema-2 geometry file SHA256: `1379ce96e16a22bb0ad4670faefadfee5d4fe679ece890011e0817ddea0be64a`. This header-only change does not alter any geometry vector.
