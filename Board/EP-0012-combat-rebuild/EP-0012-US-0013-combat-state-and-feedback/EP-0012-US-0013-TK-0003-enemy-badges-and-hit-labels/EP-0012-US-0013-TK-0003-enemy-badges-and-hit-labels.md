---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0013-combat-state-and-feedback"
ticket: "EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels"
title: "enemy-badges-and-hit-labels"
stage: "implemented"
layer: "engine-client"
depends_on: ["EP-0012-US-0013-TK-0002-weapon-panels-v3"]
files_touched: 8
serves: ["US13-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels

## Why

[Состояние, панели, обратная связь](../EP-0012-US-0013-combat-state-and-feedback.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: enemy-badges-and-hit-labels; доказать US13-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (8 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs:12; internal sealed class ObjectLabelRenderer | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs:8; internal sealed class CombatEffectStore | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs` | Existing | Emit existing public HitResult.Miss for completed point blasts with no actual ship hit/intercept; Client must not infer authoritative outcomes. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Existing | Preserve actual enemy relation on point-combat wreck; project public Destroyed badge on the surviving wreck, since defeated NPC is removed. Existing relation save field already persists this metadata. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Existing | GetRelationToPlayer currently discards wreck relations; honor captured point-wreck relation through the actual snapshot producer. |

| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Existing | Existing relationToPlayer validation accepts NPC only; admit captured Enemy/Friend/Neutral wreck relation so projection survives serialization. Do not admit self or unknown values. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

Scope correction before code: the actual point runtime currently emits Core/Edge/ShotDown but no Miss for an empty detonation. Add the existing Miss fact after authoritative blast resolution, without new RNG or changing collision/damage. Render only admitted journal results; enemy badges consume EnemyStatus only, never ModuleHealth. No numeric enemy damage label.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Production layers Engine -> Client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Enemy_badges_use_aggregate_status_and_no_damage_number**: damaged/immobilized/destroyed; ядро/край/мимо/сбита.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0013TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0013TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US13-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 13 named / 2286 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0013-tk0003-results.json).

Public enemy aggregate badges, actual Core/Edge/Miss/ShotDown labels without numeric enemy damage, baseline/no replay and 2s real expiry. Engine explicitly emits empty-blast Miss for torpedo/counter. Real destroyed NPC -> enemy wreck relation/status -> captured object JSON/ScenarioLoader -> new Engine badge PASS; self/invalid relation still rejected. Engine bounded regression 785/785. Full Save17 point roundtrip fails old defense parameters, retained as save17-gap; object-only persistence PASS is not full Save18 acceptance. Actual-screen 100/150 percent bounds and PNG inspected. Full Engine/native/product/strict forecast budget remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
