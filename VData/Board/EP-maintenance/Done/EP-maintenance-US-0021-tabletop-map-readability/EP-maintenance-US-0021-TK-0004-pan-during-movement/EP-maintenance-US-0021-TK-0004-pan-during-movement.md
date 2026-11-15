---
epic: EP-maintenance
story: EP-maintenance-US-0021-tabletop-map-readability
ticket: EP-maintenance-US-0021-TK-0004-pan-during-movement
title: Перетаскивание карты во время движения
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC4]
---

# Перетаскивание карты во время движения

[История](../EP-maintenance-US-0021-tabletop-map-readability.md)

Правая кнопка начинает перетаскивание на поле во время анимации движения дронов. Камера следует за указателем до отпускания кнопки и сохраняет положение после анимации. Панели продолжают перехватывать ввод над собой.

24 headless-теста Client в Release прошли. Проверены перетаскивание во время движения, сохранение позиции камеры, встречные дуги и три обновлённых эталона стрельбы. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-dense-pan/20261113-01/verified/dense-pan.trx`.
