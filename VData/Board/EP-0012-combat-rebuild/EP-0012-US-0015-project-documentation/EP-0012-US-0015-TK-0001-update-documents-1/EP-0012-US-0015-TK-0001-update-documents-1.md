---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0015-project-documentation"
ticket: "EP-0012-US-0015-TK-0001-update-documents-1"
title: "update-documents-1"
stage: "implemented"
layer: "documentation"
depends_on: ["EP-0012-US-0014-TK-0003-native-acceptance"]
files_touched: 5
serves: ["US15-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0015-TK-0001-update-documents-1

## Why

[Документация (обязательная завершающая история)](../EP-0012-US-0015-project-documentation.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: update-documents-1; доказать US15-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Current implementation sources are linked; historical planning assumptions remain identified.

## Code context

Точный allowlist (5 файлов). Current baseline `cec1c54cdf21c2a8f5b70fb8d30e39413e985e6b`. Scope уточнён до редактирования по EpicExecutionPrompt§6: актуальные контракты, историческая точность и все связанные документы. Evidence/Board metadata являются обязательным результатом.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/04-Engineering/PointAimCombat.md` | NEW | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/04-Engineering/BasicTorpedoCombat.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/04-Engineering/CountermeasureCombat.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/04-Engineering/ModuleCombat.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/04-Engineering/ModuleCombatDocumentation.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. После реализации обновить только перечисленные документы: новый контракт/исторические superseded ссылки/фактические acceptance gates. Искать самонаведение, chance/roll/intercept/modes/auto/burst, старые команды и числа. Не объявлять draft EP12 уже реализованным.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: documentation; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Documentation checks executed; actual results below.

- **Documentation_links_and_numeric_contract_match_implementation**: каждый старый термин либо исторический с пометкой, либо исправлен; фактические NOT RUN сохранены.

Документарный/ручной gate: проверить перечисленные критерии и локальные ссылки; `git diff --check`. Product build не требуется для evidence-only изменений. Native выполняется только при отдельном разрешении на игру; иначе NOT RUN с причиной. Graphify output проверяется при фактическом rebuild.

## Definition of Done

- Served AC US15-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: documentation and self-review complete. PASS 51 relative path references across 5 scoped files, diff check. [Evidence](../../evidence/us0015-tk0001-results.json). New central contract covers public commands, numeric rules, event order, privacy, saves/scenarios and actual failed/open acceptance; earlier engineering records explicitly historical. Runtime tests not rerun for prose-only changes. Human/product/balance OPEN. Separate commit/push follows.
