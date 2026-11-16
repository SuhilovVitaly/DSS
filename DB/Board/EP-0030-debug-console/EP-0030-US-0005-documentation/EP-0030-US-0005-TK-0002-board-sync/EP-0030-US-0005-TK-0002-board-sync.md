---
epic: EP-0030-debug-console
story: EP-0030-US-0005-documentation
ticket: EP-0030-US-0005-TK-0002-board-sync
title: Синхронизация Board эпика EP-0030
stage: approved
approval_kind: autonomous-execution
layer: documentation
test_project: none
depends_on: [EP-0030-US-0005-TK-0001-canonical-docs]
prerequisite_stories: [EP-0030-US-0001-global-console, EP-0030-US-0002-exit-tutorial-battle, EP-0030-US-0003-kill-drone, EP-0030-US-0004-console-discovery]
files_touched: 7
serves: [AC-03, AC-04]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0002 — Board sync

## Why

Board должен отражать фактическое состояние эпика: истории, тикеты, коммиты и непройденные проверки. Покрывает AC-03/04 [истории](../EP-0030-US-0005-documentation.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: G-01 «Штатный экран гибели»; G-04 «Id или подпись на карте»; декомпозиция «Декомпозировать сам».

## Assumptions

- Лимит пяти файлов относится к implementation files; здесь меняются только Board-метаданные (эпик, `Tickets.md`, frontmatter `stage`/статус пяти story-файлов). Расширение allowlist до 7 разрешено `DB/Board/EpicExecutionPrompt.md` и записано здесь заранее.
- Native-валидация записывается NOT RUN, если меньший монитор не подтверждён; Graphify — NOT RUN (graph output отсутствует).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0030-debug-console/Documentation.md` | Таблица историй, gaps, review log | Статусы историй, итоговый review log |
| `DB/Board/EP-0030-debug-console/Tickets.md` | Новый | Тикет → commit SHA, статусы историй, native validation, Graphify NOT RUN |
| `DB/Board/EP-0030-debug-console/EP-0030-US-0001-global-console/EP-0030-US-0001-global-console.md` | stage approved | Итоговый статус реализации |
| `DB/Board/EP-0030-debug-console/EP-0030-US-0002-exit-tutorial-battle/EP-0030-US-0002-exit-tutorial-battle.md` | stage approved | Итоговый статус |
| `DB/Board/EP-0030-debug-console/EP-0030-US-0003-kill-drone/EP-0030-US-0003-kill-drone.md` | stage approved | Итоговый статус |
| `DB/Board/EP-0030-debug-console/EP-0030-US-0004-console-discovery/EP-0030-US-0004-console-discovery.md` | stage draft, ticket map | Итоговый статус |
| `DB/Board/EP-0030-debug-console/EP-0030-US-0005-documentation/EP-0030-US-0005-documentation.md` | stage approved | Итоговый статус |

## Public API after the change

No API change.

## Implementation steps

1. Собрать SHA всех коммитов тикетов (`git log --oneline` ветки `feature/ep-0030-debug-console`).
2. Создать `Tickets.md`; обновить эпик и статусы историй.
3. Проверить ссылки, ID, `git diff --check`.

## Out of scope

Каноническая документация (TK-0001), production-код, тикеты других эпиков.

## Invariants

Не приписывать пользователю решений, которых он не давал; собственная проверка не называется независимым review.

## Tests

Нет automated-тестов (documentation). Проверка: ссылки, ID, `git diff --check`.

## Definition of Done

- `Tickets.md` содержит все тикеты с SHA; статусы согласованы; ссылки разрешаются; `git diff --check` чист.
- Execution evidence, commit + push.

## Self-containment check

Файлы, источник SHA и формат записи NOT RUN заданы.
