---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0012-maneuver-and-threat-ui"
ticket: "EP-0012-US-0012-TK-0001-evade-input-and-threat-select"
title: "evade-input-and-threat-select"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0011-TK-0004-launch-commands-and-reasons"]
files_touched: 8
serves: ["US12-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0012-TK-0001-evade-input-and-threat-select

## Why

[Манёвр и угрозы в UI](../EP-0012-US-0012-maneuver-and-threat-ui.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: evade-input-and-threat-select; доказать US12-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Bounded scope (8 files), updated before code under EpicExecutionPrompt authority. Existing native tracker and bridge must consume the data bindings and send explicit threat/side/direction/active payloads; pure visual helpers would not satisfy AC1.

| File | Allowed change |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Actual key down/up, threat click, render and focus-loss hooks |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs` | NEW observed threat selection, held evasion preview, release/turn/full-speed and visuals |
| `src/DeepSpaceSaga.Client/CombatInputSettings.cs` | Load/validate/map maneuver bindings; optional additions preserve schema1 existing files |
| `src/DeepSpaceSaga.Client/Data/UI/combat-input.json` | A/D, Tab, left/right and Shift bindings |
| `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` | Shift modifier edges precede actions; mapped maneuver keys reach real native input |
| `src/DeepSpaceSaga.Client/GameSessionHandle.cs` | Actual explicit evade and turnHold command payloads, existing authoritative submission |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs` | Optional enabled-engine, distant-enemy/no-flight real fixture for maneuver vs noncombat tests; existing defaults retained |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0001Tests.cs` | NEW actual input/transport/Engine and pure observed-preview checks |

A-MANEUVER-INPUT: Shift starts the existing cyclic engine.accelerate command toward engine maximum; it does not teleport velocity. Left/right hold uses engine.turnHold during Active combat or live observed torpedo danger, retaining legacy step controls outside combat. A/D preview uses only displayed projectile ray, own max speed and BlastThreatMath deadline; exit time is the conservative time to clear the R+0.5 km ray corridor at the commanded perpendicular maximum speed. The estimate must also fit the executor episode duration (observed closest-approach time + 0.5 physical seconds). No hidden fuse is assumed. Explicit click/Tab changes current threat; disappearance cancels a held evasion instead of sending against an automatic replacement. Focus loss/dialogue/pause/load clear held state.

## Public API after the change

GameSessionHandle exposes explicit point-maneuver submission methods using existing PlayerCommand fields; no Contracts/Engine changes. CombatInputSettings adds optional maneuver bindings; no simulation state.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Implement actual data-driven maneuver input, observed preview and engine payloads; preserve authoritative motion and rejection semantics.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Evade_preview_on_hold_command_on_release**: A/D и выбор угрозы click/Tab, default min entry time.
- **Arrow_hold_release_and_shift_send_turn_and_full_speed**: focus loss/cancel не оставляет held input.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0012TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0012TK0001Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US12-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 12 named / 2227 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0012-tk0001-results.json).

Data-driven A/D hold previews public observed corridor exit; only release sends real evade. Current threat defaults to observed minimum entry time with ordinal ties; click/Tab switch explicitly. Continuous turn handles direction changes, release, focus loss, pause/load; outside combat arrows retain step commands. Shift starts existing cyclic acceleration without instantaneous speed changes. Actual bridge/Engine and remap tests PASS, 89 related input checks PASS. Own base-course arrow and disabled-engine countdown headless render inspected; timer freezes on pause and icon avoids own plaque. Strict legacy ForecastBudget, final data integration and native/product remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
