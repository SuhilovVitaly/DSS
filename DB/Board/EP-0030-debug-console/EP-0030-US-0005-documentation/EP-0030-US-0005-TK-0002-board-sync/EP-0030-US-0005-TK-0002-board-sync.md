---
epic: EP-0030-debug-console
story: EP-0030-US-0005-documentation
ticket: EP-0030-US-0005-TK-0002-board-sync
title: Синхронизация Board эпика EP-0030
stage: approved
approval_kind: autonomous-execution
implementation_status: implemented
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

## Execution evidence 2026-11-16

Изменённые файлы (7, в пределах allowlist; плюс этот тикет):

- `DB/Board/EP-0030-debug-console/Documentation.md` — `stage: implemented`, `current_review: complete`; раздел «Статус исполнения (2026-11-16)»; решения пользователя 2026-11-16 (G-01 «Штатный экран гибели», G-04 «Id или подпись на карте», «Декомпозировать сам»); пометка, что A-01..A-08 — допущения исполнителя, а не решения пользователя; таблица историй со статусами и итогом 5/13; недатированная сводка «Сборка, тесты и игра не запускались» помечена как состояние планирования со ссылкой на текущий результат; запись review log.
- `DB/Board/EP-0030-debug-console/Tickets.md` (новый, по образцу EP-0004/EP-0029) — статусы историй, 13 тикетов с коммитами, валидация эпика.
- Пять story-файлов US-0001..US-0005 — `stage: implemented`; в US-0005 — review log и story evidence.

Карта SHA получена `git log --oneline 43a6fecf..HEAD` при HEAD `c95ba2a7`; все 17 упомянутых SHA проверены `git cat-file -e`. Собственный SHA коммита TK-0002 заранее не записывается — проверяется после публикации через `git log`.

Валидация, записанная в `Tickets.md` (по данным оркестратора и артефактам, не повторялась в этом тикете):

- Полный build/test на `aff52cb0`: 0 ошибок сборки; 810 падений, набор идентичен базе `43a6fecf` (Engine 630, Client 166, Performance 10, EconomyBalance 3, Motion 1); (a) регрессий 0, (b) новых падающих 0; 96 новых тестов проходят. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/full-20261116T075334Z/`, `full-base-43a6fecf-20261116T075945Z/diff.txt` (заголовок diff.txt подтверждает: «HEAD failed=810 base failed=810; failed rows identical: yes … tests new at HEAD: 96, all passed»).
- Native/manual: NOT RUN — меньший монитор не подтверждён; продуктовая приёмка открыта.
- Graphify: NOT RUN — `src/graphify-out/` игнорируется Git и содержит только исторический кеш (2026-11-05, без `graph.json`), корневого `graphify-out/` нет, `.graphifyignore` не включает EP-0030.
- Независимая валидация (Phase 4): регрессий нет, ключевые утверждения подтверждены; остаточное — native не выполнен, открытие консоли сбрасывает выбор карты/ввод манёвра как потеря фокуса.

Проверки этого тикета:

- Ссылки и якоря в 8 изменённых/новых файлах Board — perl-скрипт вне репозитория: `problems: 0`.
- ID и количества: 5 каталогов историй, 13 каталогов тикетов, 13 строк тикетов в `Tickets.md`, каждый каталог тикета упомянут.
- `git diff --check` — exit 0.
