---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0010-project-documentation
title: Документация поля «Tabletop»
stage: draft
dependencies: [EP-0024-US-0009]
created: 2026-11-12T13:00:00Z
source_request: "Описать скин `tabletop`, режим поля и новые роли в документации экрана боя и скинов; завести ExecutionStatus эпика."
current_review: epic
revision: 1
---

# Документация поля «Tabletop»

## Входное техническое задание

Документы: `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md`, `DB/Documentation/04-Engineering/TacticalBattle.md`, `DB/Documentation/04-Engineering/TacticalBattleSkins.md`, `src/DeepSpaceSaga.Client/Skins/README.md`.

## User story

Как разработчик, я хочу, чтобы документация описывала режим поля и роли спрайтов, чтобы следующие пакеты могли ими пользоваться.

## Dependencies

[EP-0024-US-0009](../EP-0024-US-0009-view-regression/EP-0024-US-0009-view-regression.md)

## Completion evidence

- Документы описывают режим и скин; ExecutionStatus заведён.

## Acceptance criteria

- **US10-AC1.** Экран боя описывает вид поля в скине «Tabletop» (модели, плашки, облака, ленты, прицелы, итог залпа, камера); инженерный документ — режим, слои и кэш облаков; контракт скинов — роли `field.style`, `field.unit.sprite.*` (корабли и торпеды) и `field.fx.*` (взрывы, шлейф), перезапекание листов; README скинов — пакет `tabletop`. Экран боя и контракт скинов описывают также панели в стальной теме: узкую колонку, панель команд, роли `ui.style`, `ui.pilot-column`, `ui.panel.bevel`, `ui.bar.*`, `ui.build.*` (D-11).
- **US10-AC2.** `ExecutionStatus.md` содержит статус каждого тикета, пути artifacts, время пересборки облаков, список снимков `tabletop`; ссылки проверены.

## Non-goals

Несвязанные с итоговым diff документы. История выполняется последней, после
всех функциональных историй и регрессии; её scope определяется фактическим
влиянием реализации, а не исходным списком четырёх документов.

## Дополнение исполнения: раздел 6 EpicExecutionPrompt

- **US10-AC3.** Инвентаризировать влияние итогового diff на документы всего
  репозитория: текущие требования, архитектуру, экран, конфигурацию, README,
  Board, тестовые инструменты и asset-workflow. Для каждого документа записать
  необходимое изменение либо проверенное отсутствие влияния.
- **US10-AC4.** Синхронизировать зависимости, coverage и ticket-to-SHA, проверить
  ссылки и ID, сохранить исторические результаты как историю. Перестроить
  используемый Graphify после итогового кода и текстов, записать его ограничения
  и реальные пути evidence. При поздних исправлениях повторить затронутую
  синхронизацию. Не объявлять native/product gates закрытыми по unit-тестам.

## Invariants

Документы описывают только реализованное.

## Решения и согласование

Решения пользователя — [D-01–D-11](../Documentation.md#решения-пользователя), допущения — [A-01–A-19](../Documentation.md#технические-допущения). Эпик утверждён пользователем 2026-11-12 ([review log](../Documentation.md#review-log)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0024-US-0010-TK-0001-docs](EP-0024-US-0010-TK-0001-docs/EP-0024-US-0010-TK-0001-docs.md) | Документация режима и скина | docs | `EP-0024-US-0009-TK-0001-snapshots`, `EP-0024-US-0009-TK-0002-panel-snapshots` | US10-AC1, US10-AC2 |
