---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0006-hover-and-acceptance
title: Наведение и приёмка вида
stage: draft
dependencies: [EP-0019-US-0003-pilot-cards, EP-0019-US-0004-phase-panel, EP-0019-US-0005-map-presentation]
created: 2026-11-08T09:31:05Z
source_request: "Карточка «Под курсором» и уголки на карте при наведении на юнит; включение всех проверок вида семи сцен и сохранение регрессионных снимков."
current_review: epic
revision: 1
---

# Наведение и приёмка вида

## Входное техническое задание

D-03, D-08, D-09. [DesignSpec §7, §8.9, §12](../DesignSpec.md#7-карточка-под-курсором).

## User story

Как игрок, я хочу, наведя мышь на тага или своего пилота, видеть его данные под панелью фазы, а без наведения — карту. Как владелец продукта, я хочу подтверждения, что экран выглядит точно по спецификации.

## Dependencies

[EP-0019-US-0003-pilot-cards](../EP-0019-US-0003-pilot-cards/EP-0019-US-0003-pilot-cards.md), [EP-0019-US-0004-phase-panel](../EP-0019-US-0004-phase-panel/EP-0019-US-0004-phase-panel.md), [EP-0019-US-0005-map-presentation](../EP-0019-US-0005-map-presentation/EP-0019-US-0005-map-presentation.md)

## Completion evidence

- Наведение работает мышью на реальном экране.
- Все проверки вида семи сцен проходят без `Skip`; регрессионные снимки сохранены.

## Acceptance criteria

- **US6-AC1.** Наведение на дрон противника, свой дрон или носитель (радиус хита 12 ед. макета для дронов, 16 для носителей; ближайший) показывает карточку под панелью фазы и уголки цвета стороны; уход курсора убирает карточку; клик не нужен.
- **US6-AC2.** Проверки `Ep0019ViewTests` (раскладка, строки, пробы) проходят для `1-move`, `1-move-no-hover`, `2-fire`, `2-fire-hover-own`, `3-salvo-flight`, `4-salvo-results`, `5-outcome`; регрессионные снимки сохранены по DesignSpec §12.5; отчёты — в artifacts.
- **US6-AC3.** Наведение не меняет выбор, приказы и цели.

## Non-goals

Подсказки (tooltip) кнопок.

## Invariants

Наведение — состояние Client; команды не отправляются.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы-нужно-решение-до-утверждения)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0019-US-0006-TK-0001-hover-hit-test-and-card](EP-0019-US-0006-TK-0001-hover-hit-test-and-card/EP-0019-US-0006-TK-0001-hover-hit-test-and-card.md) | Хит-тест наведения и карточка | client | `EP-0019-US-0003-TK-0003-pilot-card-rendering-and-selection`, `EP-0019-US-0004-TK-0005-outcome-details`, `EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays` | US6-AC1, US6-AC3 |
| [EP-0019-US-0006-TK-0002-view-acceptance](EP-0019-US-0006-TK-0002-view-acceptance/EP-0019-US-0006-TK-0002-view-acceptance.md) | Приёмка вида семи сцен | tests | `EP-0019-US-0006-TK-0001-hover-hit-test-and-card` | US6-AC2 |
