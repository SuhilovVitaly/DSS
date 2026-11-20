---
epic: EP-maintenance
story: EP-maintenance-US-0025-debug-kill-all
title: Быстрый переход к сбору трофеев через debug-консоль
stage: approved
implementation_status: implemented
dependencies: []
revision: 1
---
# Быстрый переход к сбору трофеев через debug-консоль

Как разработчик, я ввожу `kill all` в начавшемся пиратском бою, чтобы сразу проверить [сбор трофеев](../../EP-0036-demo-bounties-and-loot/EP-0036-US-0007-postbattle-loot-lifecycle/EP-0036-US-0007-postbattle-loot-lifecycle.md).

## Критерии

- Команда публикуется в `help` и работает в текущем бою.
- Вражеские носители уничтожены. Активные вражеские дроны случайно получают один из двух исходов, а при двух или более активных дронах присутствуют оба исхода. Союзники сохраняют своё состояние.
- Победа через `kill all` в пиратской встрече и в «Начальном бою» подготавливает трофеи. В потоке EP-0042 они доступны в окне результатов боя.
- Проверка победы, учёт результатов и сохранение используют существующий путь.

## Тикеты

- [EP-maintenance-US-0025-TK-0001-enemy-side-request](EP-maintenance-US-0025-TK-0001-enemy-side-request/EP-maintenance-US-0025-TK-0001-enemy-side-request.md) (contracts)
- [EP-maintenance-US-0025-TK-0002-resolve-enemy-side](EP-maintenance-US-0025-TK-0002-resolve-enemy-side/EP-maintenance-US-0025-TK-0002-resolve-enemy-side.md) (engine)
- [EP-maintenance-US-0025-TK-0003-console-command](EP-maintenance-US-0025-TK-0003-console-command/EP-maintenance-US-0025-TK-0003-console-command.md) (client)
- [EP-maintenance-US-0025-TK-0004-debug-sortie-prisoner](EP-maintenance-US-0025-TK-0004-debug-sortie-prisoner/EP-maintenance-US-0025-TK-0004-debug-sortie-prisoner.md) (engine)

## Проверки

Начальный headless-прогон подтвердил победу, отключение дронов и открытие фазы лута. Проверка окна запрашивала его до обработки ввода. Тест теперь проверяет выбор трофея кликом по карте, повтор не запускался по указанию пользователя. В смежных наборах остались два расхождения старых ожиданий с самоуничтожением носителя и значением события 18. Нативная проверка не проводилась.

Логи: D:/DeepSpaceSaga/artifacts/validation/debug-kill-all/20261120-114538-167/ и D:/DeepSpaceSaga/artifacts/validation/debug-kill-all/20261120-114617-196/.

## Доработка по ручной проверке

По результатам проверки пользователем добавлено случайное распределение исходов в отдельном потоке RNG. Лут в «Начальном бою» использует собственное сохранение результатов вылета и не создаёт фиктивную встречу. Для уничтоженного тэга доступен лом по норме лёгкого корпуса, а у отключённого доступны расходники, пилот и только компоненты, для которых уже определён трофей.

Повторные тесты и запуск игры не выполнялись по текущему указанию пользователя. Лог сборки клиента: D:/DeepSpaceSaga/artifacts/validation/debug-kill-all/20261120-115502-713-build/build.log.
