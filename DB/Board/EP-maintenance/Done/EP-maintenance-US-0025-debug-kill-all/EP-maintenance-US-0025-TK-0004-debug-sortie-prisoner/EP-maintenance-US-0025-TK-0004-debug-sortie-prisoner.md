---
epic: EP-maintenance
story: EP-maintenance-US-0025-debug-kill-all
ticket: EP-maintenance-US-0025-TK-0004-debug-sortie-prisoner
stage: approved
implementation_status: implemented
layer: engine
depends_on: [EP-maintenance-US-0025-TK-0002-resolve-enemy-side]
---
# Арест пилота после отладочной победы

После команды `kill all` в «Начальном бою» арест выбрасывал `Invalid prisoner custody provenance`. Интерфейс оставлял только повтор той же команды и блокировал выход на карту.

При аресте движок сохраняет данные отключённых вражеских дронов из отладочного боя вместе с записью о пленнике. Эти данные подтверждают происхождение пленника при загрузке и после замены списка трофеев. Для пиратских встреч проверка по истории встречи сохраняется.

## Проверки

- Клиент 20/20. `KillAllTutorialArrestSurvivesSaveAndAllowsExit` проходит через кнопку ареста и подтверждение выхода, восстанавливает пленника из JSON без прежнего списка трофеев и проверяет отказ загрузки при подмене имени.
- Движок 101/101 по трофеям и результатам боя.
- [Логи прогона](<D:/DeepSpaceSaga/artifacts/validation/ep-0042-debug-prisoner/20261120-142059-692>).
