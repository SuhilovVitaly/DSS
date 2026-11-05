---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0006-threats-and-evasion"
ticket: "EP-0012-US-0006-TK-0002-evade-command"
title: "evade-command"
stage: "implemented"
layer: "engine-contracts-motion"
depends_on: ["EP-0012-US-0004-TK-0002-countermissile-detonation"]
files_touched: 12
serves: ["US6-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0006-TK-0002-evade-command

## Why

[Угрозы и уклонение](../EP-0012-US-0006-threats-and-evasion.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: evade-command; доказать US6-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist 12 файлов. Для настоящего движения и возврата недостаточно private Engine dictionary: collision sampling и own forecasts должны пользоваться общей кусочной Motion-моделью. Добавляется owner-only Evasion snapshot; NPC metadata не публикуется. Scope расширен до перечисленных consumers до реализации.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs:6; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0006TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| `src/DeepSpaceSaga.Contracts/EvasionSnapshot.cs` | NEW | Owner-known current evasion schedule and captured base course/orbit. |
| [src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs](../../../../../src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs) | Existing transport | Optional owner-only Evasion. |
| [src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs](../../../../../src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs) | Shared conditional motion | Same piecewise evasion/base continuation as Engine. |
| [src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs](../../../../../src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs) | Shared interpolation dispatcher | Admit new point/evasion DTOs and exclude from linear fast path. |
| [src/DeepSpaceSaga.Engine/RuntimeMotion.cs](../../../../../src/DeepSpaceSaga.Engine/RuntimeMotion.cs) | Authoritative sampler | Use shared evasion model. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs) | Collision and damage consumers | Shared fractional sampling; engine damage interrupts return control. |
| `tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0006TK0002Tests.cs` | NEW | Piecewise path/deadline/base orbit parity. |
| `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0006TK0002Tests.cs` | NEW | Own schedule roundtrip and null compatibility. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

Execution API refinement: EvasionSnapshot(SampleMotionTimeMs, EndsAtMotionTimeMs, ThreatProjectileId, Side, BaseMotion) optional on ObjectMotionSnapshot. BaseMotion has Evasion=null and captures only the actor's course/orbit continuation; published only for player ship. Engine maintains suspended orbit cycle and episode count, with Save18 capture/restore in US9. D11 applies full engine speed/perpendicular direction immediately; this is a direct maneuver control, without a second invented steering phase. t_star is captured from pre-evade relative velocity and observed direction/speed/R, never endpoint/deadline. Exact fractional evasion ends at t_star+.5s; integer-ms base command scheduling resumes on ceil(end), coasting on the base velocity across that sub-ms interval. Repeated evades preserve original base and count one active episode. Valid new engine input takes control; rejected input leaves evasion intact. Engine damage prevents an automatic return using an unavailable engine. These are implementation choices, not additional user decisions.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Команда, состояние уклонения корабля (`EvadeUntilMotionMs`, базовый курс) и автовозврат. Используется и в ИИ.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Evade_uses_trace_perpendicular_and_returns_to_base**: side Left/Right относительно направления угрозы, полный ход, end=t_star+.5с.
- **Evade_rejects_missing_foreign_threat_or_disabled_engine**: unknown_threat/engine_disabled; pause не двигает state.
- **Evade_preserves_base_course_and_counts_once**: повтор команды не удваивает stats, последующее уклонение не перезаписывает исходную базовую команду.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0006TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0006TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0006TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US6-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 14 named / 487 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0006-tk0002-results.json).

D11 выполнен на настоящем Engine player/NPC path и общей piecewise Motion-модели. End=t_star+.5s по observed trace/relative velocity, без foreign endpoint/deadline. Полный ход/perpendicular, возврат к исходному курсу/paused orbit, повтор сохраняет base и считает один episode, rejected engine input atomic, accepted input берёт управление. Engine damage прерывает автовозврат; NPC schedule не публикуется. Закрыты воспроизведённые double-orbit-control и exact-turn-boundary defects. [Motion](../../evidence/us0006-tk0002-motion-results.json):3 named/240 full; [Contracts](../../evidence/us0006-tk0002-contracts-results.json):2 named/256 full; все PASS. Общая solution build0 warnings/errors. Client non-budget initial2122/1 timeout, isolated5/5, repeat2123/2123 PASS без ослабления assertions; причина initial timeout не установлена, оба run сохранены. ForecastBudget2 прежних FAIL остаются OPEN, как и full Engine gate. Save18 persistence/encounter stats/UI bindings — отдельные последующие тикеты.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.
