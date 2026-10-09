# EP-0005 performance evidence — 2026-10-09

Functional implementation and automated tests passed; the 80 FPS / p99 12.5 ms presentation gate is **FAILED / OPEN**. Human manual acceptance is **NOT RUN**. This report preserves measured failures. It does not attribute all delay to the driver or claim GPU execution/physical scanout was measured.

## Reproduction and provenance

Release build: `dotnet build tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj -c Release --no-restore`. Commands run from the isolated checkout; native processes and the raster matrix ran sequentially, without concurrent builds/tests. Ordinary OS background load was not controlled.

```powershell
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll <root> <output.json> --tactical-pipeline
dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll <root> <output.json> --tactical-pipeline --native --objects 5000 --size 3440x1440 --scale 1.5
```

Measured base HEAD `35081a109fa747dee6bbdbdd523da1022ba2a946` plus TK5 changes; exact Client DLL SHA-256 `68D3317082CD01575E2F498ED716AB59BAFD57985E120E45D81F6B9EB665EB16`. All 16 native reports and the final raster report match this hash. The ticket commit is recorded after publication in [ImplementationStatus.md](ImplementationStatus.md).

Windows 10.0.26200, .NET 8.0.26, Intel64 Family 6 Model 189 Stepping 1, 8 logical processors. Native: Intel Arc 140V GPU (16GB), OpenGL 3.3 driver 32.0.101.8860, VSync on, reported display 100 Hz.

Each case: 120 warmup and 600 measured frames. Deterministic synthetic contact grid; one player, one target, one eighth of contacts move. Normal route fixture uses the valid 250 ms turn cadence. Raster uses a virtual 80 Hz clock and excludes snapshot preparation from Render timing. Native uses a real clock and refreshes synthetic snapshots once per second inside the begin observer, so that preparation contributes to native CPU submit. This is a rendering fixture, not Engine throughput or full gameplay acceptance.

## CPU/raster matrix

Raw percentiles, allocations, GC, Update/Prepare/Draw, cache and spatial counters: [pipeline-raster.json](pipeline-raster.json). Path counts mean represented points, not predictor invocations. Times below are ms; bytes are mean allocations per frame, not retained memory.

| Objects | Size | Mode | p50 | p95 | p99 | Bytes/frame | Update p99 | Prepare p99 | Draw p99 |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 500 | 1920x1080 | paused | 2.266 | 3.266 | 3.629 | 7092 | 0.347 | 0.245 | 3.386 |
| 500 | 1920x1080 | live | 3.438 | 5.955 | 6.966 | 260421 | 0.076 | 3.786 | 3.474 |
| 500 | 1920x1080 | zoom | 5.330 | 7.947 | 9.565 | 318541 | 0.079 | 3.165 | 7.193 |
| 500 | 1920x1080 | pan | 3.244 | 4.640 | 5.417 | 260143 | 0.066 | 2.786 | 3.418 |
| 500 | 1920x1080 | route | 4.168 | 6.082 | 7.024 | 276656 | 0.080 | 3.454 | 4.615 |
| 500 | 3440x1440 | paused | 4.312 | 4.917 | 5.327 | 7733 | 0.098 | 0.099 | 5.147 |
| 500 | 3440x1440 | live | 6.081 | 8.446 | 10.381 | 333467 | 0.074 | 3.284 | 7.754 |
| 500 | 3440x1440 | zoom | 8.375 | 11.645 | 14.172 | 359437 | 0.105 | 3.055 | 12.463 |
| 500 | 3440x1440 | pan | 5.958 | 8.273 | 10.126 | 332925 | 0.069 | 3.093 | 7.720 |
| 500 | 3440x1440 | route | 7.013 | 8.231 | 9.365 | 349715 | 0.075 | 3.143 | 7.142 |
| 5000 | 1920x1080 | paused | 2.911 | 3.921 | 4.623 | 7494 | 1.150 | 0.285 | 3.338 |
| 5000 | 1920x1080 | live | 6.305 | 8.947 | 11.637 | 854419 | 0.543 | 7.247 | 4.068 |
| 5000 | 1920x1080 | zoom | 8.474 | 14.304 | 20.929 | 934066 | 0.584 | 7.433 | 11.540 |
| 5000 | 1920x1080 | pan | 7.021 | 11.305 | 12.526 | 854156 | 0.545 | 8.131 | 4.044 |
| 5000 | 1920x1080 | route | 7.383 | 9.528 | 11.483 | 870653 | 0.561 | 6.674 | 4.797 |
| 5000 | 3440x1440 | paused | 5.200 | 5.803 | 6.355 | 8213 | 0.866 | 0.223 | 5.383 |
| 5000 | 3440x1440 | live | 9.099 | 10.778 | 12.334 | 927530 | 0.557 | 6.734 | 6.239 |
| 5000 | 3440x1440 | zoom | 13.865 | 25.767 | 27.409 | 1135804 | 0.644 | 6.632 | 22.083 |
| 5000 | 3440x1440 | pan | 9.066 | 10.400 | 11.056 | 927008 | 0.549 | 5.707 | 5.935 |
| 5000 | 3440x1440 | route | 10.416 | 13.000 | 15.378 | 943842 | 0.555 | 6.783 | 8.816 |

Rows exceeding 12.5 ms are residual CPU bottlenecks, especially zoom at 5000 contacts. Stage p99 values are independent percentiles and must not be added. Unchanged paused scene reuse avoids geometry/paint rebuilds, but frame profiling and presented-frame bookkeeping still allocate; zero total allocations is not claimed.

## Native window matrix

Every report contains 600 swap-completion intervals, CPU submit, SwapBuffers wait, hardware/settings, 5 scripted interaction results and a matching PNG. All 80 actions passed (zoom, pan, pause, resume, select target). Scripts call production screen handlers during warmup; they are not physical mouse/keyboard acceptance.

