---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0008-encounter-and-time"
ticket: "EP-0012-US-0008-TK-0003-encounter-stats"
title: "encounter-stats"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0008-TK-0002-combat-speed-lock"]
files_touched: 4
serves: ["US8-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 4
---

# EP-0012-US-0008-TK-0003-encounter-stats

## Why

[Боевое столкновение и время](../EP-0012-US-0008-encounter-and-time.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: encounter-stats; доказать US8-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs` | Existing or bounded new tests | Actual encounter counters, death/evasion hooks and tests only. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Existing or bounded new tests | Actual encounter counters, death/evasion hooks and tests only. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs` | Existing or bounded new tests | Actual encounter counters, death/evasion hooks and tests only. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0003Tests.cs` | Existing or bounded new tests | Actual encounter counters, death/evasion hooks and tests only. |

A-STATS-01: count player launches, player counter interceptions of hostile torpedoes, positive-damage blast/ship hits, new player evade episodes and all enemy ship deaths. Damage totals are actual own armor/structure loss. Completed encounter flights continue to update their result after Won/ack; a new encounter resets counters and captures all live flights. Additional launches after Won belong to the prior encounter only while its flights are still live. Rejected/replayed commands and repeated frames do not emit new facts. Duration is physical time, capped at completion; idle before any encounter has zero duration. Save18 persists counters/live flight membership later.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Update counters from committed launch/hit/shot-down facts, own module losses, actual enemy deaths and first player evade episode; retain result while captured flights resolve.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Encounter_stats_count_authoritative_events_once**: все10 счётчиков, повтор receipt/frame не увеличивает.
- **Duration_uses_motion_time_and_excludes_pause**: физическая длительность сохраняется, nominal x1 реальная=physical×4.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0008TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0008TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US8-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 8 named / 696 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0008-tk0003-results.json).

All10 counters derive from actual committed facts and first player-evasion episodes. Damage is own actual loss; enemy numeric health is not disclosed. Rejected/replayed commands and repeated frames do not count again. Captured live flights and defensive actions can update a completed result after Won/ack; new encounter resets all counters. Duration uses real injected clock, excludes a long pause and is capped at completion. Initial regression695/1 orbital pose mismatch retained; unchanged isolated1/1 and one full repeat696/696 PASS, trigger unresolved, no baseline-fix claim. Solution build0 warnings/errors. Save18 persistence and integrated production/native acceptance remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

## Repair scope: captured hostile side after previous-owner death

2026-10-20: the US17-TK2 real report scenario revealed that the stats counter still looks up the owner in the current encounter. A hostile torpedo can outlive its owner and continue into a new encounter. Reproduce using the actual published scenario fixture, then update only SimulationEngine.Encounter.cs and PointCombatUS0008TK0003Tests.cs (both already allowed) to use the captured launch-side metadata published by US17-TK2. Preserve actual failed evidence and publish a separate fix. No lifetime/physics/RNG change.

Repair result: reproduced1 failed (E05 confirms intercepted hostile torpedo but TorpedoesIntercepted=0 after owner death/new encounter). Captured launch-side metadata now determines hostility; current-owner lookup is fallback only for older payloads without it. PASS21 relevant stats/report cases,776 bounded Engine regression, solution build0 warnings/errors, scoped format/diff PASS. [Reproduction and checks](../../evidence/intercept-stats-fix-results.json). Separate fixing commit/push follows; no change to physics or RNG.
