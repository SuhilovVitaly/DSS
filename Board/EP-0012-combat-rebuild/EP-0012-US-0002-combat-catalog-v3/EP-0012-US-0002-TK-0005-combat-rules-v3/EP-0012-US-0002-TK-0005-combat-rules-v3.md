---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0002-combat-catalog-v3"
ticket: "EP-0012-US-0002-TK-0005-combat-rules-v3"
title: "combat-rules-v3"
stage: "draft"
layer: "content-data"
depends_on: ["EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands"]
files_touched: 4
serves: ["US1-AC4", "US2-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0002-TK-0005-combat-rules-v3

## Why

[Каталог v3](../EP-0012-US-0002-combat-catalog-v3.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-rules-v3; доказать US1-AC4, US2-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Combat/combat-rules.json` | Existing or bounded new check | Atomic point rules / Settings / obsolete modes activation. |
| `src/DeepSpaceSaga.Client/Settings.json` | Existing or bounded new check | Atomic point rules / Settings / obsolete modes activation. |
| `src/DeepSpaceSaga.Client/Data/Combat/fire-modes.json` | Existing or bounded new check | Atomic point rules / Settings / obsolete modes activation. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0005Tests.cs` | Existing or bounded new check | Atomic point rules / Settings / obsolete modes activation. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Записать blast coreFraction=0.3; hitDisableMs=8000; scatter pivot15/slope0.0333/min0.3; reaction1400/30/200; combatStartRangeKm=100. Сохранить броню и правила гибели.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: content-data; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Atomic activation check

**Point_rules_activate_without_legacy_mode_path**: source and deployed Settings contain no fireModes entry, obsolete mode file is absent, strict point rules load and real Engine/Client point launch paths work.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Production_rules_match_decisions**: все значения сравниваются с независимыми константами, не с копией loader.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0002TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0002TK0005Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0005Tests.cs
git diff --check
```

## Definition of Done

- Served AC US1-AC4, US2-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.

Atomic activation prerequisite: replace combat rules, remove typeData.fireModes and delete its obsolete JSON in the same commit. Source EngineContentLoader.cs:216-221 rejects point rules with a fireModes path and legacy rules without it. Validate source and deployed registry plus actual launch/Client integration after this switch.
