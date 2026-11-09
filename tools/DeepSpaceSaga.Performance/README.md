# Tactical battle native acceptance

From DSS root, run the built tool with absolute evidence output under workspace artifacts:
`dotnet <tool.dll> D:/DeepSpaceSaga/DSS <absolute-report.json> --tactical-window 1 1280x720`.
The EP-0019 route checks the full map/compact overlays, actual movement/fire, replay,
New battle after outcome, exit, world camera/speed and QuickSave/QuickLoad.

For another monitor, set DSS_TACTICAL_NATIVE_SETTINGS to a task-owned copy of Settings.json
with gameSettings.selectedMonitorIndex. Set DSS_TACTICAL_NATIVE_EXPECT_SKIN to its effective
skin ID (for example classic). The native report records monitorPosition and verifies
selected-monitor-position against Silk monitor bounds. Do not change the players settings
for a probe. Use Start-Process -WindowStyle Hidden and a 300000ms tree cap. This is scripted
native input; physical-input/human acceptance remains separate.

# GPU/window probe

## World-map v2 acceptance

Run from `D:/DeepSpaceSaga/DSS`. Create a unique absolute directory under
`D:/DeepSpaceSaga/artifacts/validation/<task>/<unique-run>/` for every run.
V2 refuses to overwrite an existing evidence directory. Run native cases sequentially,
with no concurrent builds/tests/GPU probes.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>'
New-Item -ItemType Directory -Path $run -Force | Out-Null
dotnet build tools/DeepSpaceSaga.Performance -c Release --no-restore *> "$run/build.log"
if ($LASTEXITCODE) { throw 'Build failed' }
$dll = 'D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll'
function Invoke-MapEvidence([string]$name, [string[]]$evidenceArguments) {
    $process = Start-Process dotnet -ArgumentList $evidenceArguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput "$run/$name.log" -RedirectStandardError "$run/$name.err"
    if (!$process.WaitForExit(300000)) { $process.Kill($true); throw "$name TIMEOUT" }
    if ($process.ExitCode) { throw "$name failed; inspect retained report/log" }
}
# Shard by scenario to keep each process below the five-minute tree cap.
foreach ($scenario in @('Default','Default_500','Docked','Undocked','MarketProfiles','PlayerShipOnly')) {
    Invoke-MapEvidence $scenario @($dll, 'D:/DeepSpaceSaga/DSS', "$run/$scenario.json", `
        '--map-v2-acceptance', '--scenarios', $scenario)
    dotnet $dll 'D:/DeepSpaceSaga/DSS' "$run/$scenario.json" --validate-map-v2-report
    if ($LASTEXITCODE) { throw 'Raster evidence invalid' }
}
Invoke-MapEvidence 'native' @($dll, 'D:/DeepSpaceSaga/DSS', "$run/native.json", `
    '--map-v2-native-acceptance', '--seed', '42', '--viewport', '1920x1080', '--ui-scale', '1.5')
dotnet $dll 'D:/DeepSpaceSaga/DSS' "$run/native.json" --validate-map-v2-native-report
if ($LASTEXITCODE) { throw 'Native evidence invalid' }
```

Strict raster options/defaults: `--seeds 1,42,100`, `--scenarios` comma-separated enabled
names, `--viewports 1280x720,1920x1080,3440x1440,5120x1440`, `--ui-scales 1,1.5,2`,
`--warmup 5`, `--frames 30`. Four views: player start, selected-station local,
cluster fit, system. Six default scenario shards produce864PNG/18world snapshots.
Smaller explicit matrices are subsets. Unknown flags, duplicate/invalid values,
invalid tacticalMap settings and outputs outside workspace validation fail.

Schema2 records seed/version/frozen Vmax, generation config, machine/runtime,
HEAD/tool DLL SHA256/Settings hash, geometry, camera/detail, PNG/snapshot hashes,
CPU timing and allocation. Unexposed solar/cluster retry counters are explicitly
unavailable; actual AI diagnostics/configured limits are separate. Validation recomputes
human min/nearest, camera spans, coverage and hashes/dimensions. Raster completion
does not imply native FPS. Overlay state and actual detail are recorded: cluster fit
can retain System detail when the free viewport is small.

Native options: `--seed`, `--viewport`, `--ui-scale`. The Default route uses production
screen handlers/fit-focus APIs and session events, real window resize, Approach,
acknowledged fast speed, synchronization, docking dialogue, observed Dock,
QuickSave/QuickLoad and paused follow return. Overlaps use repeat-click selection.
Native surfaces/camera states and latest authoritative phase observations are
archived. This is managed scripted input, not hardware input. Internal40s and
external300000ms caps reject timeout. Slots use a unique owned directory/marker
under the evidence directory; cleanup never deletes ordinary user slots.

The native frame report records120warmup/600swap-completion intervals, GPU/refresh,
p50/p95/p99, CPU submit and swap wait. Schema2's p95 target is12.5ms (80FPS);
the older host also records its stricter p99 verdict. Keep both verdicts and display
limits visible. Neither measures physical scanout or GPU execution alone. Human,
physical and matched-baseline acceptance remain separate. Retain referenced reports/PNG;
remove disposable slots/cache/scripts after owned processes stop. Actual outcomes and
open gates belong to [EP-0018](../../DB/Board/EP-0018-map-geography-and-scale/ImplementationStatus.md).

Legacy `--solar-map`/`--solar-window` below explicitly pin v1/.08 belt width and
original cluster ranges. They do not measure shipped v2. Historical measurements
retain their meaning and are not a matched current v2 baseline.

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
Current evidence belongs to [EP-0014](../../DB/Board/EP-0014-module-panels/FinalValidation.md).

## Current EP-0012 contract — 2026-10-21

## Актуализация EP-0008 — 2026-10-12


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
    --window-probe D:/DeepSpaceSaga/artifacts/validation/window-probe/<unique-run>/map-profile.json `
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

    dotnet run -c Release --project tools/DeepSpaceSaga.Performance -- D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/artifacts/validation/solar-v1/<unique-run>/solar-max.json --solar-map --seeds 1:100 --scenarios all --config max

--config min|max selects 3/7 planets, 2/5 belts and 50/75 starting days. --scenarios accepts all or a comma-separated list of scenario folder names. Invalid input returns exit code 1 with a failed report.

Every world records generation time/allocations, snapshot time/allocations and serialized save time/bytes. Catalog parsing is outside the generation timer. Raster rendering samples the first requested seed in every scenario, at System and belt views: 120 warmup frames and 600 measured frames at 1920x1080. It records p50/p95/p99 and allocation bytes/frame. Report status indicates successful measurement; presentation.status=not-measured explicitly prevents treating raster timing as evidence of GPU 80 FPS. Hardware/runtime, source commit, full generation configuration and seed range accompany the measurements.

Native solar-system window evidence (actual production SkiaWindow, live local session):

    dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/artifacts/validation/solar-v1/<unique-run>/window-max.json --solar-window max system 1.5 1920x1080

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
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/artifacts/validation/solar-v1/<unique-run>/baseline.json --solar-map --clusters --boundary-only --seeds 1:100 --scenarios all --config max
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/artifacts/validation/solar-v1/<unique-run>/full.json --solar-map --clusters --all-map-layers --seeds 1:100 --scenarios all --config max
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll D:/DeepSpaceSaga/DSS D:/DeepSpaceSaga/artifacts/validation/solar-v1/<unique-run>/native.json --solar-window max base 1.5 1280x720 --clusters --all-map-layers
```

Full-map min forces3x10 human stations/2 AI, max5x12/4 AI; native default scenario is Default_500 seed1. Native views: system, belt, selected, cluster, base, field, poi; descriptor views require all-map-layers. During warmup the runner fits System/cluster, selects real AI/field/POI through mouse handlers, toggles layers, closes diagnostics, scrolls the selected-base info and observes run/pause/resume. Exit0 means interaction/report completion, **not**80FPS. For native acceptance inspect fullMapInteraction, targetVerdict and PNG. Final tested matrix: min/system/1/1280x720; max/system/1/1920x1080, belt/1.2/1920x1080, cluster/1.5/1920x1080, base/1.5/1280x720, field/1.5/1920x1080, poi/1.2/1280x720, poi/1.5/1920x1080.

Rows distinguish real entities, descriptors, configured decoration samples, layer flags, calendar and motion epochs. Decorations can be culled by LOD, so configured samples are not the exact drawn count. Full-map raster samples System/belt/cluster, first requested seed in each scenario,120 warmup+600 measured frames. Native reports carry live counts and actual OpenGL context separately. Optional `--client-frame-report D:/absolute/native.json` on --solar-map validates and hashes an existing native case; it is an independent case, not a substitute for matching all raster configurations. `--economy-report` is a separate optional acceptance input; this epic does not establish profitability.

Historical EP-0004 measurements: scripted native8/8 PASS, FPS target0/8 PASS (p9950.5839–54.4007ms vs12.5ms), with VSync reported100Hz on Intel Arc140V. Fresh baseline/empty-window controls also fail; exact driver/OS cause unproven. Do not claim GPU execution, physical scanout or human playthrough. Compact results and source/raw hashes are retained in [EP-0004 evidence](../../DB/Board/EP-0004-ai-territories-and-map-environment/ImplementationStatus.md); temporary native JSON/PNG were removed after review. [Current map contract](../../DB/Documentation/04-Engineering/AiMapEnvironment.md).

## Tactical battle initiative

Build Release, create an absolute evidence directory and run the requested mode
from the DSS repository root. The example uses a new diagnostic run.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/<unique-run>'
New-Item -ItemType Directory -Force $run
$env:DSS_VALIDATION_ROOT = $run
dotnet build tools/DeepSpaceSaga.Performance -c Release --no-restore
$dll = 'tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll'
dotnet $dll 'D:/DeepSpaceSaga/DSS' "$run/performance.json" --tactical-performance
dotnet $dll 'D:/DeepSpaceSaga/DSS' "$run/native.json" --tactical-window 1.5 1920x1080
```

Use a 300-second process-tree cap and separate output per case. The Release
benchmark reports authoritative turn and series time, warm mouse aiming and
CPU Skia drawing. The native mode uses a real OpenGL framebuffer, an isolated
slot, the live keyboard router and screen mouse callbacks. It saves phase PNGs
and draw/flush timings. Physical input, scanout timing and human/product review
are separate. Old realtime combat flags have been removed; their runs remain
historical evidence in the [Board](../../DB/Board).

For a skin verification case, point `DSS_TACTICAL_NATIVE_SETTINGS` at an isolated `Settings.json` with its own sibling `Skins` directory and set `DSS_TACTICAL_NATIVE_EXPECT_SKIN` to the effective ID. Set `DSS_TACTICAL_NATIVE_PICK_LAST=1` to choose the last catalog entry through the live Settings overlay before F8. The native report records the outcome and the effective-skin checks for both active battle entries. Use the same seed, viewport and scale when comparing runs. [Skin authoring contract](../../DB/Documentation/04-Engineering/TacticalBattleSkins.md)

Current rules and evidence: [TacticalBattle](../../DB/Documentation/04-Engineering/TacticalBattle.md),
[ExecutionStatus EP-0020](../../DB/Board/EP-0020-tactical-battle-mechanics/ExecutionStatus.md).
Select another monitor through `gameSettings.selectedMonitorIndex` in the isolated
settings profile (zero based); leave the regular user settings untouched. Native
scripted input does not establish physical-input, human/product or balance acceptance.

The current tactical native workflow also exercises E/D through the live keyboard router, rejects D while evading, checks the build-card model and restores the order flags before submission. It verifies replay, outcome/new battle, unchanged world/camera and quick Save/Load after exit. Run one native case at a time with no concurrent builds/tests; use both 1280x720/S1 and 1920x1080/S1.5 and an isolated settings profile for language/monitor. A passed interaction report does not mean80FPS: inspect nativeDraw timings separately, and keep physical input and human acceptance open.


Set `DSS_VALIDATION_SMALL_MONITOR=1` and supply an isolated settings profile
with an explicit smaller monitor index before launching a native battle. The
window checks the selection before becoming visible. The requested framebuffer
must fit the selected monitor.

With `DSS_TACTICAL_MEASURE_CADENCE=1`, the native harness measures 600
swap-completion intervals after 120 warm-up frames for four cases covering
stationary movement planning, panning, zooming and salvo playback. The 80 FPS
budget is p99 at or below 12.5 ms. Submit, flush and swap wait are reported
separately. Use `DSS_TACTICAL_BENCH_UNCAPPED=1` with cadence measurement to
measure rendering throughput on a lower-refresh validation display.
