---
epic: EP-maintenance
story: EP-maintenance-US-0018-tactical-battle-hud
ticket: EP-maintenance-US-0018-TK-0001-movement-stroke
title: Толщина траекторий
stage: approved
approval_kind: workflow
implementation_status: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Movement.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Templates.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs
serves: [AC1]
created: 2026-11-13T14:20:13.611Z
revision: 1
---

# Толщина траекторий

[История](../EP-maintenance-US-0018-tactical-battle-hud.md)

## Why

С уменьшением масштаба траектория утолщается. При приближении линии плана и следа становятся тоньше в Classic и Tabletop.

## Decisions and assumptions

Относительный зум камеры определяет толщину штриха.

## Code context and implementation

DrawMovement, DrawTemplates и DrawTraversedPaths рисуют траектории. Использовать монотонный коэффициент камеры для толщины.

## Public API

Без изменения публичного API.

## Invariants and out of scope

Авторитетные движение, стрельба и урон сохраняют существующую семантику. Во время воспроизведения журнал следует событиям текущего кадра.

## Tests and definition of done

Клиентские проверки траекторий и растровые сцены Classic и Tabletop.
Результаты проверок и сборки записываются перед публикацией.

## Self-containment check

Пути в files_touched задают границы реализации. Проверки используют существующие фикстуры слоя.

## Уточнение интерфейса

Уточнённое направление масштаба: при отдалении камеры штрих становится тоньше, при приближении толще. Проверить оба направления.

Headless-проверка прошла, 66/66 тестов, включая 23 растровые сцены. Проверка форматирования изменённых файлов клиента прошла. Результаты: D:/DeepSpaceSaga/artifacts/validation/tactical-battle-hud-refinement/20261113T144625876Z/verify.
