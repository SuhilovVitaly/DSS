---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0001-epic-registry
ticket: EP-0031-US-0001-TK-0001-board-registry
title: Реестр эпиков Board
stage: approved
approval_kind: autonomous-execution
implementation_status: pending
layer: documentation
test_project: none
depends_on: []
files_touched: 2
serves: [AC-01, AC-02, AC-03]
created: 2026-11-16T19:36:44Z
revision: 1
---

# TK-0001 — Реестр эпиков Board

## Why

Нужна одна точка правды о статусе эпиков перед планированием демо (US-0001).

## Decisions

D-05 эпика: выполнять без промежуточных подтверждений.

## Assumptions

A-01, A-02 эпика. Категории: «Действует», «Частично», «Заменён», «Не начат», «Постоянный».

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/README.md` | Файла нет | Создать реестр с легендой и таблицей EP-0001…EP-0030, EP-maintenance |
| этот файл | — | Записать результат |

## Public API after the change

No API change.

## Implementation steps

1. Для каждого каталога `DB/Board/EP-*` взять `title`/`stage` из `Documentation.md`, число коммитов `git log --oneline | grep -c "EP-<NNNN>"` и вывод отчёта исполнения.
2. Присвоить категорию; для EP-0006…EP-0010, EP-0012, EP-0014 сослаться на D-USER-01 EP-0015.
3. Записать таблицу и пояснение, что категория не является приёмкой.

## Out of scope

Правка файлов других эпиков.

## Invariants

Ни один исторический отчёт не изменён (`git diff --stat` затрагивает только два файла).

## Tests

Проверка: количество строк реестра равно числу каталогов `ls -d DB/Board/EP-*`; все относительные ссылки существуют; `git diff --check`.

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- AC-01…AC-03 проверены командами из раздела Tests.
- Нет работы вне Code context.

## Self-containment check

Источники статуса перечислены в шагах; категории и легенда заданы здесь.
