---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0015-project-documentation"
ticket: "EP-0012-US-0015-TK-0004-update-documents-4"
title: "update-documents-4"
stage: "implemented"
layer: "documentation"
depends_on: ["EP-0012-US-0015-TK-0003-update-documents-3"]
files_touched: 14
serves: ["US15-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0015-TK-0004-update-documents-4

## Why

[Документация (обязательная завершающая история)](../EP-0012-US-0015-project-documentation.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: update-documents-4; доказать US15-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Current implementation sources are linked; historical planning assumptions remain identified.

## Code context

Точный allowlist (14 файлов). Current baseline `294df70ce3445ca6bed2aaff956bc7f96da55f92`. Scope уточнён до редактирования по EpicExecutionPrompt§6: актуальные контракты, историческая точность и все связанные документы. Evidence/Board metadata являются обязательным результатом.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/01-Requirements/EngineRequirements.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/00-Process/CLAUDE.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Board/EP-0009-battle/ImplementationStatus.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/01-Requirements/FirstReleaseRequirements.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/README.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `tools/DeepSpaceSaga.Performance/README.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/06-Tooling/GameSessionScreenUI.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/DeepSpaceSaga.Contracts/PlayerCommand.cs` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Documentation/03-Design/TacticalMapSpecification.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Board/EP-0008-player-defense-and-hostile-combat/ImplementationStatus.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Board/EP-0010-combat-reports/ImplementationStatus.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `DB/Board/EP-0011-short-fixes/ImplementationStatus.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |

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

2026-10-21: documentation and self-review complete. PASS 163 relative path references across 14 scoped files, diff check. [Evidence](../../evidence/us0015-tk0004-results.json). Canonical EP12 requirements, current writer/data/tooling and release navigation synchronized. Prior epics explicitly superseded; source changes are XML/comments only, no executable behavior. Corrected combat-input.json path from source. Runtime tests not rerun for prose-only changes. Human/product/balance OPEN. Separate commit/push follows.
