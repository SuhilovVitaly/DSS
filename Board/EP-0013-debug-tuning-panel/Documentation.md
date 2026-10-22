---
epic: EP-0013-debug-tuning-panel
title: "Отладочная панель тактической карты и ручное маневрирование корабля игрока"
stage: in-progress
created: 2026-10-22
revision: 2
baseline: "codex/ep-0012-combat-rebuild @ e134a2ca"
validation_status: automated-pass-native-managed-pass-human-open
---

# EP-0013

[Вход в эпик и истории](EP-0013-debug-tuning-panel.md) · [Полный исходный материал revision 2](SourceSpecification.md) · [Тикеты](Tickets.md)

История оформления Board от2026-10-22: существующий пакет и полное приложение
revision2 сохранены. Утверждение ниже об отсутствии пакета относится к его
первоначальному созданию. Текущее поручение — исполнить эпик по
[EpicExecutionPrompt](../EpicExecutionPrompt.md), включая публикацию и финальную
документацию. [Фактическое выполнение](ExecutionStatus.md),
[покрытие37 AC](AcceptanceCoverage.md), [инвентаризация](DocumentationImpact.md)
и [публикации](PublicationStatus.md) отделяют реализацию от human acceptance.

Implementation authority: user's complete revision 2 specification in the execution conversation (2026-10-22), plus `Board/EpicExecutionPrompt.md`. Publication rule updated by the user on 2026-10-22: each completed ticket receives a separate commit, followed immediately by push. After every commit, verify the target branch and remote, push, then confirm the remote branch SHA matches local HEAD. Draft status does not block execution.

## Grounding and corrections

- Actual clean starting revision: `0e67e2b8`, branch `codex/ep-0012-combat-rebuild`; implementation branch `codex/ep-0013-debug-tuning-panel`.
- No EP-0013 package existed on disk. This package records the supplied requirements and implementation evidence.
- §4 lists **25** parameters (6 engine + 14 torpedo + 5 CM), despite the repeated total 29. Implement every enumerated parameter; invent none.
- Functional story IDs remain US-0001 through US-0006. The documentation ticket runs last in US-0007 (the mandatory final documentation story), preserving the original `US5-TK5` alias. Functional order remains the supplied §10 order.
- Engine registry is `Content/GameDataRegistry.cs`; definitions are immutable records, and `TypeRegistry` uses fixed numeric indices. Dispatch and snapshot construction currently live in `SimulationEngine.cs`.

## Accepted behavior

Engine owns an immutable catalog overlay: `debug.tuning.apply`, category `debug`, atomic `DebugTuningPatch` of `(moduleTypeId, field, profileId?, value)`. `DebugTuningState` holds original defaults, current values and monotonically increasing Revision. Unknown field/type/profile rejects `debug_unknown_parameter`; nonfinite/outside range/nonintegral integer fields rejects `debug_value_out_of_range`; no running session rejects `debug_session_not_running`. Pause and dock/dialogue accept tuning. Successful commands run in the normal player-command phase, using no RNG and preserving identities/counters. Snapshot publishes `{Revision, Values, Defaults}`; compact frames include it only on revision change.

### Parameter table

| Type | Field | Default | Range | Step / Ctrl |
|---|---|---|---|---|
| module.engine.tetrarch | maxSpeedKmS | 5 | 0.1–50 | 0.5 / 0.1 |
| same | accelerationMps2 | 600 | 10–20000 integer | 50 / 10 |
| same | turnRateDegPerSec | 20 | 1–360 integer | 2 / 1 |
| module.engine.strizh | maxSpeedKmS | 8 | 0.1–50 | 0.5 / 0.1 |
| same | accelerationMps2 | 1000 | 10–20000 integer | 50 / 10 |
| same | turnRateDegPerSec | 24 | 1–360 integer | 2 / 1 |
| module.torpedo.launcher.basic | reloadMs | 10000 | 100–600000 integer | 500 / 100 |
| module.torpedo.launcher.light | reloadMs | 7250 | same | same |
| each torpedo launcher, slow | speedKmS / blastRadiusKm | 6 / 9 | 0.1–100 / 0.05–50 | 0.5 / 0.1 each |
| each torpedo launcher, medium | speedKmS / blastRadiusKm | 10 / 5 | same | same |
| each torpedo launcher, fast | speedKmS / blastRadiusKm | 16 / 2.5 | same | same |
| module.countermeasure.launcher.basic | reloadMs | 12500 | 100–600000 integer | 500 / 100 |
| same, wide | speedKmS / blastRadiusKm | 15 / 1 | 0.1–100 / 0.05–50 | speed 0.5 / 0.1; radius 0.25 / 0.05 |
| same, fast | speedKmS / blastRadiusKm | 25 / 0.5 | same | same |

Defaults come from the original catalog, never from an overlay or the defaults file. Engine max speed converts km/s to its integer m/s field. Unlisted `maneuverability`, damage, armor, accuracy, AI and range are outside scope. No Data catalog edits.

### Immediate application

At command time t, ships keep position (<=1e-6 km discontinuity), clamp excessive speed to the new maximum and replan engine cycles, turn/speed holds, evasion and NPC orbit from the current pose. Evasion escape end remains unchanged, return deadline uses new limits. NPC decisions read new definitions within their 100ms grid. EngineMotion and MaxSpeedKmS projections use updated values.

Live projectile launch identity/profile locates its tuning entry. Sample old P(t), keep its scattered detonation point A, replace start with P(t) and deadline with `t + distance(P,A)/newSpeed`. Zero remaining distance detonates at t. New radius affects unexploded projectiles; foreign observations disclose only vector/speed/profile/radius, never A/deadline. Coincident blasts retain CM-first then torpedo ordering, each ordinal ID.