| Objects | Size | Scale | Interval p99 ms | CPU submit p99 ms | Swap wait p99 ms | Mean FPS | Evidence |
|---:|---|---:|---:|---:|---:|---:|---|
| 500 | 1920x1080 | 0.8 | 56.616 | 9.439 | 46.328 | 21.57 | [JSON](native-500-1920x1080-0.8.json) / [PNG](native-500-1920x1080-0.8.png) |
| 500 | 1920x1080 | 1.2 | 50.138 | 8.820 | 43.932 | 21.67 | [JSON](native-500-1920x1080-1.2.json) / [PNG](native-500-1920x1080-1.2.png) |
| 500 | 1920x1080 | 1.5 | 51.419 | 7.996 | 45.569 | 21.60 | [JSON](native-500-1920x1080-1.5.json) / [PNG](native-500-1920x1080-1.5.png) |
| 500 | 1920x1080 | 1 | 51.126 | 8.630 | 45.423 | 21.71 | [JSON](native-500-1920x1080-1.json) / [PNG](native-500-1920x1080-1.png) |
| 500 | 3440x1440 | 0.8 | 54.095 | 11.399 | 45.306 | 21.57 | [JSON](native-500-3440x1440-0.8.json) / [PNG](native-500-3440x1440-0.8.png) |
| 500 | 3440x1440 | 1.2 | 49.882 | 9.184 | 41.180 | 24.91 | [JSON](native-500-3440x1440-1.2.json) / [PNG](native-500-3440x1440-1.2.png) |
| 500 | 3440x1440 | 1.5 | 49.742 | 8.530 | 43.331 | 25.42 | [JSON](native-500-3440x1440-1.5.json) / [PNG](native-500-3440x1440-1.5.png) |
| 500 | 3440x1440 | 1 | 51.958 | 10.313 | 45.050 | 21.50 | [JSON](native-500-3440x1440-1.json) / [PNG](native-500-3440x1440-1.png) |
| 5000 | 1920x1080 | 0.8 | 55.548 | 13.415 | 45.474 | 22.98 | [JSON](native-5000-1920x1080-0.8.json) / [PNG](native-5000-1920x1080-0.8.png) |
| 5000 | 1920x1080 | 1.2 | 54.222 | 12.310 | 43.538 | 24.23 | [JSON](native-5000-1920x1080-1.2.json) / [PNG](native-5000-1920x1080-1.2.png) |
| 5000 | 1920x1080 | 1.5 | 116.265 | 62.032 | 56.357 | 21.96 | [JSON](native-5000-1920x1080-1.5.json) / [PNG](native-5000-1920x1080-1.5.png) |
| 5000 | 1920x1080 | 1 | 54.759 | 12.033 | 43.666 | 24.19 | [JSON](native-5000-1920x1080-1.json) / [PNG](native-5000-1920x1080-1.png) |
| 5000 | 3440x1440 | 0.8 | 59.240 | 19.698 | 45.355 | 21.75 | [JSON](native-5000-3440x1440-0.8.json) / [PNG](native-5000-3440x1440-0.8.png) |
| 5000 | 3440x1440 | 1.2 | 55.847 | 17.783 | 45.531 | 22.29 | [JSON](native-5000-3440x1440-1.2.json) / [PNG](native-5000-3440x1440-1.2.png) |
| 5000 | 3440x1440 | 1.5 | 56.401 | 14.069 | 46.758 | 23.10 | [JSON](native-5000-3440x1440-1.5.json) / [PNG](native-5000-3440x1440-1.5.png) |
| 5000 | 3440x1440 | 1 | 69.515 | 32.632 | 45.070 | 21.03 | [JSON](native-5000-3440x1440-1.json) / [PNG](native-5000-3440x1440-1.png) |

All 16 native verdicts failed the 12.5 ms p99 gate. Swap wait is substantial; some cases also have CPU spikes. These measurements do not isolate the contribution of OS scheduling, driver, GPU execution or display scanout. Native pipeline frame details use the existing 30-second ring and may contain fewer than 600 entries; the native interval collector contains all 600.

Model image inspection: 1920x1080 at scale 1 and 1.5, 3440x1440 at scale 0.8 and 1.5. Player/selected target markers, route and priority labels are visible; panel text remains readable. High-scale command panels extend below the viewport and the diagnostic overlay overlaps their lower area; full panel accessibility/manual interaction is not accepted by these images. The synthetic player/target omit DisplayName, so the map correctly uses Unknown object while the info panel shows their IDs. Native modal/resize/locale matrix and human manual smoke remain NOT RUN. Headless lifecycle/locale tests are separate evidence.

## Known limitations and earlier probes

[pipeline-raster-zero-cadence-stress.json](pipeline-raster-zero-cadence-stress.json) preserves an interrupted probe whose synthetic route omitted turn cadence, triggering the legacy 1 ms fallback and about 200,000 path points per frame. The normal fixture was corrected to 250 ms; production projector behavior was not silently changed. The malformed/legacy zero-cadence path remains an expensive stress case (500/1920 route p99 about 87.8 ms, about 5 MB/frame). It is not comparable with the final normal matrix.

[preliminary-native-5000-1920x1080-1.json](preliminary-native-5000-1920x1080-1.json) is an earlier smoke, superseded by the final native matrix. Historical performance reports elsewhere retain their own dated results; no cross-run speedup is inferred from different fixtures.

Automated functional validation: Client 1826/1826; final Release build zero warnings/errors. See [US-0002-Review.md](US-0002-Review.md) and per-ticket evidence for coverage. No Engine/Motion/Contracts/save changes, no new gameplay balance or human acceptance claim.
