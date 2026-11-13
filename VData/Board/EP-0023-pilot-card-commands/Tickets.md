---
epic: EP-0023-pilot-card-commands
stage: draft
created: 2026-11-11T14:30:00Z
---

# EP-0023 — карта тикетов

[Эпик и решения](Documentation.md) · [UiSpec](UiSpec.md) · 8 историй, 13 тикетов. Порядок строк соблюдает зависимости. База — `main` @ `8b0bf6ce` (после EP-0022).

## Истории

| ID | Файл | Тикетов | Dependencies |
|---|---|---|---|
| US-0001 | [EP-0023-US-0001-card-column](EP-0023-US-0001-card-column/EP-0023-US-0001-card-column.md) | 3 | main @ 8b0bf6ce |
| US-0002 | [EP-0023-US-0002-move-in-card](EP-0023-US-0002-move-in-card/EP-0023-US-0002-move-in-card.md) | 1 | US-0001 |
| US-0003 | [EP-0023-US-0003-fire-in-card](EP-0023-US-0003-fire-in-card/EP-0023-US-0003-fire-in-card.md) | 2 | US-0001 |
| US-0004 | [EP-0023-US-0004-salvo-in-card](EP-0023-US-0004-salvo-in-card/EP-0023-US-0004-salvo-in-card.md) | 1 | US-0001 |
| US-0005 | [EP-0023-US-0005-compact-panel](EP-0023-US-0005-compact-panel/EP-0023-US-0005-compact-panel.md) | 2 | US-0002, US-0003 |
| US-0006 | [EP-0023-US-0006-skins](EP-0023-US-0006-skins/EP-0023-US-0006-skins.md) | 2 | US-0002, US-0003, US-0004, US-0005 |
| US-0007 | [EP-0023-US-0007-view-regression](EP-0023-US-0007-view-regression/EP-0023-US-0007-view-regression.md) | 1 | US-0006 |
| US-0008 | [EP-0023-US-0008-project-documentation](EP-0023-US-0008-project-documentation/EP-0023-US-0008-project-documentation.md) | 1 | US-0001, US-0002, US-0003, US-0004, US-0005, US-0006, US-0007 |

