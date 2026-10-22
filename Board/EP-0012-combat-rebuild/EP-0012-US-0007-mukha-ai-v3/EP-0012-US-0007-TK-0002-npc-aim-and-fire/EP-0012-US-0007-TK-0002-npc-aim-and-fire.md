---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0007-mukha-ai-v3"
ticket: "EP-0012-US-0007-TK-0002-npc-aim-and-fire"
title: "npc-aim-and-fire"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0007-TK-0001-orbit-lead-fix"]
files_touched: 5
serves: ["US7-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0007-TK-0002-npc-aim-and-fire

## Why

[ИИ Мухи v3](../EP-0012-US-0007-mukha-ai-v3.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: npc-aim-and-fire; доказать US7-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.HostileFire.cs` | Existing or new bounded partial/test | Point AI profile selection, shared launch path, scenario threshold/dodge validation only. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointAim.cs` | Existing or new bounded partial/test | Point AI profile selection, shared launch path, scenario threshold/dodge validation only. |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | Existing or new bounded partial/test | Point AI profile selection, shared launch path, scenario threshold/dodge validation only. |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Existing or new bounded partial/test | Point AI profile selection, shared launch path, scenario threshold/dodge validation only. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0002Tests.cs` | Existing or new bounded partial/test | Point AI profile selection, shared launch path, scenario threshold/dodge validation only. |

A-AIM-01: scenario ai.fireThreshold defaults to 0.3, ai.assumedTargetDodge defaults to 0.25; values finite in [0,1]. Expected miss = assumedTargetDodge * 0.5 * available target engine acceleration * max(0,tau-1)^2 + sigma. Coverage is D03 falloff at expected miss. Friendly future positions use their current velocity; include the shooter and other ships on the same side. Point AI requires the scenario ai block; it uses no CombatReactions draw. Original production scenario 60-second gate remains OPEN as recorded in US7-TK1 and US9.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Заменить `SimulationEngine.HostileFire.cs:16-80`. Параметры `fireThreshold` и `assumedTargetDodge` берутся из `ai` сценария.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Npc_selects_max_coverage_profile_and_obeys_threshold**: coverage .3 принимается, <.3 нет; tie по ordinal profile ID.
- **Npc_rejects_friendly_blast_and_intercepts_fast_player**: свой ближе R+1км запрещает пуск; на production scenario max-speed игрока дистанция пуска достигается<=60с физики.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0007TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0007TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.HostileFire.cs src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US7-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 16 named / 523 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0007-tk0002-results.json).

Point NPC AI uses finite linear intercept, per-profile expected miss and D03 coverage, AccuracySkill scatter, ordinal ties, scenario threshold/dodge parameters and future friendly/self exclusion. Shared point launch executor; no player receipt or legacy CombatReactions delay. Repeated evaluation consumes no launch RNG; scheduler partition and future friendly-position tests PASS. Exact mathematical threshold accepts one floating-point representable step. Original production scenario <=60s gate remains OPEN for US9 as recorded in preceding ticket. Solution build 0 warnings/errors.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
