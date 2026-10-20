---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0002-combat-catalog-v3"
ticket: "EP-0012-US-0002-TK-0002-countermissile-launcher-v3"
title: "countermissile-launcher-v3"
stage: "draft"
layer: "content-data"
depends_on: ["EP-0012-US-0002-TK-0001-torpedo-launchers-v3"]
files_touched: 2
serves: ["US2-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0002-TK-0002-countermissile-launcher-v3

## Why

[Каталог v3](../EP-0012-US-0002-combat-catalog-v3.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: countermissile-launcher-v3; доказать US2-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (2 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/CombatV3/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json` | Bounded staging/activation file or matching test | Prepare and validate actual v3 component with real loader; default activation is TK5. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0002Tests.cs` | Bounded staging/activation file or matching test | Prepare and validate actual v3 component with real loader; default activation is TK5. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `Data/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json`. Одноствольная версия (`single`) использовалась Шмелем. Здесь её не трогают: она удаляется в US9-TK2 вместе со сценарием.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: content-data; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Countermissile_catalog_matches_v3**: range30, reload12500, scatter0.08, wide15/1 и fast25/0.5.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0002TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0002TK0002Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US2-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.

## Atomic catalog staging decision

Before implementation: TK1–4 author real production JSON components under `Data/CombatV3`, outside the recursively loaded active Modules/Commands folders. Each component is validated through the real loader in source and deployed output, with independent expected constants. TK5 moves those exact committed components to canonical paths in one atomic switch with rules, module categories, Settings and mode removal. Before that switch, active default catalog remains coherent; component implementation does not close default-game integration/US2 acceptance. Tests select staged component while it exists in the source tree, and canonical component after activation. No numeric or product requirement is changed.
