---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0009-scenarios-and-saves"
ticket: "EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3"
title: "scenario-combat-mukha-v3"
stage: "implemented"
layer: "content-data"
depends_on: ["EP-0012-US-0019-TK-0005-maneuver-command-catalog"]
files_touched: 5
serves: ["US9-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3

## Why

[Сценарии и сохранения](../EP-0012-US-0009-scenarios-and-saves.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: scenario-combat-mukha-v3; доказать US9-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/Scenarios/CombatMukha/scenario.json](../../../../src/DeepSpaceSaga.Client/Scenarios/CombatMukha/scenario.json) | Существует: src/DeepSpaceSaga.Client/Scenarios/CombatMukha/scenario.json:1; { | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs` | Existing actual source scenario assertions describe EP9 | Migrate only Mukha expectations to exact US9-AC1 geometry/modules/AI; preserve discovery, crew, armor and Shmel assertions until its own removal ticket. |

| `tests/DeepSpaceSaga.Client.Tests/CombatForecastCacheTests.cs` | Reproduced4 consumer failures: new scenario cannot use old catalog without Tetrarch | Use actual default v3 catalog; migrate live cache launch/defense observation to explicit point commands, preserve readiness and report/input invariants. Synthetic legacy cases stay for US19/US18 migration. |

| `tests/DeepSpaceSaga.Client.Tests/CombatReportsAcceptanceFixtures.cs` | Reproduced4 consumer failures: new scenario cannot use old catalog without Tetrarch | Use actual default v3 catalog; migrate live cache launch/defense observation to explicit point commands, preserve readiness and report/input invariants. Synthetic legacy cases stay for US19/US18 migration. |

## Public API after the change

No public API change. Canonical CombatMukha now uses exact US9-AC1 v3 data; actual report/cache consumers use the default point catalog.

## Pursuit acceptance dependency

US7-TK1 original scenario probe: 300 km starting distance cannot satisfy the <=60 physical second launch-range requirement against a directly receding max-speed player (lower bound 76.67 s). During v3 scenario migration choose a physically feasible combat-entry layout consistent with A01, then validate US7-AC1 with the actual deployed scenarios. A 100 km controller fixture is only component evidence.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Set exact US9-AC1 positions in world units: player(0,0),heading90,speed3000; NPC(-400,750),(0,850),(400,750),heading270,speed6000. Use Tetrarch engine/light Mukha torpedo launchers; AI25km/band8/flank35 for each NPC, exactly as US7-AC1. Keep nine player modules, independent catalog masses and light armor; operators20. Remove obsolete automatic-PD flags from this scenario. Validate source/deployed discovery, all numbers and actual maximum-player-speed pursuit from every NPC start.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: content-data; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Mukha_v3_loads_and_is_discoverable**: позиции в км переведены в world units×10; crew skills20,9 модулей и три enemy; deployed output scenario найден.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0009TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0009TK0001Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US9-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 14 named / 2358 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0009-tk0001-results.json).

Exact approved world geometry, masses,9 modules/light armor, skills20,Tetrarch engine,light Mukha launchers and AI25/8/35. Twelve actual-spawn maximum-speed pursuit cases PASS; no registry capability overrides. Actual source/deployed launch and57 consumer regressions PASS.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 execution dependency correction: runtime precedes production catalog activation; see [DAG](../../Tickets.md#execution-correction-2026-10-20). Scope/AC unchanged.

Before implementation: old actual-content tests must migrate atomically with the scenario, not be redirected to a historical fixture. All three NPCs use flankOffsetDeg35 exactly as the explicit US7-AC1 requirement. US7 original300km probe remains historical FAIL; acceptance now uses exact approved US9 geometry, not adjusted balance coordinates.

Before consumer changes: focused real scenario14/14 PASS including twelve maximum-speed directions/spawns. Consumer reproduction4failed/53passed: old historical catalogs lack module.engine.tetrarch. Fix only these two actual-scenario composition roots to real v3; no new historical scenario copy and no assertion removal.
