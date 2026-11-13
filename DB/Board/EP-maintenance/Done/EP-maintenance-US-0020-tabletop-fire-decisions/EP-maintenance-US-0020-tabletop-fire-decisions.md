---
epic: EP-maintenance
story: EP-maintenance-US-0020-tabletop-fire-decisions
title: Прогноз огня в Tabletop
stage: approved
approval_kind: workflow
implementation_status: implemented
---

# Прогноз огня в Tabletop

[Эпик](../../Documentation.md)

Игрок видит эффект назначенного залпа по цели выбранного пилота и понимает, как подготовить выстрел, когда допустимой цели нет.

- AC1. Прогноз использует текущий порог боеспособности и математику урона движка. [Тикет 1](EP-maintenance-US-0020-TK-0001-forecast-contract/EP-maintenance-US-0020-TK-0001-forecast-contract.md) · [Тикет 2](EP-maintenance-US-0020-TK-0002-forecast-rules/EP-maintenance-US-0020-TK-0002-forecast-rules.md) · [Тикет 3](EP-maintenance-US-0020-TK-0003-forecast-snapshot/EP-maintenance-US-0020-TK-0003-forecast-snapshot.md).
- AC2. Карточка показывает число назначенных выстрелов, шанс вывести цель из строя и диапазон снятия брони и корпуса. Расчёт предполагает, что стрелки смогут выстрелить. Решения противника скрыты. [Тикет 4](EP-maintenance-US-0020-TK-0004-pilot-fire-preview/EP-maintenance-US-0020-TK-0004-pilot-fire-preview.md).
- AC3. При отсутствии выстрела карточка объясняет препятствие и показывает ближайшую к допустимой цель. Для сравнения дистанция и угол нормируются на дальность и полуугол оружия. [Тикет 4](EP-maintenance-US-0020-TK-0004-pilot-fire-preview/EP-maintenance-US-0020-TK-0004-pilot-fire-preview.md).


## Проверки

В Release прошли 72 проверки Client и 153 проверки Engine, включая эталоны Tabletop и боевые эталоны движка. В новых кадрах просмотрены прогноз, абсорбция и причина отсутствия цели. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-salvo-forecast/20261113-forecast-01/verify-client/forecast.trx` и `D:/DeepSpaceSaga/artifacts/validation/tabletop-salvo-forecast/20261113-forecast-01/verify-engine/forecast.trx`.
