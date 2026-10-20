---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0014-balance-and-acceptance"
ticket: "EP-0012-US-0014-TK-0002-tools-analyzer"
title: "tools-analyzer"
stage: "draft"
layer: "tooling"
depends_on: ["EP-0012-US-0014-TK-0001-production-balance-harness"]
files_touched: 4
serves: ["US14-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0014-TK-0002-tools-analyzer

## Why

[Приёмка и баланс](../EP-0012-US-0014-balance-and-acceptance.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: tools-analyzer; доказать US14-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.CombatAnalyzer/DeepSpaceSaga.CombatAnalyzer.csproj` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tools/DeepSpaceSaga.CombatAnalyzer/Program.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tools/DeepSpaceSaga.CombatAnalyzer/README.md` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `global.json` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |

## Public API after the change

Proposed CLI: dotnet run --project tools/DeepSpaceSaga.CombatAnalyzer -- --seeds 3001:4000 --policy dodge|static --output <path>; --self-test. SDK в global.json выбирается из реально установленного dotnet --list-sdks на момент исполнения, не из вымышленной версии.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Анализатор и `global.json`. Эталонный симулятор и `src/DeepSpaceSaga.CombatSim` не трогаются (O11).
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: tooling; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Analyzer_self_test_checks_seed_count_columns_and_exit_status**: built-in --self-test; отчёт не меняет параметры баланса.

```powershell
dotnet build tools/DeepSpaceSaga.CombatAnalyzer/DeepSpaceSaga.CombatAnalyzer.csproj
dotnet run --project tools/DeepSpaceSaga.CombatAnalyzer -- --self-test
git diff --check
```

## Definition of Done

- Served AC US14-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
