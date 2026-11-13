---
epic: EP-maintenance
story: EP-maintenance-US-0018-tactical-battle-hud
title: Интерфейс пошагового боя
stage: approved
approval_kind: workflow
implementation_status: done
dependencies: [EP-maintenance-US-0017-tactical-minimap]
created: 2026-11-13T14:20:13.611Z
current_review: complete
revision: 1
---

# Интерфейс пошагового боя

[Эпик «Исправления и доделки»](../../Documentation.md)

## Входное техническое задание

Изменить масштабирование линий движения, заменить индикаторы HP в компактной карточке и вывести журнал вместо «Пилот и сборка». Перенос миникарты ведётся в US-0017.

## User story

В пошаговом бою игрок видит траектории при любом масштабе, различает оставшиеся HP и может прокрутить историю действий обеих сторон.

## Acceptance criteria

- AC1. С уменьшением масштаба траектория утолщается. При приближении линии плана и следа становятся тоньше в Classic и Tabletop.
- AC2. В компактной карточке пилота два горизонтальных индикатора с иконками показывают доли текущих HP корпуса и брони.
- AC3. Журнал поддерживает новый вид события перемещения с сохранением чисел существующих видов.
- AC4. После принятого движения в журнал записывается по одному событию на выполненный путь до событий автоматического огня.
- AC5. Справа отображается прокручиваемый журнал за весь бой. Выстрелы, повреждения и гибель появляются по мере проигрывания залпа. Карточка сборки убрана из экрана.

## Non-goals

Баланс, расчёт движения и урона, сохранения мира.

## Dependencies

US-0017 задаёт верхнюю границу правой колонки. Журнал получает события из контракта и движка.

## Approved ticket map

- [Толщина траекторий](EP-maintenance-US-0018-TK-0001-movement-stroke/EP-maintenance-US-0018-TK-0001-movement-stroke.md), AC1, client.
- [Шкалы корпуса и брони](EP-maintenance-US-0018-TK-0002-pilot-health-bars/EP-maintenance-US-0018-TK-0002-pilot-health-bars.md), AC2, client.
- [Контракт события перемещения](EP-maintenance-US-0018-TK-0003-movement-event/EP-maintenance-US-0018-TK-0003-movement-event.md), AC3, contracts.
- [Запись выполненных перемещений](EP-maintenance-US-0018-TK-0004-record-movement/EP-maintenance-US-0018-TK-0004-record-movement.md), AC4, engine, после EP-maintenance-US-0018-TK-0003-movement-event.
- [Панель журнала боя](EP-maintenance-US-0018-TK-0005-battle-log/EP-maintenance-US-0018-TK-0005-battle-log.md), AC5, client, после EP-maintenance-US-0018-TK-0004-record-movement.

## Gaps and backlog

Ручная приёмка в игре остаётся отдельной проверкой.

## Проверки

Release-сборка клиента и проверка форматирования изменённых C# файлов прошли. Прошли 199 проверок Contracts и 581 проверка тактического движка. В клиентском наборе прошли 570 проверок, включая сравнение 23 растровых эталонов. Проверка seed 1 прошла отдельно после обновления тестовых данных. Ручной запуск игры не выполнялся.

Результаты: D:/DeepSpaceSaga/artifacts/validation/tactical-battle-hud/20261113T142152264Z.
