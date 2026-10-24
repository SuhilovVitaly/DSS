---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0012-maneuver-and-threat-ui"
ticket: "EP-0012-US-0012-TK-0003-threat-alerts"
title: "threat-alerts"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0012-TK-0002-threat-panel"]
files_touched: 9
serves: ["US12-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0012-TK-0003-threat-alerts

## Why

[Манёвр и угрозы в UI](../EP-0012-US-0012-maneuver-and-threat-ui.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: threat-alerts; доказать US12-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Bounded scope (9 files), updated before code. Accessible styles must affect actual projectile trace/marker, and native composition must consume an actual audio dispatcher; a test-only cue would not satisfy AC3. A-PALETTE-FIXTURE: broad regression passed 2256/2257; the existing canonical palette assertion still expects 19 root properties. Add the new strict threatAlerts group to that exact schema check while retaining every previous color assertion.

| File | Allowed change |
|---|---|
| `tests/DeepSpaceSaga.Client.Tests/CombatPaletteContentTests.cs` | Include the new presentation group in exact canonical schema; retain every existing color/value check |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs` | NEW displayed-only alert state, class styles, launch flash and offscreen indicators |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Actual alert update/render and optional audio sink; colored projectile marker |
| `src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json` | Explicit accessible class styling, real-time cadence and audio presentation settings |
| `src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs` | Strict optional threatAlerts group, preserving existing v1 palette compatibility |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.PointProjectiles.cs` | Apply class width/dash/color to actual observed torpedo trace |
| `src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs` | NEW real Windows audio dispatcher; nonblocking UI, no Engine state |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Wire native audio into both new/load game screen composition paths |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0003Tests.cs` | NEW real-time rate/replay/classification/flash/edge, configuration and actual scene admission tests |

A-ALERTS: all input comes from the currently displayed admitted scene. New/load session baselines existing facts without replay; ongoing current danger may resume only after its cadence. Launch, danger and urgent each emit at most once per 1500 real ms; normal danger repeats every 3000 ms, urgent (boundary <=3 real sec) every 1500 ms. Paused/modal/inactive scenes suppress periodic sound without queueing a backlog. Enemy launches require public current/previous admitted actor relation. Offscreen arrows use public current enemy positions. Headless tests capture the same audio cue boundary without playing native sound; SkiaWindow supplies the concrete Windows sound sink. Native heard-audio acceptance remains separate.

## Public API after the change

Client-only CombatAlertCue audio boundary and optional GameSessionScreen audio sink; native window always provides the concrete implementation. No Contracts/Engine change. threatAlerts is an optional strict presentation-only palette group.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Implement accessible observed classes, displayed launch/periodic warning state, flash/edge and actual native audio composition.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Threat_alerts_are_rate_limited_and_accessible**: один type/1.5real sec; urgent<=3real sec; толщина/штрих различают классы.
- **Offscreen_hostile_launch_has_edge_indicator**: звук/flash/edge по принятому событию, Load не replay old alerts.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0012TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0012TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US12-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 22 named / 2258 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0012-tk0003-results.json).

Observed torpedo trace/marker classes use distinct width/dash/color from data. Displayed enemy launches produce real-time flash/audio cue and offscreen public-enemy arrows; session/load baselines do not replay old facts. Warning cadence 3s vs urgent 1.5s at <=3 real seconds, with shared 1.5s floor and actual playback-start gate. Native Windows sound dispatcher is wired for new/load screens and activation epochs discard old queued cues; heard-audio/native acceptance NOT RUN. Pause suppresses periodic sound without backlog; audio failure preserves visuals. Headless class/edge render inspected; 145 related visual/input checks PASS. Retained initial 2256/2257 palette schema FAIL; exact new group schema fixed with all previous RGBA assertions retained. Strict legacy ForecastBudget, final catalog, native/product remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
