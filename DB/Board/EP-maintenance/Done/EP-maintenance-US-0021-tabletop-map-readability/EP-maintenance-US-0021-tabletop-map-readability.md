---
epic: EP-maintenance
story: EP-maintenance-US-0021-tabletop-map-readability
title: Читаемость боевой карты Tabletop
stage: approved
approval_kind: workflow
implementation_status: implemented
---

# Читаемость боевой карты Tabletop

[Эпик](../../Documentation.md)

- [AC1. Стартовый обзор обоих носителей](EP-maintenance-US-0021-TK-0001-opening-carriers/EP-maintenance-US-0021-TK-0001-opening-carriers.md). В первой фазе движения Tabletop камера включает оба носителя. При горизонтальном старте они находятся у противоположных краёв свободной области карты. Положение камеры сохраняется при переходах между фазами.
- [AC2. Направленные стрелки планирования огня](EP-maintenance-US-0021-TK-0002-directional-fire/EP-maintenance-US-0021-TK-0002-directional-fire.md). Назначенный огонь и прогноз входящей атаки отображаются цепочками шевронов. Встречные линии одной пары расходятся по дугам, подписи следуют линиям. Сохраняется отображение абсорбции.
- [AC3. Снятие выделения после залпа](EP-maintenance-US-0021-TK-0003-clear-salvo-ring/EP-maintenance-US-0021-TK-0003-clear-salvo-ring.md). Во время полёта карта выделяет текущего стрелка. По завершении или пропуске полёта жёлтый круг стрелка исчезает, результаты под карточками остаются доступными.

## Проверки

135 headless-тестов Client в Release прошли, включая стартовый обзор, встречные дуги, снятие выделения, переходы камеры и визуальные эталоны. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-camera-fire/20261113-camera-fire-01/verified/camera-fire.trx`.
