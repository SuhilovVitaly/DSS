---
epic: EP-0024-battle-field-redesign
stage: draft
created: 2026-11-12T13:00:00Z
---

# EP-0024 — карта тикетов

Текущее исполнение: **IN PROGRESS** по D-12. Манифест восстанавливается по
UiSpec; отсутствие запекателя блокирует только US-0002-TK-0003.
Фактические результаты и SHA — [ExecutionStatus](ExecutionStatus.md).

[Эпик и решения](Documentation.md) · [UiSpec](UiSpec.md) · 13 историй, 26 тикетов. Порядок строк соблюдает зависимости. База — `main` @ `d7895f73` (после EP-0023). Материалы — [Materials](Materials/README.md).

## Истории

| ID | Файл | Тикетов | Dependencies |
|---|---|---|---|
| US-0001 | [EP-0024-US-0001-skin-pack](EP-0024-US-0001-skin-pack/EP-0024-US-0001-skin-pack.md) | 2 | main @ d7895f73 |
| US-0002 | [EP-0024-US-0002-ship-sprites](EP-0024-US-0002-ship-sprites/EP-0024-US-0002-ship-sprites.md) | 3 | US-0001 |
| US-0003 | [EP-0024-US-0003-counter-plates](EP-0024-US-0003-counter-plates/EP-0024-US-0003-counter-plates.md) | 1 | US-0002 |
| US-0004 | [EP-0024-US-0004-painted-clouds](EP-0024-US-0004-painted-clouds/EP-0024-US-0004-painted-clouds.md) | 2 | US-0001 |
| US-0005 | [EP-0024-US-0005-move-templates](EP-0024-US-0005-move-templates/EP-0024-US-0005-move-templates.md) | 1 | US-0002, US-0003 |
| US-0006 | [EP-0024-US-0006-fire-templates](EP-0024-US-0006-fire-templates/EP-0024-US-0006-fire-templates.md) | 2 | US-0002, US-0003 |
| US-0007 | [EP-0024-US-0007-salvo-results](EP-0024-US-0007-salvo-results/EP-0024-US-0007-salvo-results.md) | 2 | US-0006, US-0002 |
| US-0008 | [EP-0024-US-0008-camera](EP-0024-US-0008-camera/EP-0024-US-0008-camera.md) | 1 | US-0001 |
| US-0011 | [EP-0024-US-0011-steel-panels](EP-0024-US-0011-steel-panels/EP-0024-US-0011-steel-panels.md) | 3 | US-0001 |
| US-0012 | [EP-0024-US-0012-slim-column](EP-0024-US-0012-slim-column/EP-0024-US-0012-slim-column.md) | 2 | US-0011, US-0002 |
| US-0013 | [EP-0024-US-0013-command-panel](EP-0024-US-0013-command-panel/EP-0024-US-0013-command-panel.md) | 4 | US-0012 |
| US-0009 | [EP-0024-US-0009-view-regression](EP-0024-US-0009-view-regression/EP-0024-US-0009-view-regression.md) | 2 | US-0004, US-0005, US-0006, US-0007, US-0008, US-0011, US-0013 |
| US-0010 | [EP-0024-US-0010-project-documentation](EP-0024-US-0010-project-documentation/EP-0024-US-0010-project-documentation.md) | 1 | US-0009 |

