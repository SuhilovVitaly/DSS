---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0002-stale-epic-headers
ticket: EP-0031-US-0002-TK-0002-ep0021-demo-supersede
title: Пометка EP-0021
stage: approved
approval_kind: autonomous-execution
implementation_status: pending
layer: documentation
test_project: none
depends_on: [EP-0031-US-0001-TK-0001-board-registry]
files_touched: 1
serves: [AC-02]
created: 2026-11-16T19:36:44Z
revision: 1
---

# TK-0002 — Пометка EP-0021

## Why

EP-0021 D-01 (пустой старт) противоречит D-01 EP-0031 (US-0002 AC-02).

## Decisions

D-01 EP-0031: «готовый носитель типа Тетрарх такой как сейчас для тестового боя с необходимыми улучшениями по ходу реализации эпиков».

## Assumptions

Распределение историй: US-0003/0004/0005/0011 — уже в EP-0029 (см. таблицу пересечений EP-0029);
US-0001/0002/0006/0009 — EP-0034; US-0010 — EP-0037; US-0012 — EP-0041; US-0007 — EP-0042; US-0008 — отменена D-01.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0021-drone-combat-economy/Documentation.md` | `:4` `stage: draft`; `:47` D-01 пустой старт | Вставить блок-пометку сразу после заголовка `:10`; frontmatter и исходный текст не менять |

## Public API after the change

No API change.

## Implementation steps

1. После строки заголовка вставить цитату «Актуальность 2026-11-16» с заменой D-01, распределением историй и ссылками на EP-0029, EP-0031 и `DB/Board/README.md`.

## Out of scope

Удаление или перенумерация историй EP-0021.

## Invariants

Исходные разделы EP-0021 побайтно сохранены (diff — только вставка).

## Tests

`git diff -U0 DB/Board/EP-0021-*/Documentation.md` показывает только добавленные строки; ссылки существуют; `git diff --check`.

## Definition of Done

- Пометка вставлена, остальное не изменено.
- Ссылки пометки ведут на существующие файлы.

## Self-containment check

Текст пометки полностью определён Decisions и Assumptions.
