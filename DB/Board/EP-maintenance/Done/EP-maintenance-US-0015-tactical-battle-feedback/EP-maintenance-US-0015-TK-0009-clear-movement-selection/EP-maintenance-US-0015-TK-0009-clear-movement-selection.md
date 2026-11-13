---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0009-clear-movement-selection
title: Сброс выбора при входе в движение
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC9]
---

# Сброс выбора при входе в движение

[История](../EP-maintenance-US-0015-tactical-battle-feedback.md)

При входе в фазу движения Tabletop ни один дрон игрока не выбран и панель команд закрыта. Щелчок по дрону или Tab снова выбирают пилота.

## Проверки

Прошли 125 headless-тестов Client в Release. Проверены вход в движение, накопление панелей, пропуск полёта и обновлённые эталоны. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-phase-panels/20261113-phase-01/verified/phase-panels.trx`.
