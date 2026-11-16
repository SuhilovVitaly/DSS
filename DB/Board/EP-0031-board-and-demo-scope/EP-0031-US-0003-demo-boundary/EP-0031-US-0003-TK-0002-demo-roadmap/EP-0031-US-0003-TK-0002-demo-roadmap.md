---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0003-demo-boundary
ticket: EP-0031-US-0003-TK-0002-demo-roadmap
title: Дорожная карта демо
stage: approved
approval_kind: autonomous-execution
implementation_status: pending
layer: documentation
test_project: none
depends_on: [EP-0031-US-0003-TK-0001-demo-requirements]
files_touched: 1
serves: [AC-03]
created: 2026-11-16T19:36:44Z
revision: 1
---

# TK-0002 — Дорожная карта демо

## Why

Порядок демо-эпиков должен лежать в Board, единственном источнике планов (US-0003 AC-03).

## Decisions

D-01…D-04 эпика.

## Assumptions

A-03 эпика: номера EP-0032…EP-0044 — резерв; фазы и плейтесты — из предложенного плана.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/README.md` | Реестр из US-0001-TK-0001 | Добавить раздел «Дорожная карта первой демо-версии» |

## Public API after the change

No API change.

## Implementation steps

1. Добавить таблицу: номер, название, приоритет, фаза, цель, зависимости.
2. Добавить порядок фаз и три контрольные точки: плейтест 1 после EP-0034, плейтест 2 после EP-0040, демо-релиз после EP-0044.

## Out of scope

Создание папок будущих эпиков.

## Invariants

Реестр US-0001 не изменён.

## Tests

Номера EP-0032…EP-0044 не совпадают с существующими каталогами (`ls -d DB/Board/EP-003*`); `git diff --check`.

## Definition of Done

- Раздел добавлен; 13 строк; зависимости ссылаются только на номера карты или существующие эпики.

## Self-containment check

Состав карты определён в Documentation.md эпика и в этом тикете.
