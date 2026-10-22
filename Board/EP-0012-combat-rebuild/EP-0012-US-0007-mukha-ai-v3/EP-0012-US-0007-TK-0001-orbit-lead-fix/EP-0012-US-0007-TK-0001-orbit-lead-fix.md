---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0007-mukha-ai-v3"
ticket: "EP-0012-US-0007-TK-0001-orbit-lead-fix"
title: "orbit-lead-fix"
stage: "implemented"
layer: "motion"
depends_on: ["EP-0012-US-0006-TK-0003-continuous-turn-in-combat"]
files_touched: 3
serves: ["US7-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0007-TK-0001-orbit-lead-fix

## Why

[ИИ Мухи v3](../EP-0012-US-0007-mukha-ai-v3.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: orbit-lead-fix; доказать US7-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (3 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Motion/HostileOrbitMath.cs](../../../../src/DeepSpaceSaga.Motion/HostileOrbitMath.cs) | Существует: src/DeepSpaceSaga.Motion/HostileOrbitMath.cs:5; public readonly record struct HostileMotionSample(double X, double Y, double SpeedKmS, double Direction); | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0007TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0001Tests.cs` | NEW scenario acceptance | Actual Engine pursuit within 60 physical seconds from A01 100 km combat admission, with specified v3 engine/AI parameters. |

A-LEAD-01: earliest interception of the orbit-radius circle at available maximum speed replaces relative-speed division; unreachable future intercept falls back to the current target position. The command/controller still respects actual acceleration and turn limits. Production engine/scenario data activates in its own tickets.

A-LEAD-02 / integration gate: unmodified CombatMukhaSingle starts at 300 km; original scenario probe 2/4 passes, 2/4 fails. A receding 5 km/s player and 8 km/s NPC require at least (300-70)/(8-5)=76.67 physical seconds even with instantaneous acceleration. The controller regression starts at combatStartRangeKm=100 (A01), explicitly not an original-production-scenario PASS. Full US7-AC1 remains OPEN until US9 scenario migration and product/balance validation; no change to the 60 second target.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `Motion/HostileOrbitMath.cs:75-142`: упреждение `leadSeconds = min(120, d / relSpeed)` при `relSpeed → 0` даёт точку на 120 с вперёд (`:110-116`). Ограничить упреждение по времени достижимости и учитывать радиус орбиты. Тест с быстрой целью.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Production layer: Motion; matching Motion tests plus a bounded Engine scenario acceptance check. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Fast_target_is_reachable_without_120_second_runaway**: достижимая точка с учётом orbit radius; relSpeed→0 конечен.
- **Orbit_fix_is_deterministic_and_keeps_flank_sign**: одинаковые входы дают один desired heading.

Matching project: `tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj`; proposed test class `PointCombatUS0007TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0007TK0001Tests"
dotnet build tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Motion/HostileOrbitMath.cs tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0007TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US7-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 6 named / 249 full owning tests (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0007-tk0001-results.json).

Reachable orbit-circle intercept replaces the 120-second relative-speed lead; equal/unreachable speeds remain finite and flank sign is retained. Engine regression 507/507; A01 combat admission scenario 4/4. ORIGINAL 300 km production scenario probe 2 pass / 2 fail, preserved in evidence. US7-AC1 production scenario acceptance remains OPEN for US9 feasible layout migration; the 100 km fixture is explicitly component proof. No 60-second production acceptance claim.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
