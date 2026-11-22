---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0006-results-recovery
ticket: EP-0042-US-0006-TK-0004-close-retry-gate
title: Блокировка смены мира при повторе записи выхода
stage: implemented
layer: client
depends_on: [EP-0042-US-0006-TK-0003-load-routing]
serves: [AC-01, AC-03]
---

# Блокировка смены мира при повторе записи выхода

При сбое записи закрытия окна без добычи F9 мог сменить сессию, пока receipt оставался недолговечным. Диспетчер и быстрые клавиши проверяют pending acknowledge. Кнопка выхода по-прежнему повторяет тот же запрос. Проверка `BattleResultsLoad_BlockNavigationAfterFailedAcknowledgement` использует timeout собственного боя и внедрённый отказ атомарной записи.

## Исполнение

- Исправлен обход через F9 после сбоя записи закрытия результатов без лута. Мир и окно остаются в ожидании того же durable checkpoint. Кнопка выхода может повторить запись.
- Сборка Client Tests, scoped whitespace format и `git diff --check` прошли. Повтор US-0006 запускается после публикации.
