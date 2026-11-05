---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0006-client-input-and-controls
ticket: EP-0015-US-0006-TK-0007-linux-native-runtime
title: Linux native runtime for actual glyph/raster checks
stage: in-progress
layer: client-build
depends_on: ["EP-0015-US-0006-TK-0005-widgets-lists"]
files_touched: 3
serves: [AC-05]
created: 2026-11-05
revision: 1
---

# Linux native runtime

## Why and reproduction

Required glyph/raster validation at code cab5d313 failed on Ubuntu run42112081477 / job127541943500: Client80 pass/17 fail, all failing with missing libSkiaSharp. Motion1376 and Engine378 PASS. Full log: D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-text-01/linux-failed.log. Existing SkiaSharp managed package has no Linux native runtime reference; new real raster tests expose this prerequisite. This is not a glyph assertion failure or justification to exclude tests.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| Directory.Packages.props | SkiaSharp2.88.8 managed pin | Matching SkiaSharp.NativeAssets.Linux2.88.8 central pin |
| src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj | managed SkiaSharp reference only | Add official matching Linux native runtime dependency, transitively available to Client.Tests |
| .github/workflows/tactical-battle.yml | Ubuntu raster tests lack explicit host prerequisites | Ensure fontconfig/fonts and verify native library dependencies before tests |

## Decisions and scope

Technical recovery under authorized epic execution; no new combat/product rules. A separate three-file build ticket preserves R-03 size of the five-C#-file text ticket. TK-0006 now depends on this prerequisite before completion. No managed API upgrade or test exclusion. Official package/source evidence: [NuGet2.88.8](https://www.nuget.org/packages/SkiaSharp.NativeAssets.Linux/2.88.8), [SkiaSharp Linux guidance](https://github.com/mono/SkiaSharp#using-skiasharp). Existing package version preserved; Linux system fontconfig remains a host prerequisite. Final US-0017 documentation must record it.

## Acceptance and validation

Windows restore/build and owning Client tests pass without changing rendering assertions. Ubuntu required glyph/raster cases execute with actual native Skia, no DllNotFoundException; workflow checks library presence/dependencies. Test processes capped300000ms. All outputs under absolute task validation root (CI /tmp equivalent). CPU raster is not native window/GPU/physical input acceptance. Self-review, scoped config/diff check, separate ticket commit/immediate origin push and remote SHA verification required; user explicitly authorized current GitHub origin and revoked D-USER-09/R-06. Linux result/evidence recorded before TK-0006 and next functional ticket.

## Windows validation and CI publication — 2026-11-05

Official NativeAssets.Linux2.88.8 reference added with unchanged managed SkiaSharp2.88.8. Native linux-x64 libSkiaSharp.so is present transitively in Client.Tests output; actual path/length/SHA256 retained in D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-linux-runtime-01/native-runtime.json. Restore/build PASS0 warnings/errors; owning Client113/113 PASS, including unchanged glyph/raster assertions and existing text input. XML parse and diff/self-review PASS. Workflow explicitly installs fontconfig/DejaVu host prerequisites and checks linux-x64 .so dependencies with ldd before raster cases. Linux confirmation pending this push; both this card and TK-0006 remain in-progress. No native window/GPU claim.
