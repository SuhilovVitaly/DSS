---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0006-threats-and-evasion"
ticket: "EP-0012-US-0006-TK-0001-blast-threat-math"
title: "blast-threat-math"
stage: "implemented"
layer: "motion"
depends_on: ["EP-0012-US-0003-TK-0001-blast-math"]
files_touched: 2
serves: ["US6-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0006-TK-0001-blast-threat-math

## Why

[Угрозы и уклонение](../EP-0012-US-0006-threats-and-evasion.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: blast-threat-math; доказать US6-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (2 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/BlastThreatMath.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0006TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

Proposed public BlastThreatMath.Evaluate(double shipX,double shipY,double shipVelocityXKmS,double shipVelocityYKmS,double projectileX,double projectileY,double directionX,double directionY,double projectileSpeedKmS,double blastRadiusKm) -> BlastThreatResult с IsThreat, EntryAfterSeconds?, ClosestAfterSeconds, ClosestOffsetXKm/YKm, опасным интервалом; входные X/Y world units, производные расстояния км. Скрытые endpoint/deadline не используются.

Execution API refinement: результат содержит также ExitAfterSeconds?, DangerStartX/Y?, DangerEndX/Y? (world units на наблюдаемой трассе). null exit/end обозначает неограниченный интервал при нулевой относительной скорости внутри зоны; это не Infinity. Порог A02 строгий: касание без проникновения не считается угрозой. Уже внутри зоны — entry=0, в том числе при удалении. Направление нормализуется; нулевое направление допустимо только при нулевой скорости снаряда. Числовые входы/производные должны быть конечны, speed/R неотрицательны; неверный вход отклоняется явно. Вектор closest направлен от корабля к снаряду. API не принимает foreign endpoint/deadline.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Чистая математика угрозы по наблюдаемой бесконечной трассе и относительной скорости, A02. Аналитические fixtures заменяют внешнюю ссылку на Battle.threat.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: motion; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Relative_motion_threat_has_entry_and_closest_approach**: относительная прямая с известным квадратным корнем даёт t_enter и t_star.
- **Parallel_stationary_and_diverging_motion_are_finite**: v_rel=0, удаление, касание R+.5 без NaN/Infinity.

Matching project: `tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj`; proposed test class `PointCombatUS0006TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0006TK0001Tests"
dotnet build tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Motion/BlastThreatMath.cs tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0006TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US6-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 8 named / 230 full owning tests (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0006-tk0001-results.json).

A02 проверен аналитической хордой: entry4.25s, closest5s, exit5.75s при относительной скорости2km/s и поперечном расстоянии2km. Проверены world/km units, вращение/смещение, normalized direction, касание, нахождение внутри при удалении, stationary/parallel с null exit и invalid inputs. API не принимает скрытые endpoint/deadline. Engine/UI consumers выполняются отдельно.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
