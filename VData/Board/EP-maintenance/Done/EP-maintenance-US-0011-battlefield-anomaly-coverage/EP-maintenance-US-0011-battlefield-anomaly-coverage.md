---
epic: EP-maintenance
story: EP-maintenance-US-0011-battlefield-anomaly-coverage
stage: approved
implementation_status: implemented
created: "2026-11-10T14:03:31.830Z"
---

# Покрытие поля боя аномалиями

[Эпик «Исправления и доделки»](../../Documentation.md)

## Входное задание

Продолжение доработки тактического боя по запросу пользователя. Приложенный кадр показывает кресты сбитых юнитов и текущий размер подписей.

## User story

Как игрок, я хочу встречать аномалии по всему полю боя.

## Acceptance criteria

- **AC1.** Расширить генерацию за пределы центральной полосы. Сохранить размер и состав сгустков и безопасный отступ от стартовых позиций. Добавить периферийные области с сопоставимым шагом; генерация по seed остаётся детерминированной.

## Dependencies

Продолжает [US-0009](../EP-maintenance-US-0009-tactical-map-feedback/EP-maintenance-US-0009-tactical-map-feedback.md). Тикеты можно проверять последовательно независимо.

## Approved ticket map

- [EP-maintenance-US-0011-TK-0001-peripheral-anomalies](./EP-maintenance-US-0011-TK-0001-peripheral-anomalies/EP-maintenance-US-0011-TK-0001-peripheral-anomalies.md) — AC1, выполнен

## Review log

2026-11-10T14:03:31.830Z — workflow epic-writer. Запрос пользователя включает реализацию, отдельные коммиты и публикацию.

## Gaps and backlog

Ручная приёмка в запущенной игре отделена от автоматической проверки.
