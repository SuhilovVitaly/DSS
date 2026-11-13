---
epic: EP-maintenance
story: EP-maintenance-US-0018-tactical-battle-hud
ticket: EP-maintenance-US-0018-TK-0003-movement-event
title: Контракт события перемещения
stage: approved
approval_kind: workflow
implementation_status: done
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs
  - tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs
serves: [AC3]
created: 2026-11-13T14:20:13.611Z
revision: 1
---

# Контракт события перемещения

[История](../EP-maintenance-US-0018-tactical-battle-hud.md)

## Why

Журнал поддерживает новый вид события перемещения с сохранением чисел существующих видов.

## Decisions and assumptions

Новый вид события добавляется в конец перечисления. Идентификатор дрона передаётся в ActorId.

## Code context and implementation

TacticalBattleLogKind использует фиксированные численные значения. Добавить Moved = 17 и проверить JSON.

## Public API

TacticalBattleLogKind.Moved = 17.

## Invariants and out of scope

Авторитетные движение, стрельба и урон сохраняют существующую семантику. Во время воспроизведения журнал следует событиям текущего кадра.

## Tests and definition of done

TacticalBattleContractTests проверяет числовые значения и сериализацию.
Результаты проверок и сборки записываются перед публикацией.

## Self-containment check

Пути в files_touched задают границы реализации. Проверки используют существующие фикстуры слоя.
