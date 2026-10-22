---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
ticket: "EP-0012-US-0018-TK-0005-module-combat-regressions"
title: "module-combat-regressions"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0003-TK-0005-remove-homing-roll-modes"]
files_touched: 5
serves: ["US18-AC3", "US3-AC5", "US4-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0018-TK-0005-module-combat-regressions

## Why

[Удаление старой боевой модели и регрессии](../EP-0012-US-0018-legacy-combat-cleanup.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: module-combat-regressions; доказать US18-AC3, US3-AC5, US4-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests). Baseline fc72732e5fa357eb127d0e29a2a2d13268b7bf23. Сначала воспроизвести порядок §3.4 на настоящем Engine; менять только необходимую границу scheduler, не вводить новую физическую сетку. Полный Engine corpus выполняется bounded partitions с проверкой полного списка cases.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Current point implementation | Preserve current damage/defeat/kinematics and exact same-time order; Fix reproduced endpoint order; retained invariants remain unchanged. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Current point implementation | Preserve current damage/defeat/kinematics and exact same-time order; Fix reproduced endpoint order; retained invariants remain unchanged. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs` | Current point implementation | Preserve current damage/defeat/kinematics and exact same-time order; Fix reproduced endpoint order; retained invariants remain unchanged. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` | Current point implementation | Preserve current damage/defeat/kinematics and exact same-time order; Fix reproduced endpoint order; retained invariants remain unchanged. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0005Tests.cs` | NEW | Preserve current damage/defeat/kinematics and exact same-time order; Fix reproduced endpoint order; retained invariants remain unchanged. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Перевести старые торпедные end-to-end tests на геометрическую детонацию и production catalogs; сохранить независимые проверки брони, critical modules, defeat и partition invariance.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Ниже — named regression coverage; фактические команды и результаты приведены в Status / Evidence.

- **New_combat_preserves_module_damage_and_defeat**: seeds1/42/ulong.MaxValue, независимый deadline969.75ms, module hit/defeat/frozen terminal state без chance assertions.
- **Same_timestamp_player_launch_precedes_npc_launch_and_scatter_consumption**: порядок player/NPC и точное потребление launch_scatter.
- **Player_manual_detonation_can_defeat_the_player_before_same_timestamp_npc_launch**: manual blast подавляет NPC launch после defeat.
- **Scheduled_blast_death_precedes_the_queued_player_launch_at_the_same_timestamp**: scheduled detonation/death остаются раньше player command.
- **Real_clock_snapshot_and_explicit_save_preserve_player_before_npc_boundary_order**: real clock и explicit save seam (CaptureSaveState сам real clock не продвигает).

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0018TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0018TK0005Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0005Tests.cs
git diff --check
```

## Definition of Done

- Served AC US18-AC3, US3-AC5, US4-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: implementation и self-review выполнены. PASS 8 named / 2331 full owning tests (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0018-tk0005-results.json).

Full Engine gate PASS: exact discovery multiset2331 cases /2331 distinct TestIds; 2192 broad +139 corpus cases in25 bounded processes, no failures/skips. Full Client2221 PASS; whole solution0 warnings/errors. Reproduced and corrected equal-time player/NPC ordering without changing physics mesh or persisted state. Initial reproduction2 failed/3 passed, final8 passed. Balance1000 seeds and native acceptance remain US14.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

Execution scope2026-10-21: all legacy consumers were migrated and published in prerequisite US3-TK5. This final integration ticket checks retained invariants and all2331 discovered Engine cases (2323 prerequisite +8 new scheduler/damage cases), including138 long corpus cases and the100-seed Category=Balance smoke; the1000-seed acceptance remains US14. Scheduler endpoint ordering is audited against Documentation§3.4: detonation/death, player commands, NPC dodge/launch, movement. Source changes require a failing runtime reproduction first. Native/product gates remain separate.
