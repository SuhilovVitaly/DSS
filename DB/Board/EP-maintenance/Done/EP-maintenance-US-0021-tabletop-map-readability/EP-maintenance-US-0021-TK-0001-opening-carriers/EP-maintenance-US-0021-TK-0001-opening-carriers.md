---
epic: EP-maintenance
story: EP-maintenance-US-0021-tabletop-map-readability
ticket: EP-maintenance-US-0021-TK-0001-opening-carriers
title: Стартовый обзор обоих носителей
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC1]
---

# Стартовый обзор обоих носителей

[История](../EP-maintenance-US-0021-tabletop-map-readability.md)

В первой фазе движения Tabletop камера включает оба носителя. При горизонтальном старте они находятся у противоположных краёв свободной области карты. Положение камеры сохраняется при переходах между фазами.

## Проверки

135 headless-тестов Client в Release прошли, включая стартовый обзор, встречные дуги, снятие выделения, переходы камеры и визуальные эталоны. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-camera-fire/20261113-camera-fire-01/verified/camera-fire.trx`.
