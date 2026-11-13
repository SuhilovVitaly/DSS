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
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopMoveTests.cs
serves: [AC1]
created: 2026-11-13T14:20:13.611Z
revision: 1
---

# Толщина траекторий

[История](../EP-maintenance-US-0018-tactical-battle-hud.md)

## Why

При отдалении камеры траектория становится тоньше. В Tabletop разница должна быть заметна на самой стрелке между общим и ближним планами.

## Decisions and assumptions

Ширина стрелки Tabletop зависит от масштаба поля в пикселях на мировую единицу. На ближнем плане рост ширины плавно замедляется. Стрелка масштабируется по ширине, контуру и наконечнику.

## Code context and implementation

DrawMovement, DrawTemplates и DrawTraversedPaths рисуют траектории. Использовать монотонный коэффициент камеры для толщины.

## Public API

Без изменения публичного API.

## Invariants and out of scope

Авторитетные движение, стрельба и урон сохраняют существующую семантику. Во время воспроизведения журнал следует событиям текущего кадра.

## Tests and definition of done

Клиентские проверки траекторий и растровые сцены Classic и Tabletop. Для стрелки Tabletop измерить ширину заполнения на растрах при 1 и 6 пикселях на мировую единицу. На ближнем плане заполнение минимум вдвое шире.
Результаты проверок и сборки записываются перед публикацией.

## Self-containment check

Пути в files_touched задают границы реализации. Проверки используют существующие фикстуры слоя.

## Уточнение интерфейса

Уточнённое направление масштаба: при отдалении камеры штрих становится тоньше, при приближении толще. Проверить оба направления.

Headless-проверка прошла, 66/66 тестов, включая 23 растровые сцены. Проверка форматирования изменённых файлов клиента прошла. Результаты: D:/DeepSpaceSaga/artifacts/validation/tactical-battle-hud-refinement/20261113T144625876Z/verify.

Повторная проверка масштаба Tabletop прошла. 17/17 headless-тестов, включая измерение ширины на двух масштабах и сравнение восьми эталонных сцен. Результаты: D:/DeepSpaceSaga/artifacts/validation/tabletop-arrow-zoom/20261113T152116264Z/verify.
