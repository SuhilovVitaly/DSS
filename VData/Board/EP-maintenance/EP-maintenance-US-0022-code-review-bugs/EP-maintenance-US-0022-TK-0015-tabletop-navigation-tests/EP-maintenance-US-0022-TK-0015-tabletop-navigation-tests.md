---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0015-tabletop-navigation-tests
title: Навигационные тесты боя не проходят по скину Tabletop
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC15]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Навигационные тесты боя не проходят по скину Tabletop

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Ключевые маршруты боя (настройка, отмена, движение, залп, повтор, итог, возврат) автоматически проверяются и для скина по умолчанию нового профиля (Tabletop), и для Classic.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0007-TK-0001-battle-transition. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Каждый `Fixture` без явного пути настроек пишет `tacticalBattleSkinId: "classic"`. По G-01/A-02 и Settings.md новый профиль открывает Tabletop, поэтому путь, который получает игрок, автоматическими тестами маршрутов не покрыт; полный прогон Tabletop в тикете зависал и был остановлен.

**Сценарий.** Регрессия маршрута боя, проявляющаяся только в Tabletop, проходит все `BattleNavigationTests`.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs:334-340` | Фикстура с принудительным Classic. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Параметризовать ключевые маршрутные тесты по Classic и профилю по умолчанию (skin null → Tabletop); разобрать причину зависания полного прогона Tabletop.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`BattleNavigationTests` в виде `[Theory]` по двум скинам проходят в пределах лимита времени.

## Definition of Done

- AC15 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
