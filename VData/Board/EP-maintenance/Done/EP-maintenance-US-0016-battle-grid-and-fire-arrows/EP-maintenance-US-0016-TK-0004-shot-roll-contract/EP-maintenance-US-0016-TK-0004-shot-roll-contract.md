---
epic: EP-maintenance
story: EP-maintenance-US-0016-battle-grid-and-fire-arrows
ticket: EP-maintenance-US-0016-TK-0004-shot-roll-contract
title: Поля бросков в снимке выстрела
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: contracts
depends_on: []
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
serves: [AC2]
---

# Поля бросков в снимке выстрела

[История](../EP-maintenance-US-0016-battle-grid-and-fire-arrows.md)

TacticalShotSnapshot публикует nullable HitRoll и AbsorptionRoll. Старый снимок читается с пустыми значениями. TacticalBattleContractTests прошли 32/32.

Результаты: D:/DeepSpaceSaga/artifacts/validation/tabletop-disabled-roll/20261113T163726168Z/contracts.
