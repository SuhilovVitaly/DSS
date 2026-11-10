---
epic: EP-maintenance
story: EP-maintenance-US-0009-tactical-map-feedback
title: Обратная связь на тактической карте
stage: approved
implementation_status: in_progress
created: "2026-11-10T12:45:42.415Z"
revision: 1
---

# Обратная связь на тактической карте

[Эпик «Исправления и доделки»](../Documentation.md)

## Входное задание

Пять исправлений тактической карты по запросу пользователя от 10 ноября 2026 года. Скриншоты служат примерами поведения. Пользователь поручил создать и выполнить отдельный тикет, закоммитить и опубликовать каждое изменение.

## User story

Как игрок, я хочу видеть направление движения, пройденные маршруты и доступные цели и сохранять положение карты между фазами, чтобы планировать манёвр и огонь.

## Acceptance criteria

- **AC1.** Небольшой треугольник в плановой конечной точке ориентирован по конечному курсу. Выбранный маркер выделен, конечные точки по-прежнему перетаскиваются.
- **AC2.** Щелчок по выбранному дрону на карте снимает выбор и скрывает обе зоны. Повторный выбор показывает зоны этого дрона. Готовые приказы и плановые линии сохраняются.
- **AC3.** Одна начальная подгонка на новый бой. Execute movement, анимация, фазы, ходы и изменение размера сохраняют центр и масштаб. В текущем бою камера меняется колесом или перетягиванием.
- **AC4.** Тонкая непрерывная линия показывает реально пройденный путь дронов обеих сторон за два последних движения. Во время анимации линия заканчивается в текущей позе и не показывает будущие участки.
- **AC5.** Отметить на карте все цели выбранного дрона по текущему снимку. Подсветить назначенную цель и дать отличимый маркер и шанс остальным. Сектор соответствует половинному углу оружия и дальности. Приказ по-прежнему содержит одну цель.

## Dependencies

Связано с [предыдущей доработкой движения](../Done/EP-maintenance-US-0006-tactical-movement-readability/EP-maintenance-US-0006-tactical-movement-readability.md). Пять тикетов независимы.

## Approved ticket map

- [EP-maintenance-US-0009-TK-0001-directional-endpoints](../Done/EP-maintenance-US-0009-tactical-map-feedback/EP-maintenance-US-0009-TK-0001-directional-endpoints/EP-maintenance-US-0009-TK-0001-directional-endpoints.md) — AC1, Client, выполнен
- [EP-maintenance-US-0009-TK-0002-selection-only-zones](../Done/EP-maintenance-US-0009-tactical-map-feedback/EP-maintenance-US-0009-TK-0002-selection-only-zones/EP-maintenance-US-0009-TK-0002-selection-only-zones.md) — AC2, Client, выполнен
- [EP-maintenance-US-0009-TK-0003-stable-battle-camera](../Done/EP-maintenance-US-0009-tactical-map-feedback/EP-maintenance-US-0009-TK-0003-stable-battle-camera/EP-maintenance-US-0009-TK-0003-stable-battle-camera.md) — AC3, Client, выполнен
- [EP-maintenance-US-0009-TK-0004-two-turn-trails](../Done/EP-maintenance-US-0009-tactical-map-feedback/EP-maintenance-US-0009-TK-0004-two-turn-trails/EP-maintenance-US-0009-TK-0004-two-turn-trails.md) — AC4, Client, выполнен
- EP-maintenance-US-0009-TK-0005-show-all-fire-candidates — AC5, Client, ожидает публикации

## Review log

2026-11-10T12:45:42.415Z — карта подготовлена по workflow epic-writer. Запрос пользователя включает реализацию и публикацию.

## Gaps and backlog

Ручная проверка взаимодействия и внешнего вида в запущенной игре отделена от автоматических проверок.
