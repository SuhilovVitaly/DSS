---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0002-combat-catalog-v3"
ticket: "EP-0012-US-0002-TK-0003-engines-v3"
title: "engines-v3"
stage: "implemented"
layer: "content-data"
depends_on: ["EP-0012-US-0002-TK-0002-countermissile-launcher-v3"]
files_touched: 2
serves: ["US2-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0002-TK-0003-engines-v3

## Why

[Каталог v3](../EP-0012-US-0002-combat-catalog-v3.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: engines-v3; доказать US2-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (2 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/CombatV3/Modules/Engine/modules-engine.json` | Bounded staging/activation file or matching test | Prepare and validate actual v3 component with real loader; default activation is TK5. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0003Tests.cs` | Bounded staging/activation file or matching test | Prepare and validate actual v3 component with real loader; default activation is TK5. |

## Public API after the change

No public API change. Dedicated Tetrarch and updated Strizh definitions are staged under Data/CombatV3; canonical activation belongs to US2-TK5. Basic is semantically identical to its independent original JSON definition.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `Data/CombatV3/Modules/Engine/modules-engine.json` (TK5 переносит committed bytes в canonical path): новый `module.engine.tetrarch` с `combatRole: engine` и значения Стрижа по D12 (O9). Тест, что basic не изменился.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: content-data; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Tetrarch_has_dedicated_engine_and_basic_is_unchanged**: 5000м/с,600м/с²,20°/с; strizh8000/1000/24; basic byte/semantic-equivalent.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0002TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0002TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US2-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 2 named / 2336 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0002-tk0003-results.json).

Staged source/deployed Tetrarch5000/600/20 and Strizh8000/1000/24 real-loader validation PASS. Complete basic JSON independently frozen and unchanged; default Engine coherent before activation. Cruise3/6km/s is scenario US9 scope.2336 non-budget Client PASS; atomic default v3 switch US2-TK5 and native/strict budget gates OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

## Atomic catalog staging decision

Before implementation: TK1–4 author real production JSON components under `Data/CombatV3`, outside the recursively loaded active Modules/Commands folders. Each component is validated through the real loader in source and deployed output, with independent expected constants. TK5 moves those exact committed components to canonical paths in one atomic switch with rules, module categories, Settings and mode removal. Before that switch, active default catalog remains coherent; component implementation does not close default-game integration/US2 acceptance. Tests select staged component while it exists in the source tree, and canonical component after activation. No numeric or product requirement is changed.

Cruise3/6km/s belongs to scenario initial velocities (US9-AC1), not module capability schema; no invented cruise field is added.
