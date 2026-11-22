---
epic: EP-maintenance
story: EP-maintenance-US-0026-battle-results-steps
stage: approved
implementation_status: implemented
---
# Последовательные шаги результатов боя

После победы с правом на добычу игрок сначала видит арест живых пилотов отключённых дронов, если арест доступен. Затем он проходит демонтаж модулей и разбор на металлолом. Только после просмотра шага металлолома появляется выход на карту системы.

Расходники остаются доступны вместе с модулями. «Забрать всё» действует в пределах текущего шага. Переход блокируется до окончания операций и подтверждения записи. Игрок вправе оставить часть добычи после предупреждения на выходе.

При загрузке пройденные действия сохраняются. Просмотр начинается с первого подходящего шага. У результатов без добычи доступен выход из сводки.

Связанный эпик — [EP-0042](../../../EP-0042-postbattle-results-and-loot/Documentation.md).

## Тикеты

- [Выбор группы операций](EP-maintenance-US-0026-TK-0001-loot-batch-contract/EP-maintenance-US-0026-TK-0001-loot-batch-contract.md)
- [Обработка группы движком](EP-maintenance-US-0026-TK-0002-filter-loot-batch/EP-maintenance-US-0026-TK-0002-filter-loot-batch.md)
- [Последовательность экранов](EP-maintenance-US-0026-TK-0003-results-navigation/EP-maintenance-US-0026-TK-0003-results-navigation.md)
- [Лимит кают на шаге ареста](EP-maintenance-US-0026-TK-0004-prisoner-cabin-limit/EP-maintenance-US-0026-TK-0004-prisoner-cabin-limit.md)

## Проверки

Целевые headless-тесты клиента прошли 21/21, движка — 101/101. В сценарии `kill all` проверены арест, отдельный запуск разбора металлолома и выход после завершения работ. Ручная приёмка остаётся открытой. [Логи](<D:/DeepSpaceSaga/artifacts/validation/ep-0042-results-steps/20261120-142910-443>).
