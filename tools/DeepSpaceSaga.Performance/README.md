# GPU/window probe

## Module panel native validation

`DeepSpaceSaga.Performance <DSS root> <absolute report.json> --module-panels-window <scale> <WxH> <ru|en>`
opens the actual Skia/OpenGL window over a paused production PlayerShipOnly
fixture. It checks profile/operator/stat rows, caption priority/bounds, admitted
launch, reload text surviving hover, blocked preparation clicks and engine times.
Four PNGs are GPU surface captures. The mode restores its output Settings.json
and uses the report directory for disposable runtime outputs. Scale cases are
0.8/1/1.2/1.5; the small case is1280x720 at1.5.

This is managed screen input and actual Engine execution. Physical keyboard,
mouse, human visual acceptance, sound and input-to-photon latency remain separate.
Run only within an explicit user-authorized native/epic execution workflow and
write reports under D:/DeepSpaceSaga/artifacts/validation/<task>/<unique-run>/.
Current evidence belongs to [EP-0014](../../Board/EP-0014-module-panels/FinalValidation.md).

## Current EP-0012 contract — 2026-10-21

Current --combat-window routes PointCombatNativeEvidence, a real SkiaWindow/OpenGL run over actual production Engine snapshots. Command: `dotnet run --no-build --project tools/DeepSpaceSaga.Performance -- <repo> <report.json> --combat-window <0.8|1|1.2|1.5> <1280x720|1920x1080> <ru|en> <input|win|defeat> [--audio] [--clean-flight]`. Build first; hard process cap300000ms, internal3min guard. Output includes PNGs, actual locale/scale/GPU, checks, public commands and explicit NOT RUN limits. Terminal replays use unchanged shared production input policy. Tool temporarily changes deployed Settings and restores bytes. Clean-flight cancels overlapping preview before independent radius sampling. Managed callbacks/controlled clocks are not physical keyboard/mouse, normal transport, input-to-photon or auditory acceptance. Old CombatNativeEvidence/guided arguments below are historical, while unrelated map/report/performance commands retain their scope. [Point-aim combat contract](../../Documentation/04-Engineering/PointAimCombat.md).

## Актуализация EP-0008 — 2026-10-12

EP-0008 final review and recorded native matrix are linked below. The user currently defers all game/native launches: do not execute the native combat harness until that restriction is lifted. Historical13/14 completed interactions and failed frame-time target are not full acceptance. Automated solution tests and headless performance checks remain distinct from native evidence.

