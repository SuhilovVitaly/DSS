---
epic: EP-maintenance
story: EP-maintenance-US-0019-battle-accuracy-metrics
ticket: EP-maintenance-US-0019-TK-0002-chance-engine
title: Факторы фактического выстрела
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-maintenance-US-0019-TK-0001-chance-contract]
serves: [AC2]
files_touched:
  - src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs
  - src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs
  - src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs
---

# Факторы фактического выстрела

[История](../EP-maintenance-US-0019-battle-accuracy-metrics.md)

Фиксировать факторы в момент разрешения, не менять формулы или RNG. Для журнала сохранять поглощение и шанс без ловушки.

## Проверка

Целевые проверки проекта, регрессия снимка боя и эталоны Tabletop для видимых изменений.
