---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0013-battle-metrics-export
title: Метрики боя копятся в профиле пользователя, тест ничего не проверяет
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC13]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Метрики боя копятся в профиле пользователя, тест ничего не проверяет

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Экспорт метрик боя не растёт без ограничений в профиле игрока, тесты Client пишут только во временный каталог, тест отчётов проверяет их содержимое.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-maintenance-US-0019-TK-0003 (вошло в merge #113). Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Каждый принятый ответ боя в продакшне пишет форматированный JSON `%LOCALAPPDATA%/DeepSpaceSaga/BattleMetrics/<sha>.json` (если не задан `DSS_VALIDATION_ROOT`) с полным `ResolvedShot` на выстрел; файлы никогда не удаляются. Тесты Client не задают `DSS_VALIDATION_ROOT` и пишут в реальный профиль. `HeadlessDefaultTargetBattlesProduceComparableAccuracyReports` пишет 24 отчёта и ничего о них не утверждает.

**Сценарий.** Каждый сыгранный бой и каждый полный прогон тестов Client добавляет файлы в профиль без предела.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMetrics.cs:17-20` | Путь экспорта. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs:94, 101, 109` | Вызовы экспорта. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMetricsTests.cs:18, 40-61` | Тест без утверждений. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Сделать экспорт включаемым (отладка/проверка) или ограничить число файлов с ротацией. Тесты направить во временный каталог и проверять содержимое отчётов.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`BattleMetricsTests`: отчёты пишутся во временный каталог и содержат ожидаемые поля; без флага продакшн-экспорт не пишет (или соблюдает предел).

## Definition of Done

- AC13 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
