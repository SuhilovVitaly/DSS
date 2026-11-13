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

В фазе планирования движения линии невыбранных дронов тоньше и прозрачнее и заканчиваются стрелкой по курсу. Выбранный дрон сохраняет модель и рамку в конечной точке.

## Проверки

TabletopMoveTests, MapMovementTests и проверки снимков Classic и Tabletop. После уточнения прошли 35 из 35 тестов, scoped format и git diff --check. Обновлены шесть эталонов движения. Результаты в D:/DeepSpaceSaga/artifacts/validation/maintenance-movement-arrows/20261113-arrows-01/movement.trx. Кадры проверены без запуска окна игры.

## Исполнение

Tabletop рисует внутреннюю полосу невыбранной ленты шириной 6 вместо 14,5. В повторном уточнении альфа полосы уменьшена со 115 до 55 из 255, а модель и рамка в конечной точке заменены стрелкой. Классический стиль снижает альфу фоновой линии со 120 до 60 и сохраняет треугольный указатель курса.
