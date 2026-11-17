---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0002-stale-epic-headers
ticket: EP-0031-US-0002-TK-0001-ep0029-status-sync
title: Статус EP-0029
stage: approved
approval_kind: autonomous-execution
implementation_status: implemented
layer: documentation
test_project: none
depends_on: [EP-0031-US-0001-TK-0001-board-registry]
files_touched: 2
serves: [AC-01]
created: 2026-11-16T19:36:44Z
revision: 1
---

# TK-0001 — Статус EP-0029

## Why

EP-0029 ложно утверждает, что реализация не начата (US-0002 AC-01).

## Decisions

D-05 эпика.

## Assumptions

A-01, A-02 эпика: правится только ложное утверждение; приёмка не объявляется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0029-unified-carrier-and-torpedoes/Documentation.md` | `:11` `implementation_status: not-started`; `:18` «реализация не начата» | Заменить статус на `implemented`; заменить фразу на сводку с evidence |
| `DB/Board/EP-0029-unified-carrier-and-torpedoes/Tickets.md` | `:3` «Implementation NOT STARTED» | Заменить на ссылку на фактическую реализацию |

## Public API after the change

No API change.

## Implementation steps

1. В `Documentation.md` поставить `implementation_status: implemented`, в строке 18 описать: код US-0001…US-0013 поставлен (`git log --grep EP-0029`), шесть дефектов исправлены по `stories/story-20261115-211353-ep-0029-remediation.md`, приёмка владельцем и native gates открыты; добавить ссылку на EP-0031.
2. В `Tickets.md` заменить «Implementation NOT STARTED» тем же утверждением.

## Out of scope

Stage story- и ticket-файлов EP-0029, исторические таблицы grounding.

## Invariants

Решения D-01…D-07 и grounding EP-0029 не изменены.

## Tests

`grep -n "не начата\|NOT STARTED" DB/Board/EP-0029-*/Documentation.md DB/Board/EP-0029-*/Tickets.md` — пусто; `git diff --check`.

## Definition of Done

- Шаги выполнены только в двух файлах.
- Grep из Tests пуст.
- Приёмка не объявлена выполненной.

## Self-containment check

Строки и замены указаны точно; evidence — коммиты и story-файл из шага 1.

## Результат исполнения

`EP-0029/Documentation.md`: `implementation_status: implemented`; строка 18 описывает поставленный код, remediation-story и открытую приёмку. `EP-0029/Tickets.md:3`: «Implementation NOT STARTED» заменено ссылкой на статус. Строка 241 Documentation.md («Реализация не начата … в этой planning-задаче») — датированная запись планирования, сохранена как история. Проверки: ссылки 0 битых; `git diff --check` чисто.