Reload retiming: `readyAtNew = readyAtOld - oldReload + newReload`, clamp completed readiness to now; already-ready launchers stay ready. Next launch captures the new duration. Identical commands/seeds and different integration partitions must agree.

### Persistence and startup

Save optional `debugTuning` contains current nondefault overlay. Restore catalog + saved overlay **before projectile validation**, then apply startup file as a normal tuning command with live retiming. New scenario uses catalog + startup file. Decide Save18 vs Save19 from actual strict loader behavior and document in US3-TK1. Holds never persist: loaded ship keeps its sampled course/speed, with input released.

US3-TK1 decision2026-10-22: strict unknown-member rejection requires writerSave19.
Save18 migrates explicitly with no tuning/provenance; Save17 and older remain
rejected. Retimed flights retain private original launch facts/type identity,
separate from their current integration anchor. Catalog edits preserve captured
flight parameters; a debug command retimes only profile fields it changes.

Path: `AppContext.BaseDirectory/DebugTuning/`. UTF-8, two spaces, invariant decimal numbers. `debug-tuning.json` schemaVersion 1 includes writtenAtUtc and ships/launchers/profiles nested by type/profile IDs exactly as the table. Partial files fill absent values from catalog. Unknown keys, wrong types, nonfinite/out-of-range values or schema !=1 reject the **entire** file; log path/reason and show a red first-open panel message. Read only at session startup, not live. Allow optional `source: catalog` so the defaults file can be copied over the current file.

Client writes all current values after confirmed snapshot revisions, atomically via temp/replace, at most once/250ms with final revision guaranteed. Reset-all applies one complete defaults patch then deletes the current file. Every application start writes `debug-tuning.defaults.json` from original catalog with `source: catalog`; game never reads this file.

### Tactical panel

F12 configured by `debugPanel` toggles an always-available, nonpausing right overlay, width360×UI scale, scrolling to map bottom. Five collapsible sections in table order. Rows contain localized name/value, minus/plus, default-reset; Ctrl uses fine steps, wheel over value adjusts, boundaries disable buttons. Modified values use styles.Warning and gray default. Pending value italic; accepted values come only from Engine. Status shows localized rejection or applied/file-written HH:MM:SS. Reset-all requires a second click within1s; Open Folder opens DebugTuning; '?' expands the two tuning caveats (orbit speed is constrained by acceleration/turn, torpedo speed relative to ships changes lead difficulty).

Panel owns mouse click/wheel/hover within its bounds (no aim/zoom/threat selection there). Combat/maneuver keys remain active. Escape closes aim first, otherwise panel. All strings under `Debug.*` in Russian/English; missing keys fail tests. Reuse/dispose SKPaint/SKFont; unchanged render frame has no LINQ/allocation.

### Manual maneuver

Input schema2: Q torpedo, E CM, Z/C evade; A/Left and D/Right turn; W/Up and S/Down speed; ShiftLeft/ShiftRight latched full speed; Tab threat, Escape cancel, Number1/2/3 profiles, Left/Right mouse launch/cancel, wheelProfiles true, F12 debug. Axis bindings allow1–2 keys. Schema1 migrates defaults and resolves old W/A/D conflicts in favor of schema2 with a log warning. S without Ctrl reaches braking; Ctrl+S retains Ship, Ctrl+C retains its existing function, I remains reserved. Hints derive from bindings.

Input runs everywhere on tactical map, including peaceful PlayerShipOnly. Remove old arrow step/one-shot path. Commands only on axis state change; aliases don't release until all same-direction keys release; last pressed opposite key wins, releasing it restores the previous held direction. Shift starts latched acceleration which survives key release and turn, ends at max or a speed-key press. Maneuver preserves aiming.

`engine.speedHold` payload SpeedDirection(Accelerate/Brake), Active, Latched; motion carries SpeedHoldAccelerationKmS2 and SpeedHoldLatched only for own ship. It parallels turnHold: neither cancels the other. Both cancel engine cycles/approach/orbit/synchronization/evasion with Cancelled receipts. Opposite-direction releases do nothing. Disabled/destroyed engine ends holds at current pose; Client retries still-held input on recovery. Dock/dialogue/defeat sends no maneuver and Engine refuses engine_disabled. Pause accepts input, motion resumes with time. Focus loss/load releases all input. Evade clears holds and requires a fresh maneuver press; a fresh manual press interrupts evasion.

Shared Motion integrates constant a and omega using HostileOrbitMath.Sample geometry. Split at speed0/max: a becomes0 while omega continues. Own5s CurrentManeuverForecast must match Engine within0.01km. Partition invariance and engine table examples use1e-6 pose/course/speed tolerance: D3s=60deg; W2s at3=4.2km/s; W5s clamps5; S6s clamps0; D+W2s=(40deg,4.2km/s,analytic arc); opposite-key restoration and Shift cases.

## Quality gates

Per ticket: named tests + affected regression, zero-warning build, scoped format, diff check, evidence, separate commit followed immediately by push and remote-SHA verification. All test processes hard-capped300000ms with process-tree termination; partition long suites. Balance tests optional. Whole solution build before completion. Independent review is distinct from self-review; native/GPU/physical-input/human acceptance must be labeled NOT RUN if unavailable. Final story updates all relevant project docs, status/links and Graphify. Push immediately after every commit under the updated §9; verify the remote branch SHA matches local HEAD.
