---
epic: EP-maintenance
story: EP-maintenance-US-0010-tactical-readability-and-results
stage: approved
implementation_status: in_progress
created: "2026-11-10T14:03:31.830Z"
---

# Читаемость карты и результаты залпа

[Эпик «Исправления и доделки»](../Documentation.md)

## Входное задание

Продолжение доработки тактического боя по запросу пользователя. Приложенный кадр показывает кресты сбитых юнитов и текущий размер подписей.

## User story

Как игрок, я хочу легко читать манёвр и результаты огня.

## Acceptance criteria

- **AC1.** Область достижимости выбранного дрона видна при удержании левой кнопки на карте для задания точки или перетаскивания конца маршрута. Отпускание, отмена и смена фазы скрывают область. Прогноз стрельбы остаётся привязан к выбору.
- **AC2.** В начале результатов залпа над целью всплывает фактически нанесённый урон или локализованное «Промах». Текст поднимается и затухает. Поглощение отличается от обычного промаха.
- **AC3.** Уменьшить силуэты, подписи и обе шкалы на карте примерно на 20%. Размер карточек и область клика сохраняются.
- **AC4.** На месте сбитого дрона или Тага остаются крест и серая подпись с именем. Подписи избегают перекрытия соседних маркеров и не раскрывают будущие уничтожения.
- **AC5.** В результатах залпа справа внизу отображается прокручиваемый список выполненных выстрелов обеих сторон в порядке залпа. Наведение на дрон или Таг временно заменяет список карточкой этого юнита.

## Dependencies

Продолжает [US-0009](../Done/EP-maintenance-US-0009-tactical-map-feedback/EP-maintenance-US-0009-tactical-map-feedback.md). Тикеты можно проверять последовательно независимо.

## Approved ticket map

- [EP-maintenance-US-0010-TK-0001-held-movement-zone](../Done/EP-maintenance-US-0010-tactical-readability-and-results/EP-maintenance-US-0010-TK-0001-held-movement-zone/EP-maintenance-US-0010-TK-0001-held-movement-zone.md) — AC1, выполнен
- [EP-maintenance-US-0010-TK-0002-floating-shot-feedback](../Done/EP-maintenance-US-0010-tactical-readability-and-results/EP-maintenance-US-0010-TK-0002-floating-shot-feedback/EP-maintenance-US-0010-TK-0002-floating-shot-feedback.md) — AC2, выполнен
- EP-maintenance-US-0010-TK-0003-compact-unit-markers — AC3
- EP-maintenance-US-0010-TK-0004-wreck-names — AC4
- EP-maintenance-US-0010-TK-0005-round-shot-report — AC5

## Review log

2026-11-10T14:03:31.830Z — workflow epic-writer. Запрос пользователя включает реализацию, отдельные коммиты и публикацию.

## Gaps and backlog

Ручная приёмка в запущенной игре отделена от автоматической проверки.
