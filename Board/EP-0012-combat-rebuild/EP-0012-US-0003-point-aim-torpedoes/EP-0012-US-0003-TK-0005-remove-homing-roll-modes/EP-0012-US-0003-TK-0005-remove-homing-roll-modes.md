---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0003-point-aim-torpedoes"
ticket: "EP-0012-US-0003-TK-0005-remove-homing-roll-modes"
title: "remove-homing-roll-modes"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests"]
files_touched: 4
serves: ["US3-AC5"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0003-TK-0005-remove-homing-roll-modes

## Why

[Торпеда в точку](../EP-0012-US-0003-point-aim-torpedoes.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: remove-homing-roll-modes; доказать US3-AC5 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Combat/CombatChanceMath.cs](../../../../src/DeepSpaceSaga.Engine/Combat/CombatChanceMath.cs) | Существует: src/DeepSpaceSaga.Engine/Combat/CombatChanceMath.cs:5; internal static class CombatChanceMath | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Combat/FireModeMath.cs](../../../../src/DeepSpaceSaga.Engine/Combat/FireModeMath.cs) | Существует: src/DeepSpaceSaga.Engine/Combat/FireModeMath.cs:5; internal static class FireModeMath | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0005Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Удалить использование self-homing, replan, hit roll и modes из торпедного runtime; удалить CombatChanceMath/FireModeMath после перевода всех Engine-потребителей. Перенос ссылок и старых тестов закрывает US-0018; при обнаружении оставшейся ссылки тикет не закрывать и не ослаблять тесты.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Torpedo_resolution_has_no_hit_roll_or_mode_rng**: исход зависит от геометрии; RNG location/scatter допустимы, torpedo-hit/breakdown нет.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0003TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0003TK0005Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/Combat/CombatChanceMath.cs src/DeepSpaceSaga.Engine/Combat/FireModeMath.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0005Tests.cs
git diff --check
```

## Definition of Done

- Served AC US3-AC5 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.
