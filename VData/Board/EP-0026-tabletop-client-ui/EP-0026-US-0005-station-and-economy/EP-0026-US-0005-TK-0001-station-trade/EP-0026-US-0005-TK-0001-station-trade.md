---
epic: EP-0026-tabletop-client-ui
story: EP-0026-US-0005-station-and-economy
ticket: EP-0026-US-0005-TK-0001-station-trade
title: Станция, торговля и финансы
stage: implemented
---

# Станция, торговля и финансы

## Основание и зависимости

[Родительская история](../EP-0026-US-0005-station-and-economy.md) определяет зависимости и пользовательский результат. [Эпик](../../Documentation.md) определяет визуальный контракт.

## Code context

Область реализации — UI/Screens/Station, Trade, Finance; UI/Controls/StationToolbar.

## Критерии приёмки

Оформить рынки, суммы и статусы. Проверить текущую котировку, недоступную сделку и возврат из вложенных окон. Проверить применимые состояния из родительской истории.

## Проверка

Зафиксировать фактические проверки, review и открытые условия приёмки перед отдельным коммитом.


## Уточнение области

TradePainter, StationScreen, FinanceScreen и StationToolbar получают общие плиты, поля, состояния и семантические цвета. Растровый тест Finance проверяет новый фон в production-рендере.


## Результат

Станция, торговля, финансы, панель ресурсов и списки используют стальную палитру, золотой выбор и цвета статусов. Код котировок и проводок не менялся. Release и 211/211 целевых проверок TradeUx, FinanceScreen, StationToolbar, StationScreen и StationRoutePresentation прошли. Отчёт D:/DeepSpaceSaga/artifacts/validation/ep0026-tabletop-client-ui/economy-ui-20261113-140112/. git diff --check прошёл. Scoped formatter выявил существующие отступы вне изменённых участков. Сквозной нативный маршрут ведётся в US-0009.
