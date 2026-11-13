---
epic: EP-maintenance
story: EP-maintenance-US-0018-tactical-battle-hud
ticket: EP-maintenance-US-0018-TK-0002-pilot-health-bars
title: Шкалы корпуса и брони
stage: approved
approval_kind: workflow
implementation_status: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopSlimCardTests.cs
serves: [AC2]
created: 2026-11-13T14:20:13.611Z
revision: 1
---

# Шкалы корпуса и брони

[История](../EP-maintenance-US-0018-tactical-battle-hud.md)

## Why

В компактной карточке пилота два горизонтальных индикатора с иконками показывают доли текущих HP корпуса и брони.

## Decisions and assumptions

Шкалы используют существующие цвета скина и проекцию текущего кадра.

## Code context and implementation

DrawSlimCard использует PilotStat.Fill и существующие иконки и шкалы. Сохранить выделение урона после залпа.

## Public API

Без изменения публичного API.

## Invariants and out of scope

Авторитетные движение, стрельба и урон сохраняют существующую семантику. Во время воспроизведения журнал следует событиям текущего кадра.

## Tests and definition of done

TabletopSlimCardTests проверяет долю заполнения и выделение урона.
Результаты проверок и сборки записываются перед публикацией.

## Self-containment check

Пути в files_touched задают границы реализации. Проверки используют существующие фикстуры слоя.

## Уточнение интерфейса

Модель дрона находится справа вверху. Справа от каждой шкалы показано текущее и максимальное HP. Иконки уменьшены и окрашены в жёлтый цвет Tabletop.

Headless-проверка прошла, 66/66 тестов, включая 23 растровые сцены. Проверка форматирования изменённых файлов клиента прошла. Результаты: D:/DeepSpaceSaga/artifacts/validation/tactical-battle-hud-refinement/20261113T144625876Z/verify.
