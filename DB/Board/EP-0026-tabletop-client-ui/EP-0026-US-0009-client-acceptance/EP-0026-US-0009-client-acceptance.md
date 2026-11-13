---
epic: EP-0026-tabletop-client-ui
story: EP-0026-US-0009-client-acceptance
title: Сквозная приёмка интерфейса
stage: draft
dependencies: [EP-0026-US-0002-menus-and-sessions, EP-0026-US-0003-world-map, EP-0026-US-0004-ship-and-flight, EP-0026-US-0005-station-and-economy, EP-0026-US-0006-crew-and-dialogue, EP-0026-US-0007-tactical-flow, EP-0026-US-0008-developer-ui]
---

# Сквозная приёмка интерфейса

## Связь с техническим заданием

Полный переход подтверждается по всем доступным точкам входа и состояниям интерфейса. [EP-0026](../Documentation.md#исходное-техническое-задание).

## User story

Как владелец продукта, я могу пройти весь клиент и сверить покрытие с перечнем экранов. На каждом маршруте Tabletop читаем, ввод соответствует видимым областям, а переходы работают при поддерживаемом масштабе и на целевых конфигурациях.

## Зависимости

- [EP-0026-US-0002-menus-and-sessions](../EP-0026-US-0002-menus-and-sessions/EP-0026-US-0002-menus-and-sessions.md) — Меню, настройки и сохранения.
- [EP-0026-US-0003-world-map](../EP-0026-US-0003-world-map/EP-0026-US-0003-world-map.md) — Карта мира и навигация.
- [EP-0026-US-0004-ship-and-flight](../EP-0026-US-0004-ship-and-flight/EP-0026-US-0004-ship-and-flight.md) — Корабль, модули и управление полётом.
- [EP-0026-US-0005-station-and-economy](../EP-0026-US-0005-station-and-economy/EP-0026-US-0005-station-and-economy.md) — Станции, торговля и финансы.
- [EP-0026-US-0006-crew-and-dialogue](../EP-0026-US-0006-crew-and-dialogue/EP-0026-US-0006-crew-and-dialogue.md) — Диалоги, экипаж и контракты.
- [EP-0026-US-0007-tactical-flow](../EP-0026-US-0007-tactical-flow/EP-0026-US-0007-tactical-flow.md) — Подготовка, бой, итог и возврат.
- [EP-0026-US-0008-developer-ui](../EP-0026-US-0008-developer-ui/EP-0026-US-0008-developer-ui.md) — Отладочные экраны и мастерская портретов.

## Completion evidence

Перечень экранов сопоставлен с кадрами и результатами сквозных проверок. Покрыты крайние UI scale, длинные строки доступных локалей, узкое окно, ошибки, возврат, старые настройки и загрузка сохранения. На согласованных сценах представлены сравнимые замеры отрисовки и повторные входы без накопления ресурсов. Владелец сверяет внешний вид по эталонам.

## Ограничения и gaps

G-02 и G-03 фиксируют, какие эталоны и конфигурации нужно согласовать. Автоматические, нативные и продуктовые результаты фиксируются отдельно.

## Тикеты

- [EP-0026-US-0009-TK-0001-route-validation](EP-0026-US-0009-TK-0001-route-validation/EP-0026-US-0009-TK-0001-route-validation.md)
