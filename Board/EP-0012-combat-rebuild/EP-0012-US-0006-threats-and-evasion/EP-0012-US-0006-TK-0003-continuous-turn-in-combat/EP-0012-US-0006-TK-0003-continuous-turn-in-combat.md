---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0006-threats-and-evasion"
ticket: "EP-0012-US-0006-TK-0003-continuous-turn-in-combat"
title: "continuous-turn-in-combat"
stage: "implemented"
layer: "contracts-motion-engine"
depends_on: ["EP-0012-US-0006-TK-0002-evade-command"]
files_touched: 12
serves: ["US6-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0006-TK-0003-continuous-turn-in-combat

## Why

[Угрозы и уклонение](../EP-0012-US-0006-threats-and-evasion.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: continuous-turn-in-combat; доказать US6-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (12 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0006TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `src/DeepSpaceSaga.Engine/RuntimeMotion.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TurnHold.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |
| `tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0006TK0003Tests.cs` | Bounded integration dependency | Continuous turn, cancellation, shared arc and regression only. |

A-TURN-01: turnHold is an explicit continuous control; existing step commands remain unchanged outside combat. Encounter/UI routing is integrated by later owning tickets. Accepted turnHold exposes only the own signed rate; foreign controls remain masked. Pausing clears the latch at the last confirmed motion time. Save18 persistence belongs to US10.

## Public API after the change

Proposed команда engine.turnHold {direction,active}; payload/contracts готовит US17-TK4, Engine использует те же единицы °/физическую секунду.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Команда удержания поворота: `engine.turnHold {direction, active}`. Остановка поворота по отпусканию. Сверить с текущим каталогом команд Engine.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Bounded cross-layer prerequisite: Contracts own-control scalar, shared Motion arc and Engine execution; matching Engine tests exercise the shared implementation. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Turn_hold_integrates_until_release**: continuous rate max engine, active=false останавливает; вне боя прежний step.
- **Turn_hold_stops_on_pause_disable_and_session_reset**: нет застрявшего поворота.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0006TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0006TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0006TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US6-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 7 named / 494 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0006-tk0003-results.json).

TurnHold integrates at the engine signed angular rate and stops on matching release, pause/watchdog/dialogue, damage, session reset or accepted replacement. Wrong-key release, replay and invalid replacement preserve control. Shared Motion 3 named / 243 full PASS; solution build 0 warnings/errors. Existing outside-combat step semantics retained. Encounter/UI routing and Save18 remain downstream; explicit turnHold is accepted independently of encounter state. The first targeted run had 5 pass / 1 fail because the fixture used noncanonical engine.cancelAll; corrected to canonical maintainCourse without production workaround.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
