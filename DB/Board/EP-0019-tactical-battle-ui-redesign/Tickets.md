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
|---|---|---|---|---| [EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity](EP-0019-US-0001-reference-and-golden-harness/EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity/EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity.md) | Сборка на SDK 8.0.4xx | client | [] | [US1-AC1] |
| [EP-0019-US-0001-TK-0002-bundle-battle-font](EP-0019-US-0001-reference-and-golden-harness/EP-0019-US-0001-TK-0002-bundle-battle-font/EP-0019-US-0001-TK-0002-bundle-battle-font.md) | Поставка шрифта DejaVu Sans и единая растеризация текста | client | [EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity] | [US1-AC2] |
| [EP-0019-US-0001-TK-0003-scene-harness](EP-0019-US-0001-reference-and-golden-harness/EP-0019-US-0001-TK-0003-scene-harness/EP-0019-US-0001-TK-0003-scene-harness.md) | Харнесс сцен и проверки вида | tests | [EP-0019-US-0001-TK-0002-bundle-battle-font] | [US1-AC3, US1-AC4, US1-AC5] |
| [EP-0019-US-0002-TK-0001-design-tokens-and-primitives](EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-TK-0001-design-tokens-and-primitives/EP-0019-US-0002-TK-0001-design-tokens-and-primitives.md) | Токены и примитивы макета | client | [EP-0019-US-0001-TK-0003-scene-harness] | [US2-AC1, US2-AC2] |
| [EP-0019-US-0002-TK-0002-full-screen-map-shell](EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-TK-0002-full-screen-map-shell/EP-0019-US-0002-TK-0002-full-screen-map-shell.md) | Карта на весь экран и удаление старой панели | client | [EP-0019-US-0002-TK-0001-design-tokens-and-primitives] | [US2-AC1, US2-AC3] |
| [EP-0019-US-0002-TK-0003-phase-camera-fit](EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-TK-0003-phase-camera-fit/EP-0019-US-0002-TK-0003-phase-camera-fit.md) | Камера по фазам | client | [EP-0019-US-0002-TK-0002-full-screen-map-shell] | [US2-AC4] |
| [EP-0019-US-0002-TK-0004-localization-strings](EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-TK-0004-localization-strings/EP-0019-US-0002-TK-0004-localization-strings.md) | Строки RU/EN нового интерфейса | content-data | [EP-0019-US-0002-TK-0002-full-screen-map-shell] | [US2-AC5] |
| [EP-0019-US-0003-TK-0001-pilot-portrait-data](EP-0019-US-0003-pilot-cards/EP-0019-US-0003-TK-0001-pilot-portrait-data/EP-0019-US-0003-TK-0001-pilot-portrait-data.md) | Портреты и пол пилотов | content-data | [EP-0019-US-0002-TK-0004-localization-strings] | [US3-AC4] |
| [EP-0019-US-0003-TK-0002-pilot-card-projection](EP-0019-US-0003-pilot-cards/EP-0019-US-0003-TK-0002-pilot-card-projection/EP-0019-US-0003-TK-0002-pilot-card-projection.md) | Данные карточки по фазам | client | [EP-0019-US-0003-TK-0001-pilot-portrait-data] | [US3-AC2, US3-AC3] |
| [EP-0019-US-0003-TK-0003-pilot-card-rendering-and-selection](EP-0019-US-0003-pilot-cards/EP-0019-US-0003-TK-0003-pilot-card-rendering-and-selection/EP-0019-US-0003-TK-0003-pilot-card-rendering-and-selection.md) | Отрисовка карточек и выбор кликом | client | [EP-0019-US-0003-TK-0002-pilot-card-projection] | [US3-AC1, US3-AC2, US3-AC5] |
| [EP-0019-US-0004-TK-0001-phase-panel-container-and-header](EP-0019-US-0004-phase-panel/EP-0019-US-0004-TK-0001-phase-panel-container-and-header/EP-0019-US-0004-TK-0001-phase-panel-container-and-header.md) | Контейнер панели и шапка | client | [EP-0019-US-0002-TK-0004-localization-strings] | [US4-AC1] |
| [EP-0019-US-0004-TK-0002-move-details-and-input](EP-0019-US-0004-phase-panel/EP-0019-US-0004-TK-0002-move-details-and-input/EP-0019-US-0004-TK-0002-move-details-and-input.md) | Детали движения и ввод | client | [EP-0019-US-0004-TK-0001-phase-panel-container-and-header] | [US4-AC2] |
| [EP-0019-US-0004-TK-0003-fire-details-and-target-click](EP-0019-US-0004-phase-panel/EP-0019-US-0004-TK-0003-fire-details-and-target-click/EP-0019-US-0004-TK-0003-fire-details-and-target-click.md) | Детали стрельбы и назначение цели | client | [EP-0019-US-0004-TK-0001-phase-panel-container-and-header] | [US4-AC3] |
| [EP-0019-US-0004-TK-0004-salvo-and-results-details](EP-0019-US-0004-phase-panel/EP-0019-US-0004-TK-0004-salvo-and-results-details/EP-0019-US-0004-TK-0004-salvo-and-results-details.md) | Детали залпа и результатов | client | [EP-0019-US-0004-TK-0001-phase-panel-container-and-header] | [US4-AC4] |
| [EP-0019-US-0004-TK-0005-outcome-details](EP-0019-US-0004-phase-panel/EP-0019-US-0004-TK-0005-outcome-details/EP-0019-US-0004-TK-0005-outcome-details.md) | Итог боя | client | [EP-0019-US-0004-TK-0004-salvo-and-results-details] | [US4-AC5] |
| [EP-0019-US-0005-TK-0001-markers-labels-and-edge](EP-0019-US-0005-map-presentation/EP-0019-US-0005-TK-0001-markers-labels-and-edge/EP-0019-US-0005-TK-0001-markers-labels-and-edge.md) | Маркеры, подписи и носители за краем | client | [EP-0019-US-0002-TK-0003-phase-camera-fit] | [US5-AC1, US5-AC2, US5-AC5] |
| [EP-0019-US-0005-TK-0002-movement-overlays](EP-0019-US-0005-map-presentation/EP-0019-US-0005-TK-0002-movement-overlays/EP-0019-US-0005-TK-0002-movement-overlays.md) | Слои фазы движения | client | [EP-0019-US-0005-TK-0001-markers-labels-and-edge] | [US5-AC3] |
| [EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays](EP-0019-US-0005-map-presentation/EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays/EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays.md) | Слои стрельбы, залпа и результатов | client | [EP-0019-US-0005-TK-0002-movement-overlays] | [US5-AC4] |
| [EP-0019-US-0006-TK-0001-hover-hit-test-and-card](EP-0019-US-0006-hover-and-acceptance/EP-0019-US-0006-TK-0001-hover-hit-test-and-card/EP-0019-US-0006-TK-0001-hover-hit-test-and-card.md) | Хит-тест наведения и карточка | client | [EP-0019-US-0003-TK-0003-pilot-card-rendering-and-selection, EP-0019-US-0004-TK-0005-outcome-details, EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays] | [US6-AC1, US6-AC3] |
| [EP-0019-US-0006-TK-0002-view-acceptance](EP-0019-US-0006-hover-and-acceptance/EP-0019-US-0006-TK-0002-view-acceptance/EP-0019-US-0006-TK-0002-view-acceptance.md) | Приёмка вида семи сцен | tests | [EP-0019-US-0006-TK-0001-hover-hit-test-and-card] | [US6-AC2] |
| [EP-0019-US-0007-TK-0001-update-contracts-and-docs](EP-0019-US-0007-project-documentation/EP-0019-US-0007-TK-0001-update-contracts-and-docs/EP-0019-US-0007-TK-0001-update-contracts-and-docs.md) | Итоговая документация | documentation | [EP-0019-US-0006-TK-0002-view-acceptance] | [US7-AC1, US7-AC2, US7-AC3] |
|

## Контроль передачи

- **US-0001:** сборка, шрифт, сцены и проверки готовы до первой правки экрана; проверки вида пока `Skip`.
- **US-0002:** каркас, токены, камера, строки — основа для US-0003–US-0005.
- **US-0003–US-0005:** каждый тикет включает проверки вида своей области (DesignSpec §12).
- **US-0006:** наведение, все проверки вида семи сцен и регрессионные снимки — главный критерий D-08.
- **US-0007:** документация и ExecutionStatus.

Блокирующие решения пользователя: Q-01–Q-04 в [Documentation.md](Documentation.md#открытые-вопросы-нужно-решение-до-утверждения).