## Тикеты

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0023-US-0001-TK-0001-column-layout](EP-0023-US-0001-card-column/EP-0023-US-0001-TK-0001-column-layout/EP-0023-US-0001-TK-0001-column-layout.md) | Раскладка колонки переменной высоты и прокрутка | client-ui | — | US1-AC2, US1-AC3 |
| [EP-0023-US-0001-TK-0002-card-base](EP-0023-US-0001-card-column/EP-0023-US-0001-TK-0002-card-base/EP-0023-US-0001-TK-0002-card-base.md) | Базовая карточка: портрет, плашка, вертикальные шкалы и иконки | client-ui | `EP-0023-US-0001-TK-0001-column-layout` | US1-AC1 |
| [EP-0023-US-0001-TK-0003-card-commands-input](EP-0023-US-0001-card-column/EP-0023-US-0001-TK-0003-card-commands-input/EP-0023-US-0001-TK-0003-card-commands-input.md) | Команды карточки: реестр, клики, перетаскивание, колесо | client-ui | `EP-0023-US-0001-TK-0001-column-layout` | US1-AC3 |
| [EP-0023-US-0002-TK-0001-move-block](EP-0023-US-0002-move-in-card/EP-0023-US-0002-TK-0001-move-block/EP-0023-US-0002-TK-0001-move-block.md) | Блок движения в карточке: отрисовка и управление | client-ui | `EP-0023-US-0001-TK-0003-card-commands-input` | US2-AC1, US2-AC2, US2-AC3 |
| [EP-0023-US-0003-TK-0001-fire-block-render](EP-0023-US-0003-fire-in-card/EP-0023-US-0003-TK-0001-fire-block-render/EP-0023-US-0003-TK-0001-fire-block-render.md) | Блок стрельбы в карточке: состояния и подробности | client-ui | `EP-0023-US-0001-TK-0003-card-commands-input` | US3-AC1, US3-AC2 |
| [EP-0023-US-0003-TK-0002-fire-block-input](EP-0023-US-0003-fire-in-card/EP-0023-US-0003-TK-0002-fire-block-input/EP-0023-US-0003-TK-0002-fire-block-input.md) | Блок стрельбы: цели, ловушка, «Не стрелять», уклонение и прокрутка | client-ui | `EP-0023-US-0003-TK-0001-fire-block-render` | US3-AC2, US3-AC3 |
| [EP-0023-US-0004-TK-0001-salvo-block](EP-0023-US-0004-salvo-in-card/EP-0023-US-0004-TK-0001-salvo-block/EP-0023-US-0004-TK-0001-salvo-block.md) | Блок залпа и итога выстрела в карточке | client-ui | `EP-0023-US-0001-TK-0001-column-layout` | US4-AC1, US4-AC2 |
| [EP-0023-US-0005-TK-0001-compact-panel](EP-0023-US-0005-compact-panel/EP-0023-US-0005-TK-0001-compact-panel/EP-0023-US-0005-TK-0001-compact-panel.md) | Сжатая правая панель движения и стрельбы | client-ui | `EP-0023-US-0002-TK-0001-move-block`, `EP-0023-US-0003-TK-0002-fire-block-input` | US5-AC1, US5-AC2 |
| [EP-0023-US-0005-TK-0002-remove-panel-controls](EP-0023-US-0005-compact-panel/EP-0023-US-0005-TK-0002-remove-panel-controls/EP-0023-US-0005-TK-0002-remove-panel-controls.md) | Удаление элементов выбранного дрона из правой панели | client-ui | `EP-0023-US-0005-TK-0001-compact-panel` | US5-AC3 |
| [EP-0023-US-0006-TK-0001-skin-roles](EP-0023-US-0006-skins/EP-0023-US-0006-TK-0001-skin-roles/EP-0023-US-0006-TK-0001-skin-roles.md) | Роли скина для новой карточки и значения в паках | client-ui | `EP-0023-US-0005-TK-0002-remove-panel-controls` | US6-AC1, US6-AC3 |
| [EP-0023-US-0006-TK-0002-skin-scenes](EP-0023-US-0006-skins/EP-0023-US-0006-TK-0002-skin-scenes/EP-0023-US-0006-TK-0002-skin-scenes.md) | Сцены скинов с новой карточкой | client-tests | `EP-0023-US-0006-TK-0001-skin-roles` | US6-AC2 |
| [EP-0023-US-0007-TK-0001-snapshots](EP-0023-US-0007-view-regression/EP-0023-US-0007-TK-0001-snapshots/EP-0023-US-0007-TK-0001-snapshots.md) | Пересоздание снимков и пробы приёмки | client-tests | `EP-0023-US-0006-TK-0002-skin-scenes` | US7-AC1, US7-AC2 |
| [EP-0023-US-0008-TK-0001-docs](EP-0023-US-0008-project-documentation/EP-0023-US-0008-TK-0001-docs/EP-0023-US-0008-TK-0001-docs.md) | Документация карточки пилота с командами | docs | `EP-0023-US-0007-TK-0001-snapshots` | US8-AC1, US8-AC2 |

## Контроль передачи

- **US-0001:** новая колонка и базовая карточка; без раскрытия раскладка совпадает с текущей; маршрутизация команд готова.
- **US-0002:** движение выбранного дрона управляется из карточки; приказы совпадают с клавишами.
- **US-0003:** цель, ловушка, «Не стрелять» и уклонение — только в карточке выбранного дрона; планы совпадают с прежними.
- **US-0004:** итог выстрела в карточке стрелявшего.
- **US-0005:** правая панель движения и стрельбы сжата до `…152`; старые элементы удалены.
- **US-0006:** роли скина, значения в contrast и Xenon Star, сцены скинов зелёные.
- **US-0007:** 15 снимков пересозданы, пробы UiSpec §10 зелёные.
- **US-0008:** документация и ExecutionStatus.
