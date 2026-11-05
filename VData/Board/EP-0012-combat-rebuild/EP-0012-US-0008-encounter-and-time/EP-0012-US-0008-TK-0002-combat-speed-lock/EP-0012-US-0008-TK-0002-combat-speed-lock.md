---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0008-encounter-and-time"
ticket: "EP-0012-US-0008-TK-0002-combat-speed-lock"
title: "combat-speed-lock"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0008-TK-0001-encounter-state"]
files_touched: 7
serves: ["US8-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0008-TK-0002-combat-speed-lock

## Why

[Боевое столкновение и время](../EP-0012-US-0008-encounter-and-time.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-speed-lock; доказать US8-AC2 в границах этой карточки.

## Decisions / Assumptions

A-SPEED-FRAME-FIX: actual compact frames within high-speed admission must carry their confirmed point-encounter speed, rather than the publication interval starting speed. Existing Presentation.cs and named test scope covers the fix; legacy interval mapping remains unchanged. Reproduce with injected real clock and actual CapturePresentationUpdate before implementation.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (7 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationClock.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationClock.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationClock.cs:9; public readonly record struct SimulationClockState(long GameTimeMs, SimulationSpeed Speed, | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs` | Existing | Same bounded clock/control policy for presentation updates and atomic rejection acknowledgement. |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | Existing | Modal resume respects Active lock and preserves quarter remainder. |
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | Existing | Return immediate combat_speed_locked reason with confirmed speed. |

A-SPEED-01: Engine.TrySetSpeed returns the rejection reason; SetSpeed keeps its existing void facade. High-speed point sessions consume a captured real interval in one-real-millisecond clock steps, so admission takes effect at the next real-millisecond boundary instead of advancing an entire high-speed snapshot interval. Quarter remainder is preserved; no Reset for encounter transitions. Active permits 0/1 only; prior speed restored on Won unless a watchdog/modal/terminal pause owns the clock. User-requested modal resume remains authoritative. Save18 persists restore state later.

## Public API after the change

Add Engine.TrySetSpeed and atomic SetPresentationSpeed/ResumePresentationControl; existing SetSpeed/ResumePresentation callers remain compatible.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `SimulationClock` и обработчик `SetSpeed`. Учесть, что модальные окна ставят Speed0 (`SimulationEngine.cs:785`).
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Bounded Engine plus LocalClient acknowledgement integration; matching Engine tests only. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Combat_speed_locks_and_restores_previous**: Speed2+ rejected combat_speed_locked; 0/1 accepted; terminal defeat остаётся pause.
- **Modal_pause_and_quarter_remainder_survive_combat**: 1физическая секунда=4реальных, дробный остаток не теряется.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0008TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0008TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationClock.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US8-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 11 named / 688 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0008-tk0002-results.json).

Active accepts Speed0/1 and rejects Speed2+ with combat_speed_locked. Engine and LocalClient return immediate atomic acknowledgement with confirmed speed. Prior speed restored on Won; terminal and watchdog pauses remain authoritative, dialogue resume is capped while Active. Bounded real clock intervals apply admission and victory restoration within the captured interval at integer real-ms resolution; x1=1 physical second per4 real seconds and quarter remainder survive pause/modal changes. A real docking dialogue is tested; initial fixture mismatched station/ship heading and was corrected without relaxing docking. Solution build 0 warnings/errors. Save18 restore-state persistence remains downstream.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 follow-up: reproduced5 of6 Active compact frames publishing Speed4 rather than confirmed Speed1. Fixed point frame projection; PASS12/12 named,782/782 bounded Engine, solution build0 warnings/errors, scoped formats/diff PASS. [Actual baseline and fixed results](../../evidence/compact-speed-fix-results.json).
