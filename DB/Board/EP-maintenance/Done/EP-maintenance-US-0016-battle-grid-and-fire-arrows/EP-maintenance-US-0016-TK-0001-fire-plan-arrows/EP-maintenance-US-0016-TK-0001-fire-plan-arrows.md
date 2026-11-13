---
epic: EP-maintenance
story: EP-maintenance-US-0016-battle-grid-and-fire-arrows
ticket: EP-maintenance-US-0016-TK-0001-fire-plan-arrows
title: Стрелки планирования огня
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC1]
---

# Стрелки планирования огня

[User story](../EP-maintenance-US-0016-battle-grid-and-fire-arrows.md)

Тонкие полупрозрачные красные стрелки показывают приказы и угрозы. Выбранный приказ выделен яркостью и толщиной.

## Проверки

MapFireTests и TabletopFireTests проверяют отображение прицелов, стрелок и шансов. Эталонные кадры показывают выбранные приказы и угрозы в подготовке стрельбы.
