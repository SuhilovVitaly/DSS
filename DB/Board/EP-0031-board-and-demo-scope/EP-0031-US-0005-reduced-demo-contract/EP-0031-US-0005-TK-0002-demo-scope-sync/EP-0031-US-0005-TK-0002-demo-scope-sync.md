---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0005-reduced-demo-contract
ticket: EP-0031-US-0005-TK-0002-demo-scope-sync
title: Процессные документы, каталог экранов и граница демо в Board
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: documentation
test_project: none
depends_on: [EP-0031-US-0005-TK-0001-current-requirements]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T00:00:00Z
revision: 1
---

# Процессные документы, каталог экранов и граница демо в Board

## Why

Тикет 2/2 истории EP-0031-US-0005. Закрывает остаток AC-02 вне требований и AC-01/AC-03 на стороне Board, чтобы исполнители EP-0032 начинали с согласованной границы демо. [Story](../EP-0031-US-0005-reduced-demo-contract.md).

## Decisions

Добавлен 2026-11-17 при ревью декомпозиции EP-0032…EP-0041 (поручение владельца: «пройди по всем, реши нужно ли что-то изменить и добавить. делай это автономно»). Human approval формулировок не заявляется.

## Assumptions

- Матрица переиспользования EP-0021/EP-0029 уже есть в DemoPlan; тикет проверяет её полноту, а не пишет заново.
- После TK-0001 канонические требования уже описывают Save21.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/00-Process/CLAUDE.md` | `:25` — «Карта и временный тактический бой имеют отдельные DTO и камеры» | Уточнить: сессия боя временная, исход постоянный через settlement; ссылка на актуальный Save21. |
| `DB/Documentation/02-FirstRelease/Screens/ScreenCatalog.md` | `:28` — «Hire, Contracts и отдельный Ship — оболочки» | Ship — рабочий экран EP-0029; Hire/Contracts — заглушки до EP-0033-US-0004 / EP-0034-US-0006. |
| `DB/Board/EP-0031-board-and-demo-scope/DemoPlan.md` | Сокращённый план rev 2, матрица EP-0021/EP-0029, таблица покрытия | Только проверка/дополнение: каждому демо-результату EP-0021 назначен владелец; пустой старт, верфь и backlog помечены «не зависимость демо». |
| `DB/Board/EP-0031-board-and-demo-scope/Documentation.md` | Карточка эпика EP-0031 | Статус US-0005 и ссылки на evidence TK-0001/TK-0002. |

Разрешены только эти 4 файла. Read-only: [README](../../../README.md), [TicketPlan](../../../EP-0041-demo-validation-and-release/TicketPlan.md), [EP-0021](../../../EP-0021-drone-combat-economy/Documentation.md).

## Public API after the change

No API change.

## Implementation steps

1. Поправить две строки процессных документов по факту кода (см. таблицу) и проверить, что больше нигде в `DB/Documentation` нет утверждения «Save20 — текущий writer» (`grep`); найденное вне allowlist записать в Board как остаток.
2. Сверить DemoPlan с README и TicketPlan: 60–120 минут, три способа заработка (торговля, перевозки, бой/заказы), один кластер, готовый Tetrarch с четырьмя пассажирскими местами. Каждый пункт EP-0021 имеет владельца из EP-0032…EP-0041 или явное «вне демо».
3. Отметить в карточке EP-0031: US-0005 выполнена, ссылки на evidence обоих тикетов. Исторические US-0001…US-0004 не переоткрывать.

## Served acceptance criteria

- **AC-01:** Board и актуальные требования согласованы с сокращённым объёмом: 60–120 минут, три способа заработка, один кластер, готовый носитель и дополнительные пассажирские места.
- **AC-02:** Устранены устаревшие текущие описания Save20, временного боя и Ship shell после сверки источников; история прежних поставок сохранена.
- **AC-03:** EP-0021 сопоставлен с новым владельцем каждого демо-результата; пустой старт, верфь и прочий backlog не стали обязательными зависимостями.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Out of scope

Правка кода, тикетов EP-0032…EP-0041 и README-реестра (его ведут board-status тикеты эпиков).

## Invariants

- Board — единственное место статуса; Documentation описывает текущее поведение.
- Исторические решения и evidence не переписываются.

## Tests

Проверки: LinksResolve, StatementsMatchSource, OnlyMarkdownInDocumentation; `grep` на «Save20» в `DB/Documentation` даёт только исторические упоминания миграции; `git diff --check`. Build/test не требуется.

## Definition of Done

- Четыре файла обновлены в allowlist; найденные остатки вне allowlist записаны в Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/EP-0031-US-0005-TK-0002-demo-scope-sync/<run>/`.

## Self-containment check

Allowlist, шаги и проверки перечислены здесь. Новый продуктовый выбор или расширение файлов требует правки Board.
