# GPU/window probe

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

See [the September 24 investigation](../../Documentation/04-Engineering/Performance500/gpu-jitter-2026-09-24.md)
for the captured failure and validation results.
