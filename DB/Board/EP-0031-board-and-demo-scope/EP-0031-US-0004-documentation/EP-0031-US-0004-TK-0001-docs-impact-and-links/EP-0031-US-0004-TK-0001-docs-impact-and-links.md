---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0004-documentation
ticket: EP-0031-US-0004-TK-0001-docs-impact-and-links
title: Инвентаризация и навигация
stage: approved
approval_kind: autonomous-execution
implementation_status: pending
layer: documentation
test_project: none
depends_on: [EP-0031-US-0002-TK-0001-ep0029-status-sync, EP-0031-US-0002-TK-0002-ep0021-demo-supersede, EP-0031-US-0003-TK-0002-demo-roadmap]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-16T19:36:44Z
revision: 1
---

# TK-0001 — Инвентаризация и навигация

## Why

Завершающая документационная история (US-0004).

## Decisions

D-05 эпика.

## Assumptions

A-04, A-05 эпика.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0031-board-and-demo-scope/DocumentationImpact.md` | Нет | Создать инвентаризацию |
| `DB/Documentation/README.md` | `Начать здесь` без демо и реестра | Добавить две ссылки |
| `DB/Board/EP-0031-board-and-demo-scope/Documentation.md` | `implementation_status: in-progress` | Поставить фактический статус и итог |
| `DB/Board/EP-0031-board-and-demo-scope/Tickets.md` | Статусы `pending` | Синхронизировать статусы |

Карточки тикетов EP-0031 обновляются в своих коммитах и в лимит не входят.

## Public API after the change

No API change.

## Implementation steps

1. Найти документы, упоминающие границы релиза, Hire/Contracts, EP-0021 и EP-0029 (`grep -rl`), записать инвентаризацию.
2. Добавить ссылки в README.
3. Проверить все относительные ссылки изменённых файлов скриптом в `D:/DeepSpaceSaga/artifacts/validation/ep-0031/<run>/`, затем удалить скрипт.
4. Синхронизировать Documentation.md и Tickets.md эпика.

## Out of scope

Graphify, push.

## Invariants

В `DB/Documentation` только `.md`; нет файлов в корне репозитория.

## Tests

Скрипт проверки ссылок — 0 битых; `git diff --check`; `git status` без посторонних файлов EP-0031.

## Definition of Done

- AC-01…AC-03 выполнены и записаны в DocumentationImpact.md.

## Self-containment check

Файлы и проверки перечислены полностью.
