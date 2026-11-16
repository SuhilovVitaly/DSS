---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0003-demo-boundary
ticket: EP-0031-US-0003-TK-0001-demo-requirements
title: Граница демо в требованиях
stage: approved
approval_kind: autonomous-execution
implementation_status: pending
layer: documentation
test_project: none
depends_on: [EP-0031-US-0001-TK-0001-board-registry]
files_touched: 1
serves: [AC-01, AC-02]
created: 2026-11-16T19:36:44Z
revision: 1
---

# TK-0001 — Граница демо в требованиях

## Why

Решения владельца о демо должны жить в каноническом документе требований (US-0003).

## Decisions

D-01…D-04 эпика с исходным сообщением (см. Documentation.md эпика). Владелец явно
поручил «сделай EP-N», пункт которого — переписать границы демо в этом документе.

## Assumptions

Демо-требования помечаются «принято, не реализовано»; номера эпиков указываются ссылкой на реестр.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/01-Requirements/FirstReleaseRequirements.md` | 21 строка; `:14` Hire/Contracts/Ship — оболочки; нет раздела демо | Переименовать в «Границы текущей игры и первой демо-версии»; добавить раздел «Первая демо-версия»; таблицу текущих границ сохранить |

## Public API after the change

No API change.

## Implementation steps

1. Добавить раздел: цель (вертикальный срез 60–120 мин), цикл, D-01…D-04, состав по фазам со ссылкой на `DB/Board/README.md`.
2. Явно написать, что эти требования приняты, но не реализованы, и что Hire/Contracts остаются оболочками до соответствующих эпиков.

## Out of scope

`EngineRequirements.md`, остальные документы (их сверка — US-0004).

## Invariants

Строки таблицы текущих границ не меняют смысл (diff строк таблицы пуст).

## Tests

Проверка ссылок документа; `git diff --check`; ручная сверка таблицы.

## Definition of Done

- Раздел добавлен, таблица сохранена, ссылки существуют.

## Self-containment check

Содержание раздела полностью задано D-01…D-04 и реестром.
