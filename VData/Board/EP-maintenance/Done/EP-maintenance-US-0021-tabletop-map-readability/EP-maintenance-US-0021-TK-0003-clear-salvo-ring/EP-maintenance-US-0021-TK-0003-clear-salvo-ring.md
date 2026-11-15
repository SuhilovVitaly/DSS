---
epic: EP-maintenance
story: EP-maintenance-US-0021-tabletop-map-readability
ticket: EP-maintenance-US-0021-TK-0003-clear-salvo-ring
title: Снятие выделения после залпа
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC3]
---

# Снятие выделения после залпа

[История](../EP-maintenance-US-0021-tabletop-map-readability.md)

Во время полёта карта выделяет текущего стрелка. По завершении или пропуске полёта жёлтый круг стрелка исчезает, результаты под карточками остаются доступными.

## Проверки

135 headless-тестов Client в Release прошли, включая стартовый обзор, встречные дуги, снятие выделения, переходы камеры и визуальные эталоны. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-camera-fire/20261113-camera-fire-01/verified/camera-fire.trx`.
