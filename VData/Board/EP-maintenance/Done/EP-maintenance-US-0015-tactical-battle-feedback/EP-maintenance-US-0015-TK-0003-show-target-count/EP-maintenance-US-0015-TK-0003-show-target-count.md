---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0003-show-target-count
title: Число целей рядом с оружием
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC3]
---

# Число целей рядом с оружием

[User story](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерий

У каждого пилота при планировании стрельбы показывается число доступных целей рядом с названием оружия, в том числе ноль.

## Проверки

TabletopSnapshots, Ep0019ViewTests.

## Исполнение

Доступные цели берутся из текущего плана огня. Заклинившее оружие, пыль и неактивный дрон дают ноль. Подписи локализованы для русского и английского.
