---
epic: EP-0020-tactical-battle-mechanics
stage: draft
created: 2026-11-08T11:46:28Z
---

# EP-0020 — карта тикетов

[Эпик и решения](Documentation.md) · [UiSpec](UiSpec.md) · 5 историй, 13 тикетов. Механика M1 «Уклонение и сознание» — US-0001…US-0004; US-0005 — документация, всегда последняя. Порядок строк соблюдает зависимости.

**Весь эпик исполняется после полного выполнения и приёмки [EP-0019](../EP-0019-tactical-battle-ui-redesign/Tickets.md).**

## Истории

| ID | Файл | Тикетов | Dependencies |
|---|---|---|---|
| US-0001 | [EP-0020-US-0001-consciousness](EP-0020-US-0001-consciousness/EP-0020-US-0001-consciousness.md) | 2 | EP-0019 |
| US-0002 | [EP-0020-US-0002-evasion-resolution](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-evasion-resolution.md) | 5 | US-0001 |
| US-0003 | [EP-0020-US-0003-evasion-calibration](EP-0020-US-0003-evasion-calibration/EP-0020-US-0003-evasion-calibration.md) | 1 | US-0002 |
| US-0004 | [EP-0020-US-0004-evasion-ui](EP-0020-US-0004-evasion-ui/EP-0020-US-0004-evasion-ui.md) | 4 | US-0002, EP-0019 |
| US-0005 | [EP-0020-US-0005-project-documentation](EP-0020-US-0005-project-documentation/EP-0020-US-0005-project-documentation.md) | 1 | US-0003, US-0004 |

## Тикеты

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
|
|
|
|
|
|
|
|
|
|
|
|
|

## Контроль передачи

- **US-0001:** данные и состояние сознания без изменения исходов боёв.
- **US-0002:** механика в движке; ожидания исторических тестов обновляются только с записанной причиной.
- **US-0003:** отчёт калибровки для штрафа −20; данные не меняются.
- **US-0004:** интерфейс по UiSpec; проверки вида и регрессионные снимки EP-0019 зелёные, пробы UiSpec §7 проходят.
- **US-0005:** документация и ExecutionStatus.

Блокирующие решения пользователя: [открытые вопросы](Documentation.md#открытые-вопросы).
