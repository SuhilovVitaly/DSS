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

### Content-driven simulation data

The engine holds no hardcoded game data. `EngineContentLoader` reads `Client/Settings.json` (copied to the output next to the client executable), which lists the type-data JSON files in `Client/Data/`:

| File | Defines |
|------|---------|
| `module-types.json` | Module types — slots, mass, structure points, power draw, `commandTypeIds`, base cycle time, fuel, inertia, success chance |
| `item-types.json` | Item types |
| `Data/Commands/<ModuleType>/commands.json` | Commands, addressed by `(objectId, moduleId)`; split per owning module type (Engine, Scanner, NavigationComputer, DrillingUnit), each entry carries a `type` field cross-checked against `ModuleTypeDefinition.CommandTypeIds` |
| `factory-types.json` | Factory types (loaded only when referenced by Settings) |
| `recipes.json` | Recipes (loaded only when referenced by Settings) |

All definitions land in `GameDataRegistry` (`Engine/Content/`). Scenarios (`Client/Scenarios/<name>/scenario.json`, e.g. `Default`, `Default_500`, `Docked`) are loaded by `ScenarioLoader` (`Engine/Scenario/`) and describe world state: game time, speed, player ship id, space objects with their installed modules. `scenarioMetadata` also carries an optional player-facing `description`, shown in the client's `ScenarioSelectScreen` (New Game picker); save files never set it. A scenario may carry a `masterSeed`; the `Default` scenario does not, so New Game generates one. RNG streams are derived from the master seed (`Engine/Rng/RngStreamSeedDerivation.cs`) — same seed + same command sequence ⇒ identical world. Save files (`Saves/quicksave.json` next to the client executable) are scenario JSON plus extra engine state and are loaded through the same `ScenarioLoader` with `allowNonZeroGameTime: true`.

`ScenarioRepository.ListScenarios` (`Engine.LocalClient/`) recursively finds every `scenario.json` under `Client/Scenarios/` for the New Game picker (`IGameSessionFactory.ListScenarios`), skipping any file that fails to parse/validate. Starting a session from a specific picked scenario (rather than `Settings.json`'s `defaultScenario`) goes through `IGameSessionFactory.CreateSessionFromScenario` → `LocalGameSessionConnection.CreateFromScenarioFile` → `SimulationEngine.CreateFromScenarioFile` → `EngineContentLoader.CreateEngineFromScenarioFile`.

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
| `Replace(screen)` | Transition (e.g. NEW GAME → ScenarioSelect, or picking a row → GameSession) |
| `ReplaceAll(screen)` | Return to root (e.g. MAIN MENU) |

NEW GAME from `MainMenuScreen` no longer starts a session directly — it replaces the current screen with `ScenarioSelectScreen`; only picking a row there (`ScreenEvent.ScenarioSelected`, scenario path carried the same way `LoadSlotRequested` carries a slot id) actually creates the session and replaces the screen with `GameSessionScreen`.

Screen folders:
```
UI/Screens/
├── IScreen.cs              (shared interface + ScreenEvent enum)
├── MainMenu/               (MainMenuScreen + MenuLayout)
├── ScenarioSelect/          (ScenarioSelectScreen + ScenarioSelectLayout — New Game scenario picker)
├── GameMenu/                (GameMenuScreen + GameMenuLayout)
├── GameSession/             (GameSessionScreen + command panel, tactical map, labels, trails)
├── Settings/                (SettingsScreen + SettingsLayout)
├── Save/                    (SaveScreen + SaveLayout)
├── Load/                    (LoadScreen + LoadLayout)
├── Finance/                 (FinanceScreen + FinanceLayout)
└── Ship/                    (ShipScreen + ShipLayout)
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

## Modal Pause Rule

**In single-player mode, any open modal screen automatically stops the authoritative simulation.**

While at least one modal screen exists (modal depth ≥ 1):
- `GameTimeMs` does not increase;
- simulation ticks do not advance the game world;
- object movement stops;
- module cycles do not progress;
- game turn processing / scheduled simulation events do not execute;
- RNG simulation events do not execute.

Client rendering and UI continue to run at the target frame rate.

When the first modal screen opens, the current simulation speed is saved. The previous speed is restored only after the last modal screen closes. Nested modal screens (e.g. GameMenu → Settings → Confirmation) pause only once on the first and resume only once on the last. If the game was already at `Speed0` before the modal, it stays at `Speed0` after.

Session-control is done via `IGameSessionConnection.SetSimulationSpeedAsync(SimulationSpeed)`. The confirmed speed is included in `AuthoritativeSnapshot.CurrentSpeed`. Additionally, `SnapshotBuffer.CurrentSpeed` stores the client-side authoritative speed tracker, updated immediately after `SetSimulationSpeedAsync` completes — this allows the renderer to gate prediction without waiting up to 1 second for the next snapshot. Client-side motion prediction reads `SnapshotBuffer.CurrentSpeed`: at `Speed0`, prediction delta is zero regardless of real elapsed time.

## Current state

Playable end-to-end pipeline: Engine → Snapshot → Connection → Buffer → Renderer, with content-driven simulation (module/item/command/factory/recipe definitions from JSON), scenario loading, master-seed RNG, save/load, tactical map with object selection, ship command panel, object labels and trails, camera pan/zoom, UI scale (100/120/150%), settings screen, and modal pause. Engine behavior requirements live in `deep_space_saga_engine_requirements.md` (source of truth; see `AGENTS.md`).
