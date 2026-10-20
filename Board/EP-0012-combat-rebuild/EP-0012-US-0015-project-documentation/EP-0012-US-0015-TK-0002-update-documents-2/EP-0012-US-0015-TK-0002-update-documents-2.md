---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0015-project-documentation"
ticket: "EP-0012-US-0015-TK-0002-update-documents-2"
title: "update-documents-2"
stage: "draft"
layer: "documentation"
depends_on: ["EP-0012-US-0015-TK-0001-update-documents-1"]
files_touched: 4
serves: ["US15-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0015-TK-0002-update-documents-2

## Why

[Документация (обязательная завершающая история)](../EP-0012-US-0015-project-documentation.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: update-documents-2; доказать US15-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [Documentation/04-Engineering/PhaseThreeCombatAcceptance.md](../../../../Documentation/04-Engineering/PhaseThreeCombatAcceptance.md) | Существует: Documentation/04-Engineering/PhaseThreeCombatAcceptance.md:1; # Phase Three Combat Acceptance — EP-0008 | Только изменения для scope этой карточки и served AC. |
| [Documentation/04-Engineering/CombatReports.md](../../../../Documentation/04-Engineering/CombatReports.md) | Существует: Documentation/04-Engineering/CombatReports.md:1; # Боевые доклады экипажа — EP-0010 | Только изменения для scope этой карточки и served AC. |
| [Documentation/02-FirstRelease/TechnicalTasks/CombatTrajectoryForecasting.md](../../../../Documentation/02-FirstRelease/TechnicalTasks/CombatTrajectoryForecasting.md) | Существует: Documentation/02-FirstRelease/TechnicalTasks/CombatTrajectoryForecasting.md:1; # ТЗ: прогноз траекторий торпед и ПРО с учётом текущего манёвра цели | Только изменения для scope этой карточки и served AC. |
| [Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md) | Существует: Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md:1; # Панель боевых докладов на тактическом экране | Только изменения для scope этой карточки и served AC. |

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

Все ниже — **план**, не выполненные тесты.

- **Documentation_links_and_numeric_contract_match_implementation**: каждый старый термин либо исторический с пометкой, либо исправлен; фактические NOT RUN сохранены.

Документарный/ручной gate: проверить перечисленные критерии и локальные ссылки; `git diff --check`. Product build не требуется для evidence-only изменений. Native выполняется только при отдельном разрешении на игру; иначе NOT RUN с причиной. Graphify output проверяется при фактическом rebuild.

## Definition of Done

- Served AC US15-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
