---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0001-scale-unit-markers
title: Масштаб фишек
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC1]
---

# Масштаб фишек

[User story](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерий

Фишки поля и призраки конечных точек уменьшаются до ⅓ размера при минимальном зуме и достигают прежнего размера при максимальном. Размер текста фиксирован.

## Проверки

OverlayCameraInteractionTests, HoverCardTests, MapDesignTests.

## Исполнение

Камера вычисляет масштаб фишек по логарифму кратности зума. Модели кораблей, курсорные зоны наведения и точки выбора используют этот масштаб. Подписи и модели в карточках остаются прежнего размера.