## Тикеты

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0024-US-0001-TK-0001-pack-files](EP-0024-US-0001-skin-pack/EP-0024-US-0001-TK-0001-pack-files/EP-0024-US-0001-TK-0001-pack-files.md) | Пакет `tabletop` в клиенте и в сборке | client-data | — | US1-AC1 |
| [EP-0024-US-0001-TK-0002-tabletop-mode](EP-0024-US-0001-skin-pack/EP-0024-US-0001-TK-0002-tabletop-mode/EP-0024-US-0001-TK-0002-tabletop-mode.md) | Признак режима и доступ к листам спрайтов | client-ui | `EP-0024-US-0001-TK-0001-pack-files` | US1-AC2, US1-AC3 |
| [EP-0024-US-0002-TK-0001-sprite-renderer](EP-0024-US-0002-ship-sprites/EP-0024-US-0002-TK-0001-sprite-renderer/EP-0024-US-0002-TK-0001-sprite-renderer.md) | Отрисовка модели: кадр, масштаб, тень, маркеры | client-ui | `EP-0024-US-0001-TK-0002-tabletop-mode` | US2-AC1, US2-AC2 |
| [EP-0024-US-0002-TK-0002-sprite-geometry](EP-0024-US-0002-ship-sprites/EP-0024-US-0002-TK-0002-sprite-geometry/EP-0024-US-0002-TK-0002-sprite-geometry.md) | Занятые прямоугольники и наведение на модели | client-ui | `EP-0024-US-0002-TK-0001-sprite-renderer` | US2-AC3 |
| [EP-0024-US-0002-TK-0003-bake-tool](EP-0024-US-0002-ship-sprites/EP-0024-US-0002-TK-0003-bake-tool/EP-0024-US-0002-TK-0003-bake-tool.md) | Запекатель спрайтов как инструмент тестового проекта | client-tests | `EP-0024-US-0001-TK-0001-pack-files` | US2-AC1 |
| [EP-0024-US-0003-TK-0001-plates](EP-0024-US-0003-counter-plates/EP-0024-US-0003-TK-0001-plates/EP-0024-US-0003-TK-0001-plates.md) | Плашка юнита и её раскладка | client-ui | `EP-0024-US-0002-TK-0002-sprite-geometry` | US3-AC1, US3-AC2 |
| [EP-0024-US-0004-TK-0001-clouds-render](EP-0024-US-0004-painted-clouds/EP-0024-US-0004-TK-0001-clouds-render/EP-0024-US-0004-TK-0001-clouds-render.md) | Отрисовка облаков в режиме | client-ui | `EP-0024-US-0001-TK-0002-tabletop-mode` | US4-AC1, US4-AC3 |
| [EP-0024-US-0004-TK-0002-clouds-cache](EP-0024-US-0004-painted-clouds/EP-0024-US-0004-TK-0002-clouds-cache/EP-0024-US-0004-TK-0002-clouds-cache.md) | Кэш облаков и бюджет времени | client-ui | `EP-0024-US-0004-TK-0001-clouds-render` | US4-AC2 |
| [EP-0024-US-0005-TK-0001-templates](EP-0024-US-0005-move-templates/EP-0024-US-0005-TK-0001-templates/EP-0024-US-0005-TK-0001-templates.md) | Ленты-шаблоны движения | client-ui | `EP-0024-US-0003-TK-0001-plates` | US5-AC1, US5-AC2 |
| [EP-0024-US-0006-TK-0001-fire-render](EP-0024-US-0006-fire-templates/EP-0024-US-0006-TK-0001-fire-render/EP-0024-US-0006-TK-0001-fire-render.md) | Сектор, ленты, угрозы и прицелы | client-ui | `EP-0024-US-0003-TK-0001-plates` | US6-AC1 |
| [EP-0024-US-0006-TK-0002-badges](EP-0024-US-0006-fire-templates/EP-0024-US-0006-TK-0002-badges/EP-0024-US-0006-TK-0002-badges.md) | Значки на лентах: раскладка, порядок, строки | client-ui | `EP-0024-US-0006-TK-0001-fire-render` | US6-AC2 |
| [EP-0024-US-0007-TK-0001-salvo](EP-0024-US-0007-salvo-results/EP-0024-US-0007-TK-0001-salvo/EP-0024-US-0007-TK-0001-salvo.md) | Итог залпа в режиме tabletop | client-ui | `EP-0024-US-0006-TK-0002-badges` | US7-AC2, US7-AC3 |
| [EP-0024-US-0007-TK-0002-torpedo-fx](EP-0024-US-0007-salvo-results/EP-0024-US-0007-TK-0002-torpedo-fx/EP-0024-US-0007-TK-0002-torpedo-fx.md) | Пиксельная торпеда, шлейф и анимация взрыва | client-ui | `EP-0024-US-0007-TK-0001-salvo`, `EP-0024-US-0002-TK-0001-sprite-renderer` | US7-AC1, US7-AC2, US7-AC3 |
| [EP-0024-US-0008-TK-0001-camera](EP-0024-US-0008-camera/EP-0024-US-0008-TK-0001-camera/EP-0024-US-0008-TK-0001-camera.md) | Кадрирование по фазам | client-ui | `EP-0024-US-0001-TK-0002-tabletop-mode` | US8-AC1 |
| [EP-0024-US-0011-TK-0001-steel-roles](EP-0024-US-0011-steel-panels/EP-0024-US-0011-TK-0001-steel-roles/EP-0024-US-0011-TK-0001-steel-roles.md) | Роли и текстуры стальной темы в пакете | client-data | `EP-0024-US-0001-TK-0001-pack-files` | US11-AC1 |
| [EP-0024-US-0011-TK-0002-steel-frames](EP-0024-US-0011-steel-panels/EP-0024-US-0011-TK-0002-steel-frames/EP-0024-US-0011-TK-0002-steel-frames.md) | Прямые углы, тёмный скос и тень плит | client-ui | `EP-0024-US-0011-TK-0001-steel-roles`, `EP-0024-US-0001-TK-0002-tabletop-mode` | US11-AC2 |
| [EP-0024-US-0011-TK-0003-role-colours](EP-0024-US-0011-steel-panels/EP-0024-US-0011-TK-0003-role-colours/EP-0024-US-0011-TK-0003-role-colours.md) | Цвета полос и имени в правых окнах из ролей | client-ui | `EP-0024-US-0011-TK-0001-steel-roles` | US11-AC3 |
| [EP-0024-US-0012-TK-0001-slim-layout](EP-0024-US-0012-slim-column/EP-0024-US-0012-TK-0001-slim-layout/EP-0024-US-0012-TK-0001-slim-layout.md) | Раскладка узкой колонки | client-ui | `EP-0024-US-0011-TK-0002-steel-frames` | US12-AC1 |
| [EP-0024-US-0012-TK-0002-slim-card](EP-0024-US-0012-slim-column/EP-0024-US-0012-TK-0002-slim-card/EP-0024-US-0012-TK-0002-slim-card.md) | Узкая карточка пилота | client-ui | `EP-0024-US-0012-TK-0001-slim-layout`, `EP-0024-US-0002-TK-0001-sprite-renderer` | US12-AC2, US12-AC3 |
| [EP-0024-US-0013-TK-0001-panel-frame](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0001-panel-frame/EP-0024-US-0013-TK-0001-panel-frame.md) | Панель команд: положение, плита, ввод | client-ui | `EP-0024-US-0012-TK-0002-slim-card` | US13-AC1 |
| [EP-0024-US-0013-TK-0002-panel-move](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0002-panel-move/EP-0024-US-0013-TK-0002-panel-move.md) | Движение в панели | client-ui | `EP-0024-US-0013-TK-0001-panel-frame` | US13-AC2 |
| [EP-0024-US-0013-TK-0003-panel-fire](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0003-panel-fire/EP-0024-US-0013-TK-0003-panel-fire.md) | Стрельба в панели | client-ui | `EP-0024-US-0013-TK-0001-panel-frame` | US13-AC3 |
| [EP-0024-US-0013-TK-0004-panel-salvo](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0004-panel-salvo/EP-0024-US-0013-TK-0004-panel-salvo.md) | Полёт и итог залпа в панели | client-ui | `EP-0024-US-0013-TK-0001-panel-frame` | US13-AC4 |
| [EP-0024-US-0009-TK-0001-snapshots](EP-0024-US-0009-view-regression/EP-0024-US-0009-TK-0001-snapshots/EP-0024-US-0009-TK-0001-snapshots.md) | Снимки `tabletop` и проверка Classic | client-tests | `EP-0024-US-0007-TK-0002-torpedo-fx`, `EP-0024-US-0007-TK-0001-salvo`, `EP-0024-US-0008-TK-0001-camera`, `EP-0024-US-0004-TK-0002-clouds-cache`, `EP-0024-US-0005-TK-0001-templates` | US9-AC1, US9-AC2 |
| [EP-0024-US-0009-TK-0002-panel-snapshots](EP-0024-US-0009-view-regression/EP-0024-US-0009-TK-0002-panel-snapshots/EP-0024-US-0009-TK-0002-panel-snapshots.md) | Снимки и пробы панелей `tabletop` | client-tests | `EP-0024-US-0009-TK-0001-snapshots`, `EP-0024-US-0011-TK-0003-role-colours`, `EP-0024-US-0013-TK-0002-panel-move`, `EP-0024-US-0013-TK-0003-panel-fire`, `EP-0024-US-0013-TK-0004-panel-salvo` | US9-AC3 |
| [EP-0024-US-0010-TK-0001-docs](EP-0024-US-0010-project-documentation/EP-0024-US-0010-TK-0001-docs/EP-0024-US-0010-TK-0001-docs.md) | Документация режима и скина | docs | `EP-0024-US-0009-TK-0001-snapshots`, `EP-0024-US-0009-TK-0002-panel-snapshots` | US10-AC1, US10-AC2 |