[Текущий контракт](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/01-Requirements/EngineRequirements.md#phase-three-combat) · [Проверки и ограничения](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/PhaseThreeCombatAcceptance.md).


The existing positional command runs CPU/raster benchmarks. `--window-probe`
creates a real OpenGL window and renders the game screen from a tactical-map
snapshot, including camera scale, player following, selection and prediction age.
It does not run the engine or replay a stream of authoritative snapshots.

Build from the repository root:

```powershell
dotnet build tools/DeepSpaceSaga.Performance -c Release
$snapshot = (Get-ChildItem src/DeepSpaceSaga.Client/bin/Debug/net8.0/TacticalMapSnapshots/*.json |
    Sort-Object LastWriteTime | Select-Object -Last 1).FullName
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll `
    --window-probe D:/DeepSpaceSaga/Downloads/map-profile.json `
    --snapshot $snapshot --assets D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/bin/Debug/net8.0 `
    --visible --topmost --seconds 60
```

Run variants sequentially, without builds or other GPU probes in parallel.
The first three seconds are warmup; closing the window ends the probe.

- Default: production GPU options, VSync on, automatic buffer swap disabled so
  event/update/render/flush/swap durations can be measured separately.
- `--no-vsync`: isolate long driver/GPU waits from refresh waiting. The probe
  sleeps 5 ms each frame in this mode; interval/FPS figures include that sleep
  and Windows scheduling and are **not** the production game's frame rate.
- `--legacy`: restore path-mask caching and the old mask-filtered target
  forecast. Everything else is current code, including cached reticle glow;
  this is a controlled reproduction of the offending settings, not an old binary.
- `--gpu-stages`: flush and finish after each drawing stage to localize stalls.
  This deliberately changes batching and stalls the CPU; use it only for diagnosis,
  never as a throughput result. The production renderer does not call `glFinish`.
- `--finish` / `--submit`: diagnose whole-frame completion/submission separately.
- `--no-input`: test native event polling without Silk input devices.
- `--image <absolute.png>`: capture one frame during warmup for visual inspection.
- `--cache-mb N`, `--no-path-cache`, `--no-blur`, `--no-target-blur`,
  `--no-reticle-blur`: controlled cache/effect experiments. Combine with `--legacy`
  when bisecting the old pipeline.

Without `--snapshot`, only a cleared window is drawn as a presentation/input
control. Without `--visible`, the window stays hidden and the probe sleeps 5 ms.
Keep visibility, viewport size, VSync, asset directory and snapshot identical
within an A/B pair. Warmup image readback is excluded from steady-state results.

JSON includes every measured frame, interval percentiles, input and swap maxima,
GC counters/pause time, GPU resource count/cache bytes, driver information and
the source snapshot hash. Swap duration includes driver/GPU/display waiting;
it does not measure physical scanout. Screen predictions advance from the saved
authoritative state; trails bootstrap normally rather than restoring their exact
historical buffers. An Approach can finish during a long probe.

See [the September 24 investigation](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/Performance500/gpu-jitter-2026-09-24.md)
for the captured failure and validation results.

## Solar-system evidence

Run the production generation/resource pipeline for all requested seeds and scenarios:

    dotnet run -c Release --project tools/DeepSpaceSaga.Performance -- D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/solar-max.json --solar-map --seeds 1:100 --scenarios all --config max

--config min|max selects 3/7 planets, 2/5 belts and 50/75 starting days. --scenarios accepts all or a comma-separated list of scenario folder names. Invalid input returns exit code 1 with a failed report.

Every world records generation time/allocations, snapshot time/allocations and serialized save time/bytes. Catalog parsing is outside the generation timer. Raster rendering samples the first requested seed in every scenario, at System and belt views: 120 warmup frames and 600 measured frames at 1920x1080. It records p50/p95/p99 and allocation bytes/frame. Report status indicates successful measurement; presentation.status=not-measured explicitly prevents treating raster timing as evidence of GPU 80 FPS. Hardware/runtime, source commit, full generation configuration and seed range accompany the measurements.

Native solar-system window evidence (actual production SkiaWindow, live local session):

    dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/window-max.json --solar-window max system 1.5 1920x1080

Select min|max, system|belt|selected, UI scale 1|1.2|1.5, and window dimensions.
The runner uses client assets/settings, selects the start station, exercises actual
pause/resume buttons during warmup, captures a PNG, measures 600 frames after 120
warmup frames, and closes its window. Run cases sequentially without concurrent
builds/tests. The production client supports the same opt-in collector through
DSS_MAP_FRAME_REPORT; without it no evidence files or engine requests are added.
DSS_MAP_FRAME_IMAGE, DSS_MAP_FRAME_WINDOW and DSS_MAP_FRAME_EXIT are optional
diagnostic controls, enabled only with the report collector.

Reports distinguish CPU submit, SwapBuffers wait, and intervals between completed
swaps. None measures physical scanout or GPU execution alone. targetVerdict is
failed when p99 exceeds 12.5 ms, or display-limited when VSync's reported monitor
refresh is below 80 Hz; measurement completion never implies the FPS target passed.
Keep final textual results with hardware, commit, settings and limitations; remove
temporary PNG/JSON after inspection.

## EP-0004 complete-map evidence (2026-10-08)

Use absolute repository/output/reference paths: the runner enters the Client asset directory. All layers require `--clusters`; without `--all-map-layers`, environment/POI are disabled for a clean baseline. `--ai-placement` enables detailed bounded placement diagnostics. `--boundary-only` makes the baseline select one min/max cluster boundary, matching the full-map run.

```powershell
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS-EP-0004 D:/DeepSpaceSaga/baseline.json --solar-map --clusters --boundary-only --seeds 1:100 --scenarios all --config max
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS-EP-0004 D:/DeepSpaceSaga/full.json --solar-map --clusters --all-map-layers --seeds 1:100 --scenarios all --config max
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS-EP-0004 D:/DeepSpaceSaga/native.json --solar-window max base 1.5 1280x720 --clusters --all-map-layers
```

Full-map min forces3x10 human stations/2 AI, max5x12/4 AI; native default scenario is Default_500 seed1. Native views: system, belt, selected, cluster, base, field, poi; descriptor views require all-map-layers. During warmup the runner fits System/cluster, selects real AI/field/POI through mouse handlers, toggles layers, closes diagnostics, scrolls the selected-base info and observes run/pause/resume. Exit0 means interaction/report completion, **not**80FPS. For native acceptance inspect fullMapInteraction, targetVerdict and PNG. Final tested matrix: min/system/1/1280x720; max/system/1/1920x1080, belt/1.2/1920x1080, cluster/1.5/1920x1080, base/1.5/1280x720, field/1.5/1920x1080, poi/1.2/1280x720, poi/1.5/1920x1080.

Rows distinguish real entities, descriptors, configured decoration samples, layer flags, calendar and motion epochs. Decorations can be culled by LOD, so configured samples are not the exact drawn count. Full-map raster samples System/belt/cluster, first requested seed in each scenario,120 warmup+600 measured frames. Native reports carry live counts and actual OpenGL context separately. Optional `--client-frame-report D:/absolute/native.json` on --solar-map validates and hashes an existing native case; it is an independent case, not a substitute for matching all raster configurations. `--economy-report` is a separate optional acceptance input; this epic does not establish profitability.

Current measurements: scripted native8/8 PASS, FPS target0/8 PASS (p9950.5839–54.4007ms vs12.5ms), with VSync reported100Hz on Intel Arc140V. Fresh baseline/empty-window controls also fail; exact driver/OS cause unproven. Do not claim GPU execution, physical scanout or human playthrough. Compact results and source/raw hashes are retained in [EP-0004 evidence](../../Board/EP-0004-ai-territories-and-map-environment/ImplementationStatus.md); temporary native JSON/PNG were removed after review. [Current map contract](../../Documentation/04-Engineering/AiMapEnvironment.md).

## EP-0008 combat native evidence

Build Release, then run each native case sequentially without concurrent tests/builds:

```powershell
dotnet build tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj -c Release --no-restore
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/ep8-native/combat.json --combat-window 1.5 1280x720
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/ep8-native/recovery.json --combat-window 1.5 1280x720 --recovery
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/ep8-native/no-solution.json --combat-window 1.5 1280x720 --no-solution
```

Matrix:1280x720 and1920x1080, scale1/1.2/1.5, normal and recovery. No-solution uses a deliberately slow, short-lived, zero-accuracy custom defense module; it is not a balance claim about production defaults. Do not combine that option with recovery (its registry intentionally differs from production save loader content). The runner uses production content and real LocalSession, setting a separate initial fixture with8 Enemy NPCs before NewGame. It never sends player commands on behalf of NPCs or edits positions/HP during battle. Two manual PR launches, independent auto toggle, preview equality, selected target, zoom, x100/pause and journal scrolling use actual screen input handlers. Recovery uses real terminal routing, native Defeat/Load presentation, cancel and a separate pre-death save slot.

The executable temporarily applies the selected UI scale to its own output Settings.json and restores original bytes in finally. It does not edit source Settings.json or user slots. A deterministic injected clock advances5 physical milliseconds per measured render in the normal case; the recovery case remains paused after Load. These cases prove rendering and transport interactions, not real-time AI throughput. Full generated-world battle/clock/AI continuity is tested separately by PhaseThreeCombatEndToEndTests/PhaseThreeDeterminismTests.

Reports contain720 presented frames (120 warmup/600 measured), actual OpenGL hardware, CPU submit and completed-swap intervals, plus combatInteraction checks. Exit0 means interaction/report completion, not a passed80FPS target. Inspect targetVerdict independently; GPU execution/physical scanout and human playthrough are not measured. PNGs capture actual native surfaces at preview/two-PR/journal/defeat/load stages. Reports and separate *.slots files remain at the requested output location for review.

## EP-0010 report window evidence — 2026-10-16

`CombatReportsNativeEvidence` / Program flag `--reports-window` prepares a real-window report UI matrix over paused production CombatMukha seed1 with explicit synthetic UI report fixtures. Arguments after the DLL: DSS root, output JSON, `--reports-window`, scale, dimensions, language; optional `--art` captures8 variants and `--performance` collects600-frame samples. This is owner-operated tooling. **User instruction: never launch the game autonomously. Agents must not execute the game or this native/performance harness without a new explicit user request.** Building and headless unit tests are allowed.

The one existing1280×720/UI100/English sample contains64 frames and UI checks; it does not complete the full matrix, native Save/Load/Defeated or performance acceptance. The run without `--performance` has zero measured perf samples; `targetPassed=false` is unmeasured, not a measured FPS failure. [Actual evidence/limitations](../../Board/EP-0010-combat-reports/evidence/NativeAcceptance.md), [performance status](../../Board/EP-0010-combat-reports/evidence/Performance.md). Raster/headless tests cannot establish GPU/scanout FPS or human acceptance.

## US20 native run — 2026-10-21

The harness now checks the exact allowed combat speed set, restored right ship card/left health details, torpedo aim exit and absence of forward foreign geometry. Counter fixture uses displayed poses and searches a candidate through production preview; the recorded run still has two failed CM launch/detonation assertions. Do not call the whole native scenario PASS. Physical hardware input and hearing remain NOT RUN. [Raw evidence and current limits](../../Board/EP-0012-combat-rebuild/EP-0012-US-0020-bugs/ExecutionStatus.md).
