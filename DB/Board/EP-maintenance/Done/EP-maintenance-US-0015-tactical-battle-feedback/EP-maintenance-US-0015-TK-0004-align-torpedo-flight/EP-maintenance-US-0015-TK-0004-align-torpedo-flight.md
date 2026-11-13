---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0004-align-torpedo-flight
title: Направление торпеды
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC4]
---

# Направление торпеды

[User story](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерий

Горизонтальный кадр спрайта торпеды поворачивается на фактический угол полёта. Сохраняется цвет стороны. Размер торпеды использует тот же коэффициент масштаба, что и размер дронов.

## Проверки

TabletopSpriteTests, TabletopSalvoTests.

## Исполнение

Для каждой стороны берётся один горизонтальный кадр из существующего атласа. Угол вычисляется по экранному вектору от начала к концу полёта.

Коэффициент "UnitScale" передаётся в рендер торпеды в Tabletop и в рендер снаряда других скинов. Проверка "TabletopSpriteTests" сравнивает фактическую ширину пиксельного спрайта при масштабе 1 и 1/3.
