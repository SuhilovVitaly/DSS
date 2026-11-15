---
epic: EP-maintenance
story: EP-maintenance-US-0016-battle-grid-and-fire-arrows
ticket: EP-maintenance-US-0016-TK-0003-nested-battle-grid
title: Вложенная сетка боя
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC3]
---

# Вложенная сетка боя

[User story](../EP-maintenance-US-0016-battle-grid-and-fire-arrows.md)

Сетка привязана к координатам поля. Клетка крупного уровня содержит пять клеток следующего уровня по каждой оси. Плотность и плавное появление линий определяет общий GridRenderer. Базовый шаг поля боя — пять игровых единиц.

## Проверки

GridRendererTests проверяет вложенность, смену уровней и плавное появление линий. Эталоны Classic и Tabletop проверяют использование сетки на карте боя.
