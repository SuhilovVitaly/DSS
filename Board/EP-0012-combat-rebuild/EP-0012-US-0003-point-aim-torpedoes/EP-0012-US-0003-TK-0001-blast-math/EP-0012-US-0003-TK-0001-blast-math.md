---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0003-point-aim-torpedoes"
ticket: "EP-0012-US-0003-TK-0001-blast-math"
title: "blast-math"
stage: "draft"
layer: "motion"
depends_on: ["EP-0012-US-0019-TK-0005-maneuver-command-catalog"]
files_touched: 2
serves: ["US3-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0003-TK-0001-blast-math

## Why

[Торпеда в точку](../EP-0012-US-0003-point-aim-torpedoes.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: blast-math; доказать US3-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (2 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/BlastMath.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0003TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

Proposed public static BlastMath: double Falloff(double distanceKm,double radiusKm,double coreFraction); double ScatterSigma(double rangeKm,double scatterFraction,int accuracySkill); (double X,double Y) PositionAt(double startX,double startY,double endX,double endY,double progress); double DetonationTimeMs(double launchedAtMs,double distanceKm,double speedKmS). RNG не вызывается в Motion.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Создать чистые falloff, sigma, линейную позицию и время подрыва по D01–D05. Вместо внешнего симулятора — независимые аналитические fixtures из Tests; 20 граничных/промежуточных точек с явными expected значениями.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: motion; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Falloff_has_core_linear_edge_and_zero_outside**: R=10; d=0/3/6.5/10/11 => k=1/1/0.5/0/0.
- **Scatter_sigma_uses_fraction_and_skill**: d=50, p=.02; skill0/15/30 => sigma1.4995/1/.5005км.
- **Position_and_deadline_are_linear**: d=60км,v=6км/с => 10000мс; середина в5000мс.

Matching project: `tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj`; proposed test class `PointCombatUS0003TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0003TK0001Tests"
dotnet build tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Motion/BlastMath.cs tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0003TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US3-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
