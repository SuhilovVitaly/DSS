---
epic: EP-maintenance
story: EP-maintenance-US-0016-battle-grid-and-fire-arrows
ticket: EP-maintenance-US-0016-TK-0005-capture-shot-rolls
title: Запись фактических бросков выстрела
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
depends_on: [EP-maintenance-US-0016-TK-0004-shot-roll-contract]
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
serves: [AC2]
---

# Запись фактических бросков выстрела

[История](../EP-maintenance-US-0016-battle-grid-and-fire-arrows.md)

TacticalBattleSalvo сохраняет уже используемые броски, а TacticalBattleSnapshotBuilder публикует их. Пропущенная проверка оставляет поле пустым. Порядок и число вызовов RNG сохраняются. AnomalySalvoTests прошли 8/8.

Результаты: D:/DeepSpaceSaga/artifacts/validation/tabletop-disabled-roll/20261113T163726168Z/engine.
