---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0012-build-card-ui
title: Карточка «Пилот и сборка»
stage: done
dependencies: [EP-0020-US-0011-build-slots, EP-0020-US-0010-anomaly-ui]
created: 2026-11-08T11:46:28Z
source_request: "Для своих дронов заменить карточку «Под курсором» карточкой «Пилот и сборка» со схемой рамы (силуэт по раме, слоты-ячейки с модулями и числами, повреждения) и итогами массы и движения — точно по [UiSpec](../UiSpec.md) §22–§26."
current_review: epic
revision: 1
---

# Карточка «Пилот и сборка»

## Входное техническое задание

D-34–D-42. Вид задаёт только [UiSpec](../UiSpec.md) §22–§26.

## User story

Как игрок, я хочу при выборе своего дрона или наведении на него видеть справа структуру его рамы и модули, чтобы понимать, чем он силён и что в нём повреждено.

## Dependencies

[EP-0020-US-0011-build-slots](../EP-0020-US-0011-build-slots/EP-0020-US-0011-build-slots.md), [EP-0020-US-0010-anomaly-ui](../EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-anomaly-ui.md)

## Completion evidence

- Карточка во всех фазах для выбранного своего дрона; при наведении на другого своего — его; противник, носитель и аномалия — прежние карточки на время наведения.
- Схема строится только из `Slots` снимка.
- Проверки вида EP-0019 и пробы UiSpec §25 зелёные.

## Acceptance criteria

- **US12-AC1.** Модель карточки: показываемый дрон по правилу A-35; ячейки из `Slots` с текстами §23; итоги из `Limits` и `Build`; обрезка имён многоточием по ширине (§23.3).
- **US12-AC2.** Отрисовка по §22–§24: место, шапка, силуэт рамы по `FrameId`, ячейки, пустые и отсутствующие слоты, красные ячейки повреждений, итоги и легенда; правая колонка занята для подписей карты на высоту карточки.
- **US12-AC3.** Проверки вида EP-0019 обновлены по US-0012 TK-0003; пробы UiSpec §25 проходят.

## Non-goals

Редактирование сборки, подсказки по клику, сборка противника.

## Invariants

Карточка только показывает данные снимка; приказы и выбор не меняются.

## Решения и согласование

Исполнение всего эпика разрешено текущим поручением пользователя.

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0012-TK-0001-build-card-model](EP-0020-US-0012-TK-0001-build-card-model/EP-0020-US-0012-TK-0001-build-card-model.md) | Модель карточки «Пилот и сборка» | client | `EP-0020-US-0011-TK-0002-slot-snapshot`, `EP-0020-US-0010-TK-0004-anomaly-view-regression` | US12-AC1 |
| [EP-0020-US-0012-TK-0002-build-card-render](EP-0020-US-0012-TK-0002-build-card-render/EP-0020-US-0012-TK-0002-build-card-render.md) | Отрисовка карточки и силуэтов рам | client | `EP-0020-US-0012-TK-0001-build-card-model` | US12-AC2 |
| [EP-0020-US-0012-TK-0003-build-card-view-regression](EP-0020-US-0012-TK-0003-build-card-view-regression/EP-0020-US-0012-TK-0003-build-card-view-regression.md) | Проверки вида EP-0019 после карточки сборки | tests | `EP-0020-US-0012-TK-0002-build-card-render` | US12-AC3 |

## Исполнение

US12-AC1:8model tests, A35/captured last selection, RU/EN. US12-AC2:7raster/geometry tests и literal UiSpec probes, unknown-frame fallback. US12-AC3: Client2252PASS,29originalEP19PASS,15strictsnapshots different0,12M3oldpanels exact. Engine не вызывается рисованием; input блокируется всей карточкой300design units. Self-review выполнен; native/global gates фиксируются в ExecutionStatus отдельно. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us12-tk3/view-audit.json`.
