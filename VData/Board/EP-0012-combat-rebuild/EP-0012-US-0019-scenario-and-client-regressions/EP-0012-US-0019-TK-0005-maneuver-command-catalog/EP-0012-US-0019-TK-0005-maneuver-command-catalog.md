---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0019-scenario-and-client-regressions"
ticket: "EP-0012-US-0019-TK-0005-maneuver-command-catalog"
title: "maneuver-command-catalog"
stage: "implemented"
layer: "content-data"
depends_on: ["EP-0012-US-0002-TK-0005-combat-rules-v3"]
files_touched: 4
serves: ["US19-AC1", "US6-AC2", "US6-AC3", "US8-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0019-TK-0005-maneuver-command-catalog

## Why

[Совместимость сценариев и старых клиентских поверхностей](../EP-0012-US-0019-scenario-and-client-regressions.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: maneuver-command-catalog; доказать US19-AC1, US6-AC2, US6-AC3, US8-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/Data/Commands/Engine/commands.json](../../../../../src/DeepSpaceSaga.Client/Data/Commands/Engine/commands.json) | Существует: src/DeepSpaceSaga.Client/Data/Commands/Engine/commands.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Data/module-types.json](../../../../../src/DeepSpaceSaga.Client/Data/module-types.json) | Существует: src/DeepSpaceSaga.Client/Data/module-types.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json](../../../../../src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json) | Существует: src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json:1; { | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change. Register engine.turnHold, maneuver.evade and combat.acknowledgeResults as category-inherited Engine metadata. Typed command payloads retain authoritative dispatch; numeric engine definitions and all thirteen original commands unchanged.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Зарегистрировать engine.turnHold, maneuver.evade и combat.acknowledgeResults в command catalog и разрешённых командах категории/установленного двигателя. Сохранить прежние внебоевые команды; data ownership остаётся Client/Data, validation — существующий Engine loader.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: content-data; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Maneuver_command_catalog_resolves_new_contract_ids**: production loader находит новые команды; неизменённые внебоевые turnLeftStep/turnRightStep работают.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0019TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0019TK0005Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs
git diff --check
```

## Definition of Done

- Served AC US19-AC1, US6-AC2, US6-AC3, US8-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 2 named / 2344 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0019-tk0005-results.json).

Actual canonical source/deployed catalog resolves typed hold/evade/ack controls. Real Engine executes both step directions, inertia rejection, hold/release, foreign-trace evasion and active encounter acknowledgment; no module capability changes.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