## Контроль передачи

- **US-0001:** пакет `tabletop` в сборке; признак режима, листы кораблей и торпед, строки эффектов доступны.
- **US-0002:** детальные модели кораблей по типу и курсу, масштаб по окну, наведение; запекатель и генератор эффектов как инструменты.
- **US-0003:** плашки имени, брони и корпуса; рамки после залпа.
- **US-0004:** облака «краской» с кэшем.
- **US-0005:** движение лентами-шаблонами, зона, «призраки», подсказка.
- **US-0006:** стрельба лентами, угрозы, прицелы, значки поверх лент.
- **US-0007:** пиксельная торпеда со шлейфом и анимация взрыва, прежние отлетающие цифры, затем ленты итога, значки и маркеры.
- **US-0008:** камера по фазам.
- **US-0011:** окна и кнопки в воронёной стали с тёмными кромками; полосы и имя из ролей.
- **US-0012:** узкая колонка: карточки 150×40 с портретом, именем, бронёй, корпусом, оружием и кораблём.
- **US-0013:** панель команд снизу справа от выбранной карточки с отступом 30 % в движении, стрельбе, полёте и итоге.
- **US-0009:** снимки `tabletop` с полем и панелями, Classic без изменений.
- **US-0010:** документация и ExecutionStatus.
