---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0004-point-aim-countermissiles"
ticket: "EP-0012-US-0004-TK-0002-countermissile-detonation"
title: "countermissile-detonation"
stage: "implemented"
layer: "engine-and-contracts"
depends_on: ["EP-0012-US-0004-TK-0001-countermissile-point-launch"]
files_touched: 5
serves: ["US4-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0004-TK-0002-countermissile-detonation

## Why

[Противоракеты](../EP-0012-US-0004-point-aim-countermissiles.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: countermissile-detonation; доказать US4-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist 5 файлов, с Engine/Contracts и matching tests. Scheduler уже использует NextPointDetonationTime из point partial, поэтому расширение kinds/batch делается там. В исходном event DTO отсутствовал обязательный destroyed[] из US4-AC2: добавляется nullable список ID фактически сбитых снарядов без private данных, с отдельным transport test. Point factory получает независимый torpedoScatter argument для точных аналитических same-time fixtures на настоящем Engine, без ручной вставки projectile state.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs) | Shared point scheduler/resolver | Counter-before-torpedo batch, radius destruction, state cleanup. |
| [src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs](../../../../src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs) | Event facts | Append optional DestroyedProjectileIds. |
| `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0004TK0002Tests.cs` | NEW | Empty/nonempty/absent destroyed IDs roundtrip without private data. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| [tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0001Tests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0001Tests.cs) | Point registry fixture | Explicit independent torpedo scatter parameter for exact-deadline scenarios. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

Execution API refinement: CombatJournalEntry appends ImmutableArray<string>? DestroyedProjectileIds=null, omitted when null, emitted as [] for an observed empty counter blast. Engine emits ordinal victim IDs, excluding the detonator itself, plus one ShotDown fact per victim; no child blast or interception RNG. All sides/kinds are eligible at <=R. Same-time counter ordinal order precedes remaining torpedoes; destroyed due counter/torpedo cannot detonate. Removing flights updates launcher/defense active IDs without resetting captured reload. Internal NPC shared launcher accepts explicit ProjectileKind for production-path fixtures/consumers.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Подрыв по §3.4.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Countermissile_destroys_all_projectile_kinds_and_sides**: свой/чужой torpedo и countermissile в R удалены без цепного взрыва; ship цел.
- **Same_time_countermissile_prevents_torpedo_blast**: при одинаковом deadline сначала ПРО, затем оставшиеся torpedoes по ordinal ID.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0004TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0004TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US4-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 5 named / 346 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0004-tk0002-results.json).

Counter-first ordinal batch удаляет все kinds/sides при <=R без chain blast, ship damage или interception/hit-location RNG. DestroyedProjectileIds содержит ordinal victim IDs без самого детонатора, ShotDown сохраняет victim identity и source counter correlation. Проверены5 реальных сценариев: all kinds/sides, radius boundary/outside, same-time1000ms, counter-vs-counter, partition equality. Launch reload и active IDs после удаления согласованы. Дополнительно [Contracts evidence](../../evidence/us0004-tk0002-contracts-results.json):3 named /254 full PASS, nullable absent/empty/nonempty ID lists roundtrip и privacy. Общая solution build PASS0 warnings/errors3.91s. Legacy auto-defense cleanup остаётся следующим тикетом, full Engine/Client gates OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
