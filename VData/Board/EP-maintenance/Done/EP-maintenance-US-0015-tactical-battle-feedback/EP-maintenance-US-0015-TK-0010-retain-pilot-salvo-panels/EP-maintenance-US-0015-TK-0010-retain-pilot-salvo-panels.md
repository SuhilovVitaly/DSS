---
epic: EP-maintenance
story: EP-maintenance-US-0015-tactical-battle-feedback
ticket: EP-maintenance-US-0015-TK-0010-retain-pilot-salvo-panels
title: Панели результатов пилотов до движения
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC10]
---

# Панели результатов пилотов до движения

[История](../EP-maintenance-US-0015-tactical-battle-feedback.md)

После прилёта торпеды под карточкой стрелявшего пилота открывается его собственный результат. Ранее показанные результаты остаются на месте во время следующих выстрелов и на экране результатов. При переходе к движению они закрываются. Пропуск полёта показывает результаты всех состоявшихся выстрелов.

## Проверки

Прошли 125 headless-тестов Client в Release. Проверены вход в движение, накопление панелей, пропуск полёта и обновлённые эталоны. Результаты в `D:/DeepSpaceSaga/artifacts/validation/tabletop-phase-panels/20261113-phase-01/verified/phase-panels.trx`.
