---
epic: EP-maintenance
story: EP-maintenance-US-0018-tactical-battle-hud
ticket: EP-maintenance-US-0018-TK-0004-record-movement
title: Запись выполненных перемещений
stage: approved
approval_kind: workflow
implementation_status: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-maintenance-US-0018-TK-0003-movement-event]
files_touched:
  - src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs
  - tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/JournalEventsTests.cs
serves: [AC4]
created: 2026-11-13T14:20:13.611Z
revision: 1
---

# Запись выполненных перемещений

[История](../EP-maintenance-US-0018-tactical-battle-hud.md)

## Why

После принятого движения в журнал записывается по одному событию на выполненный путь до событий автоматического огня.

## Decisions and assumptions

Источник записи — LastMovement после успешной команды Move.

## Code context and implementation

Move формирует TacticalMovementPath из LastMovement. Добавлять события до NoTargets и разрешения залпа.

## Public API

Без изменения публичного API.

## Invariants and out of scope

Авторитетные движение, стрельба и урон сохраняют существующую семантику. Во время воспроизведения журнал следует событиям текущего кадра.

## Tests and definition of done

JournalEventsTests проверяет состав и последовательность событий движения.
Результаты проверок и сборки записываются перед публикацией.

## Self-containment check

Пути в files_touched задают границы реализации. Проверки используют существующие фикстуры слоя.
