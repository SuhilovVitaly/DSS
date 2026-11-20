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
- Вражеские носители уничтожены, уцелевшие вражеские дроны отключены, союзники сохраняют своё состояние.
- В пиратской встрече штатное завершение боя сразу открывает фазу лута.
- Проверка победы, учёт результатов и сохранение используют существующий путь.

## Тикеты

- [EP-maintenance-US-0025-TK-0001-enemy-side-request](EP-maintenance-US-0025-TK-0001-enemy-side-request/EP-maintenance-US-0025-TK-0001-enemy-side-request.md) (contracts)
- [EP-maintenance-US-0025-TK-0002-resolve-enemy-side](EP-maintenance-US-0025-TK-0002-resolve-enemy-side/EP-maintenance-US-0025-TK-0002-resolve-enemy-side.md) (engine)
- [EP-maintenance-US-0025-TK-0003-console-command](EP-maintenance-US-0025-TK-0003-console-command/EP-maintenance-US-0025-TK-0003-console-command.md) (client)

## Проверки

Начальный headless-прогон подтвердил победу, отключение дронов и открытие фазы лута. Проверка окна запрашивала его до обработки ввода. Тест теперь проверяет выбор трофея кликом по карте, повтор не запускался по указанию пользователя. В смежных наборах остались два расхождения старых ожиданий с самоуничтожением носителя и значением события 18. Нативная проверка не проводилась.

Логи: D:/DeepSpaceSaga/artifacts/validation/debug-kill-all/20261120-114538-167/ и D:/DeepSpaceSaga/artifacts/validation/debug-kill-all/20261120-114617-196/.
