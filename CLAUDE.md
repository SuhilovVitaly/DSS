# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Deep Space Saga (DSS) — 2D sci-fi simulation of Solar System development. .NET 8.0, target 80 FPS, Windows + macOS. GPU-accelerated rendering via SkiaSharp over Silk.NET OpenGL window.

## Build, Test, Run

```bash
dotnet build DeepSpaceSaga.sln          # Build all projects
dotnet test DeepSpaceSaga.sln           # Run all tests (xUnit)
dotnet run --project src/DeepSpaceSaga.Client  # Launch the client window
```

Build treats warnings as errors (`TreatWarningsAsErrors=true` in `Directory.Build.props`). Tests use xUnit — individual tests can be filtered with `dotnet test --filter "FullyQualifiedName~SmokeTests"`.

## Architecture

### Strict engine/renderer separation

The defining architectural invariant: the **engine is a pure .NET library with zero graphics dependencies**. The client/renderer communicates with the engine exclusively through a single interface boundary.

```
Game Client → IGameSessionConnection → LocalGameSessionConnection → .NET Engine
```

- **`IGameSessionConnection`** (in `Contracts`) — the sole interface between client and engine. Fully async / message-oriented. No synchronous per-frame calls into the engine.
- **`LocalGameSessionConnection`** (in `Engine.LocalClient`) — in-process implementation. Replaceable with a future `Engine.NetworkClient` without changing client or renderer code.
- **Render loop never calls the engine synchronously per frame.** It draws from client-side state (last snapshot + motion prediction).

### Project graph

| Project | Role | Depends on |
|---------|------|------------|
| `DeepSpaceSaga.Contracts` | DTOs, commands, `IGameSessionConnection`, `AuthoritativeSnapshot` | *nothing* |
| `DeepSpaceSaga.Motion` | Deterministic motion/orbit math + prediction. Shared by engine and client. | `Contracts` |
| `DeepSpaceSaga.Engine` | Authoritative simulation (pure logic, no graphics) | `Contracts`, `Motion` |
| `DeepSpaceSaga.Engine.LocalClient` | `LocalGameSessionConnection` — in-process gateway | `Contracts`, `Engine`, `Motion` |
| `DeepSpaceSaga.Client` | Executable: Silk.NET window, SkiaSharp GPU renderer, input, client state | `Contracts`, `Motion`, `Engine.LocalClient` |

**Direction rule:** `Client` → `Engine.LocalClient` → `Engine`. `Client` does **NOT** reference `Engine` directly. `Contracts` references nothing. Only `Client` pulls in graphics packages (SkiaSharp, Silk.NET).

### Composition root (`Program.cs`)

`Client/Program.cs` is the only place that wires concrete implementations — creates `SimulationEngine`, wraps in `LocalGameSessionConnection`, casts to `IGameSessionConnection`, passes to `SkiaWindow`. Swapping in a network client is a one-line change here.

## Key conventions

- **TFM:** `net8.0`, **namespace root:** `DeepSpaceSaga.*`
- **Central package management:** all versions in `Directory.Packages.props`; `PackageReference` without `Version` in `.csproj` files
- **Nullable enabled, implicit usings, warnings as errors** across all projects
- **Coordinate system:** `double` precision, `1 unit = 100 m`, Sun at (0,0), unbounded map (not yet implemented — reserved convention)
- **Simulation timing** (reserved, not yet implemented): internal tick 100 ms, authoritative tick 1 s, snapshots at 1 Hz + client-side prediction
- **Commands** addressed by `(objectId, moduleId)` key (marker type only for now)
- Graphics/Skia/GL types must **never** cross the `IGameSessionConnection` boundary — only JSON-serializable domain DTOs

## Current state

This is a **compilable scaffolding** — empty projects with marker types, zero game logic. The client opens a Silk.NET window and clears the screen to dark navy blue via SkiaSharp GPU rendering. No simulation, no input handling, no prediction.
