---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0007-draw-drone-wrecks
title: Общий спрайт обломков
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC7]
---

# Общий спрайт обломков

[User story](../EP-maintenance-US-0015-tactical-battle-feedback.md)

## Критерий

На поле уничтоженные дроны обеих сторон используют один серый спрайт и серое имя без показателей брони и корпуса.

## Проверки

TabletopSalvoTests, TabletopSnapshots, Ep0019ViewTests.

## Исполнение

Общий прозрачный PNG дрона 44×13 создан встроенным ImageGen в стиле серого пиксельного корпуса с разломом и двумя обломками. На поле для уничтоженных дронов скрываются счётчики, метка ловушки и рамка наведения.
