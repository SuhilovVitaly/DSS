---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0005-map-presentation
title: Карта в новом стиле
stage: done
dependencies: [EP-0019-US-0002-overlay-layout-shell]
created: 2026-11-08T09:31:05Z
source_request: "Перерисовать содержимое карты по DesignSpec §8: маркеры, подписи с выносками, носители за краем, прогнозы пути, секторы и стрелки огня, снаряд, выноски результатов."
current_review: epic
revision: 1
---

# Карта в новом стиле

## Входное техническое задание

A-09, A-11. [DesignSpec §8](../DesignSpec.md#8-карта). Текущий рендер — `TacticalBattleMapRenderer.cs`.

## User story

Как игрок, я хочу видеть на карте только то, что нужно в текущей фазе, с читаемыми подписями, чтобы быстро оценивать позицию.

## Dependencies

[EP-0019-US-0002-overlay-layout-shell](../EP-0019-US-0002-overlay-layout-shell/EP-0019-US-0002-overlay-layout-shell.md)

## Completion evidence

- Карта во всех сценах соответствует DesignSpec §8 и проходит пробы §12.4.
- Слои, которых нет в DesignSpec §8, не рисуются.

## Acceptance criteria

- **US5-AC1.** Маркеры, кольцо выбора, подписи с полосами и раскладкой (порядок, стороны, отступы, выноски, занятые прямоугольники панелей) соответствуют DesignSpec §8.2–8.3.
- **US5-AC2.** Носитель вне камеры рисуется у края безопасной области со стрелкой и расстоянием в у.е.
- **US5-AC3.** Движение: прогнозы всех своих дронов по текущим приказам (`TacticalGeometry.Path`), выделение выбранного, квадраты концов, «Конец пути»; перетаскивание конца пути работает.
- **US5-AC4.** Стрельба, залп и результаты: следы, сектор выбранного стрелка, стрелки с шансом, снаряд по `TacticalBattleResults`, выноски урона и кольца пострадавших.
- **US5-AC5.** Прогноз и кормовые секторы противника и «все секторы огня» не рисуются.

## Non-goals

Камера (US-0002), наведение (US-0006).

## Invariants

Рендер читает только кадр; `VisibleUnits` и допуск обломков по `LethalImpact` сохраняются.

## Решения и согласование

Исполнение draft-карточек разрешено запросом выполнить весь эпик. Использованы записанные варианты по умолчанию; художественное/пользовательское одобрение не приписывается. Итог — [ExecutionStatus](../ExecutionStatus.md).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0019-US-0005-TK-0001-markers-labels-and-edge](EP-0019-US-0005-TK-0001-markers-labels-and-edge/EP-0019-US-0005-TK-0001-markers-labels-and-edge.md) | Маркеры, подписи и носители за краем | client | `EP-0019-US-0002-TK-0003-phase-camera-fit` | US5-AC1, US5-AC2, US5-AC5 |
| [EP-0019-US-0005-TK-0002-movement-overlays](EP-0019-US-0005-TK-0002-movement-overlays/EP-0019-US-0005-TK-0002-movement-overlays.md) | Слои фазы движения | client | `EP-0019-US-0005-TK-0001-markers-labels-and-edge` | US5-AC3 |
| [EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays](EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays/EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays.md) | Слои стрельбы, залпа и результатов | client | `EP-0019-US-0005-TK-0002-movement-overlays` | US5-AC4 |

## Итоговая проверка

Все тикеты истории реализованы и опубликованы. Критерии сопоставлены проверкам в [ExecutionStatus](../ExecutionStatus.md); текущие результаты заменяют начальную запись blocked. Human/physical input и проверки недоступных ОС остаются отдельными gates.
