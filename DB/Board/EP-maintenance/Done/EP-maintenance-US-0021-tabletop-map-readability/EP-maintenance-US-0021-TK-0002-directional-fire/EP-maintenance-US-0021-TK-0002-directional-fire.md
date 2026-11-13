---
epic: EP-maintenance
story: EP-maintenance-US-0021-tabletop-map-readability
ticket: EP-maintenance-US-0021-TK-0002-directional-fire
title: Направленные стрелки планирования огня
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC2]
---

# Направленные стрелки планирования огня

[История](../EP-maintenance-US-0021-tabletop-map-readability.md)

Назначенный огонь и прогноз входящей атаки отображаются цепочками шевронов. Встречные линии одной пары расходятся по дугам, подписи следуют линиям. Сохраняется отображение абсорбции.

## Проверки

Шевроны уменьшены по ширине и толщине. Номинальный шаг сокращён с 12 до 8 единиц экранной разметки. Это относится к назначенным и входящим выстрелам.

135 headless-тестов Client в Release прошли, включая стартовый обзор, встречные дуги, снятие выделения, переходы камеры и визуальные эталоны. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-camera-fire/20261113-camera-fire-01/verified/camera-fire.trx`.

24 headless-теста Client в Release прошли. Проверены перетаскивание во время движения, сохранение позиции камеры, встречные дуги и три обновлённых эталона стрельбы. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-dense-pan/20261113-01/verified/dense-pan.trx`.
