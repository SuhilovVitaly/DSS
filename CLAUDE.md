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

The defining architectural invariant: the **engine is a pure .NET library with zero graphics dependencies**. The client/renderer communicates with the engine exclusively through a single async interface boundary.

```
Engine → IGameSessionConnection → LocalGameSessionConnection → Client
                                                                  ├── SnapshotBuffer
                                                                  ├── MotionPredictor
                                                                  └── Renderer @ 80 FPS
```

- **`IGameSessionConnection`** (in `Contracts`) — async/message-oriented interface. Same shape for in-process and network implementations.
- **`LocalGameSessionConnection`** (in `Engine.LocalClient`) — in-process adapter. Replaceable with a future `NetworkGameSessionConnection` without client changes.
- **Render loop never calls the engine synchronously per frame.** It reads from client-side state (`SnapshotBuffer` + motion prediction).

### Timing constants

| Parameter | Value |
|-----------|-------|
| Internal simulation tick | 100 ms |
| Authoritative snapshot interval | 1000 ms (1 Hz) |
| Target render FPS | 80 |

### Project graph

| Project | Role | Depends on |
|---------|------|------------|
| `DeepSpaceSaga.Contracts` | DTOs, `IGameSessionConnection`, `PlayerCommand`, `AuthoritativeSnapshot`, `ObjectMotionSnapshot` | *nothing* |
| `DeepSpaceSaga.Motion` | Deterministic motion math + prediction (`IMotionPredictor`, `LinearMotionPredictor`). Shared by engine and client. | `Contracts` |
| `DeepSpaceSaga.Engine` | Authoritative simulation (pure logic, no graphics). Produces immutable snapshots. | `Contracts`, `Motion` |
| `DeepSpaceSaga.Engine.LocalClient` | `LocalGameSessionConnection` — in-process gateway, bridges Engine ↔ Client | `Contracts`, `Engine`, `Motion` |
| `DeepSpaceSaga.Client` | Executable: Silk.NET window, SkiaSharp GPU renderer, input, `SnapshotBuffer`, screens | `Contracts`, `Motion`, `Engine.LocalClient` |

**Direction rule:** `Client` → `Engine.LocalClient` → `Engine`. `Client` does **NOT** reference `Engine` directly. `Contracts` references nothing. Only `Client` pulls in graphics packages (SkiaSharp, Silk.NET).

### Client ↔ Engine data flow

```
Engine (authoritative)
  │
  │  produces immutable AuthoritativeSnapshot every 1 s
  │  uses DeepSpaceSaga.Motion for deterministic position calculation
  ▼
IGameSessionConnection (async boundary)
  │
  │  IAsyncEnumerable<AuthoritativeSnapshot>
  ▼
Client receive loop (background task, GameSessionHandle)
  │
  ▼
SnapshotBuffer → BufferedSnapshot (snapshot + Stopwatch timestamp, atomic)
  │
  ▼
MotionPredictor (client-side, LinearMotionPredictor from DeepSpaceSaga.Motion)
  │
  ▼
Renderer @ 80 FPS (reads only client-side state)
```

**Critical rules:**
- Renderer never queries Engine directly.
- Renderer works only with data already in client memory.
- `LocalGameSessionConnection` and future `NetworkGameSessionConnection` implement the same `IGameSessionConnection` — client code never knows the difference.
- Motion prediction is client-side only; it never modifies authoritative state.
- Unconfirmed commands do not affect prediction.
- `BufferedSnapshot` bundles snapshot + receipt timestamp atomically (single `Interlocked.Exchange`).
- Both Engine and Client use the same `DeepSpaceSaga.Motion` library for deterministic position calculation.

### Motion conventions

| Property | Convention |
|----------|-----------|
| Speed | km/s |
| Direction | degrees, 0° = up, 90° = right, clockwise |
| World units | 1 unit = 100 m, so 1 km/s = 10 world units/s |
| Sun position | (0, 0) |
| Map boundaries | farthest orbital radius + 10% |

### Client navigation

Screens are managed by `ScreenStack` (`UI/ScreenStack.cs`):

| Method | Use |
|--------|-----|
| `SetRoot(screen)` | Set first screen |
| `Push(screen)` | Open overlay (e.g. Esc → GameMenu) |
| `Pop()` | Close overlay (e.g. RESUME) |
| `Replace(screen)` | Transition (e.g. NEW GAME → GameSession) |
| `ReplaceAll(screen)` | Return to root (e.g. MAIN MENU) |

Screen folders:
```
UI/Screens/
├── IScreen.cs              (shared interface + ScreenEvent enum)
├── MainMenu/               (MainMenuScreen + MenuLayout)
├── GameMenu/                (GameMenuScreen + GameMenuLayout)
└── GameSession/             (GameSessionScreen)
```

Shared UI style: `UI/Controls/MenuStyle.cs` (Verdana fonts, DSS button colors, hover/pressed/disabled states).

### Custom cursors

PNG cursor images in `Images/Cursors/`:
- `cursor.png` — default cursor (resized 26×26)
- `cursor-selected.png` — hover over interactive elements

Loaded at startup via Silk.NET `ICursor.Image`. Fallback to standard cursor if files missing.

### Composition root (`Program.cs`)

`Client/Program.cs` is the only place that wires concrete implementations. Engine is created via `IGameSessionFactory`, and the rest of the client sees only `IGameSessionConnection` and `SnapshotBuffer`. Swapping in a network client is a one-line change here.

## Key conventions

- **TFM:** `net8.0`, **namespace root:** `DeepSpaceSaga.*`
- **Central package management:** all versions in `Directory.Packages.props`; `PackageReference` without `Version` in `.csproj` files
- **Nullable enabled, implicit usings, warnings as errors** across all projects
- **Coordinate system:** `double` precision, `1 unit = 100 m`, Sun at (0,0)
- **Map boundaries:** defined by the maximum orbital radius of the farthest world object + 10%
- **Commands** addressed by `(objectId, moduleId)` key
- Graphics/Skia/GL types must **never** cross the `IGameSessionConnection` boundary — only JSON-serializable domain DTOs

## Current state

Architectural skeleton with end-to-end pipeline: Engine → Snapshot → Connection → Buffer → Renderer. MainMenu and GameMenu screens with overlay navigation. No gameplay mechanics yet.
