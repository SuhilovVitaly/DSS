---
epic: EP-0026-tabletop-client-ui
story: EP-0026-US-0004-ship-and-flight
ticket: EP-0026-US-0004-TK-0001-flight-panels
title: Панели корабля и полёта
stage: implemented
---

# Панели корабля и полёта

## Основание и зависимости

[Родительская история](../EP-0026-US-0004-ship-and-flight.md) определяет зависимости и пользовательский результат. [Эпик](../../Documentation.md) определяет визуальный контракт.

## Code context

Область реализации — UI/Screens/Ship, UI/Screens/GameSession/Controls и панели манёвра.

## Критерии приёмки

Оформить диаграмму и параметры полёта с различимыми состояниями, проверить команды и отказ. Проверить применимые состояния из родительской истории.

## Проверка

Зафиксировать фактические проверки, review и открытые условия приёмки перед отдельным коммитом.


## Уточнение области

ShipScreen, CommandsPanel, ObjectInfoPanel и обвязка GameSession используют общий отрисовщик. Тест ShipScreen проверяет реальное покрытие плитой вместо загрузки фонового PNG.


## Результат

Ship, HUD, блоки модулей, статусы и сведения объекта используют стальные плиты и утопленные поля. Геометрия прежних кнопок и данные снимков проверены. Release и 166/166 проверок ShipScreen, CommandsPanel, ObjectInfoPanel, ManualAxes, ManualManeuver, MechanicsPanel, ModuleDiagram и WeaponHudControls прошли. Scoped whitespace и git diff --check прошли. Отчёт D:/DeepSpaceSaga/artifacts/validation/ep0026-tabletop-client-ui/flight-final-20261113-135916/. Сквозная нативная приёмка остаётся в US-0009.
