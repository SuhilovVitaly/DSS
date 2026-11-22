---
epic: EP-maintenance
story: EP-maintenance-US-0026-battle-results-steps
ticket: EP-maintenance-US-0026-TK-0004-prisoner-cabin-limit
stage: approved
implementation_status: implemented
layer: client
depends_on: [EP-maintenance-US-0026-TK-0003-results-navigation]
---
# Лимит кают на шаге ареста

После ареста в последнюю свободную каюту координаты ещё не перерисованной кнопки могли отправить следующий запрос. Движок отклонял такой арест. Окно результатов проверяет свежий снимок непосредственно перед отправкой и показывает «Нет свободных кают» у заблокированных строк.

Регрессия воспроизведена с двумя открытыми карточками пилотов и одной свободной каютой. После первого ареста второй клик до перерисовки больше не отправляет команду. Прямой запрос к движку возвращает `prisoner_no_cabin`. В состоянии остаётся ровно один пленник.

Проверки — [воспроизведение](<D:/DeepSpaceSaga/artifacts/validation/ep-0042-prisoner-cabin-limit/20261120-143739-156/client.log>), [клиент 21/21](<D:/DeepSpaceSaga/artifacts/validation/ep-0042-prisoner-cabin-limit/20261120-143842-843/client.log>), [движок 9/9](<D:/DeepSpaceSaga/artifacts/validation/ep-0042-prisoner-cabin-limit/20261120-143816-324/engine.log>).
