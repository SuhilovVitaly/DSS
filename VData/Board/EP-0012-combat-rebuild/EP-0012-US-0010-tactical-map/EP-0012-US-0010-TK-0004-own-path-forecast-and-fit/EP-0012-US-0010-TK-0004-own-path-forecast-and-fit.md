---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0010-tactical-map"
ticket: "EP-0012-US-0010-TK-0004-own-path-forecast-and-fit"
title: "own-path-forecast-and-fit"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0010-TK-0003-persistent-range-rings"]
files_touched: 8
serves: ["US10-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0010-TK-0004-own-path-forecast-and-fit

## Why

[Тактическая карта](../EP-0012-US-0010-tactical-map.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: own-path-forecast-and-fit; доказать US10-AC3 в границах этой карточки.

## Decisions / Assumptions

A-OWN-FIT: Active encounter replaces long own previews with shared-current-maneuver5s physical path and1s tangent velocity vector. The vector scale is a technical display convention. BareF fits public current enemy ships/projectiles, own captured endpoint circles and own5s path into AvailableMapRect; no foreign fuse, infinite ray horizon or future AI segment is fitted. Ctrl+F remains finance. Include actual KeyboardEdgeTracker and existing shortcut tests because bareF is currently gated out; modifiers must precede F in one poll, focus resync still suppresses held keys. Change obsolete bare-F suppression assertions to the new binding, preserve Ctrl+F finance assertion with explicit modifier. Point projectiles use their dedicated renderer rather than generic selected-object future previews.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (8 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs:6; internal enum MapFitMode { Target, Route, System } | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:14; public sealed partial class GameSessionScreen : IScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs:8; internal static class CombatTrajectoryProjector | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` | Existing Ctrl-only F binding | Real bareF delivery, modifier ordering, focus resync and retained finance shortcut tests. |
| `tests/DeepSpaceSaga.Client.Tests/KeyboardEdgeTrackerTests.cs` | Existing Ctrl-only F binding | Real bareF delivery, modifier ordering, focus resync and retained finance shortcut tests. |
| `tests/DeepSpaceSaga.Client.Tests/MechanicsPanelTests.cs` | Existing Ctrl-only F binding | Real bareF delivery, modifier ordering, focus resync and retained finance shortcut tests. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailStore.cs` | Actual own held-turn screen also rejects negative bootstrap time | Start newly observed forward-only controls with current pose; preserve actually recorded history. |

A-OWN-TRAIL: baseline6/6 failures include two real screen negative-time bootstrap exceptions for public TurnHold. Do not reconstruct pre-command history from a current turn/evasion; collect real admitted poses from first observation.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Own_path_uses_current_confirmed_turn_for_five_seconds**: дуга из current command без enemy secret inputs.
- **Fit_battle_includes_relevant_ships_and_projectiles**: F помещает бой в свободный map viewport, не под панели.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0010TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0010TK0004Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US10-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 8 named / 2171 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0010-tk0004-results.json).

Baseline6/6 failures reproduced: bareF suppressed/finance, own forecast801points instead51 and held-turn history negative-time crash. Active own path now5physical seconds via public shared Motion sampler, independent left/right20deg/s trig expected endpoints, plus1physical-second tangent velocity vector. Long own navigation preview replaced by5s path while confirmed goal marker remains. F fits public current combat ships/projectiles, own endpoint circles, own path/vector in actual AvailableMapRect; excludes unrelated station and foreign unbounded ray/fuse. Actual Engine circle fits outside panels at100/150%DPI;1280x720@150% synthetic layout also verified useful/nonoverlapping. Ctrl+F finance and focus resync retained with modifier-first physical edges. No backwards reconstruction for new turn/evasion history. PASS59 controls/trails checks,2171 nonbudget Client; build/format/diff PASS. Strict legacy ForecastBudget/native gates remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
