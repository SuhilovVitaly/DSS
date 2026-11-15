---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0002-hover-battle-buttons
title: Наведение на кнопки
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC2]
---

# Наведение на кнопки

[User story](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерий

Доступные кнопки боя подсвечиваются и включают курсор действия. Пустая область и недоступные команды сохраняют обычный курсор.

## Проверки

TabletopCommandPanelTests, BattleNavigationTests.

## Исполнение

Подсветка и курсор используют уже зарегистрированные зоны фазовых кнопок и команд пилота с учётом обрезки. Подтверждение выхода использует те же зоны, что и клики.
