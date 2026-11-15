---
epic: EP-maintenance
story: EP-maintenance-US-0019-battle-accuracy-metrics
ticket: EP-maintenance-US-0019-TK-0001-chance-contract
title: Снимок факторов попадания
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: []
serves: [AC1]
files_touched:
  - src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs
---

# Снимок факторов попадания

[История](../EP-maintenance-US-0019-battle-accuracy-metrics.md)

Публиковать составляющие шанса как необязательные поля кандидата и выстрела.

## Проверка

Целевые проверки проекта, регрессия снимка боя и эталоны Tabletop для видимых изменений.
