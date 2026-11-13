---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0005-dim-unselected-paths
title: Маршруты остальных дронов
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC5]
---

# Маршруты остальных дронов

[User story](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерий

В фазе движения линии невыбранных дронов тоньше и прозрачнее.

## Проверки

TabletopMoveTests, MapMovementTests.

## Исполнение

Tabletop рисует внутреннюю полосу невыбранной ленты шириной 6 вместо 14,5 и с альфа 115 из 255. Классический стиль также ослабляет фоновые плановые линии.
