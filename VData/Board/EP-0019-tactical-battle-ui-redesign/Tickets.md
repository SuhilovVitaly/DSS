---
epic: EP-0019-tactical-battle-ui-redesign
stage: draft
created: 2026-11-08T09:31:05Z
---

# EP-0019 — карта тикетов

[Эпик и решения](Documentation.md) · [DesignSpec](DesignSpec.md) · [Assets](Assets/README.md) · 7 историй, 21 тикетов, включая завершающую историю документации. Порядок строк соблюдает зависимости.

## Истории

| ID | Файл | Тикетов | Dependencies |
|---|---|---|---|
| US-0001 | [EP-0019-US-0001-reference-and-golden-harness](EP-0019-US-0001-reference-and-golden-harness/EP-0019-US-0001-reference-and-golden-harness.md) | 3 | none |
| US-0002 | [EP-0019-US-0002-overlay-layout-shell](EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-overlay-layout-shell.md) | 4 | US-0001 |
| US-0003 | [EP-0019-US-0003-pilot-cards](EP-0019-US-0003-pilot-cards/EP-0019-US-0003-pilot-cards.md) | 3 | US-0002 |
| US-0004 | [EP-0019-US-0004-phase-panel](EP-0019-US-0004-phase-panel/EP-0019-US-0004-phase-panel.md) | 5 | US-0002 |
| US-0005 | [EP-0019-US-0005-map-presentation](EP-0019-US-0005-map-presentation/EP-0019-US-0005-map-presentation.md) | 3 | US-0002 |
| US-0006 | [EP-0019-US-0006-hover-and-acceptance](EP-0019-US-0006-hover-and-acceptance/EP-0019-US-0006-hover-and-acceptance.md) | 2 | US-0003, US-0004, US-0005 |
| US-0007 | [EP-0019-US-0007-project-documentation](EP-0019-US-0007-project-documentation/EP-0019-US-0007-project-documentation.md) | 1 | US-0001, US-0002, US-0003, US-0004, US-0005, US-0006 |

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
|
|
|
|
|
|
|
|

## Контроль передачи

- **US-0001:** сборка, шрифт, сцены и проверки готовы до первой правки экрана; проверки вида пока `Skip`.
- **US-0002:** каркас, токены, камера, строки — основа для US-0003–US-0005.
- **US-0003–US-0005:** каждый тикет включает проверки вида своей области (DesignSpec §12).
- **US-0006:** наведение, все проверки вида семи сцен и регрессионные снимки — главный критерий D-08.
- **US-0007:** документация и ExecutionStatus.

Блокирующие решения пользователя: Q-01–Q-04 в [Documentation.md](Documentation.md#открытые-вопросы-нужно-решение-до-утверждения).
