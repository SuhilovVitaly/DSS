---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0010-tactical-map"
ticket: "EP-0012-US-0010-TK-0002-projectiles-and-blasts-render"
title: "projectiles-and-blasts-render"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0010-TK-0001-combat-visuals-v3"]
files_touched: 12
serves: ["US10-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0010-TK-0002-projectiles-and-blasts-render

## Why

[Тактическая карта](../EP-0012-US-0010-tactical-map.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: projectiles-and-blasts-render; доказать US10-AC1 в границах этой карточки.

## Decisions / Assumptions

A-POINT-RENDER: expand bounded scope to actual render integration: point partial, forecast geometry handoff, marker policy, effects, and external text-size style with its coverage test. No fake render DTO path: GameSessionScreen consumes admitted public own/observed motion and actual journal facts. Physical endpoint circle radius is R*10*ppu, core uses captured CoreFraction; minimum6px is a separate marker, never radius inflation. Foreign ray/profile has no endpoint circle or fuse. Real effect durations come from styles; reset/seen-event baseline prevents replay. Legacy branch retained only until planned cleanup. Headless Skia pixel and actual screen checks do not close native gate.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (12 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs:6; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs:8; internal sealed class CombatEffectStore | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.PointProjectiles.cs` | Required point renderer integration / external text size | Bounded changes for US10-AC1; preserve old contracts until cleanup. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatForecast.cs` | Required point renderer integration / external text size | Bounded changes for US10-AC1; preserve old contracts until cleanup. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Required point renderer integration / external text size | Bounded changes for US10-AC1; preserve old contracts until cleanup. |
| `src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs` | Required point renderer integration / external text size | Bounded changes for US10-AC1; preserve old contracts until cleanup. |
| `src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json` | Required point renderer integration / external text size | Bounded changes for US10-AC1; preserve old contracts until cleanup. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0001Tests.cs` | Required point renderer integration / external text size | Bounded changes for US10-AC1; preserve old contracts until cleanup. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailStore.cs` | Actual point screen crashes on bootstrap negative time | Exclude point flights from generic reconstructed history; own trail comes from captured launch and foreign past stays unguessed. |

A-TRAIL-FIX: actual GameSessionScreen reproduced5/6 failures including real Engine payload: generic trail bootstrap predicts -10000ms into point CurrentManeuverForecast. Fix owning client store rather than permitting fabricated pre-launch history in Motion. Preserve initial failed run as evidence.

| `tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs` | Repeated15s AdvanceTo timeout in nonbudget suite; isolated5/5 passes | Remove20x transport/watchdog clock mismatch in fixture; keep assertions and timeout intact. |

A-UI-FIXTURE-CLOCK: initial2157/1 nonbudget run repeated earlier AdvanceTo timeout, isolated5/5 passes. Source fixture accelerates SnapshotBuffer timestamp20x while engine updates every100 real ms; client watchdog may call1000ms stale after50 real ms. Use actual Stopwatch for the buffer. This repairs fixture clock consistency, not product thresholds or assertions. Record both initial failure and later evidence; do not claim isolated pass proves exact timeout cause.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `GameSessionScreen.Combat.cs:91-233` (старые трассы, точки, взрывы) заменяются.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Projectile_visuals_use_own_or_observed_schema**: круги только для own endpoint, observed line без endpoint/deadline.
- **Blast_geometry_scales_correctly_and_marks_minimum_size**: physical radius error<=1px; при R<6px отдельный marker6px.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0010TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0010TK0002Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US10-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 11 named / 2159 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0010-tk0002-results.json).

Actual GameSessionScreen renders own captured path/blast core/real countdown, observed ray/profile without endpoint/fuse, counter radius and public detonation/shotdown effects. Pixel outer-radius error<=1px at1/.1ppu including1.5DPI; separate6px marker at.001ppu, pause freezes physical countdown, effect duration is real. Actual Engine launch/manual detonation reaches screen; history reset suppresses replay. Initial real screen crash5/6 caused generic negative-time trail bootstrap; corrected owning trail store without inventing foreign past. Existing Basic UI fixture20x transport-clock mismatch removed after initial2157/1timeout, negative-control proof and isolated5/5; no expectations/timeouts relaxed. Corrected nonbudget2159/2159. Text size added to external style group25fields. Self-review headless preview attached; native and production activation remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
[Headless Skia preview](../../evidence/us0010-tk0002-headless.png) — public Engine point-catalog fixture; not native acceptance.
