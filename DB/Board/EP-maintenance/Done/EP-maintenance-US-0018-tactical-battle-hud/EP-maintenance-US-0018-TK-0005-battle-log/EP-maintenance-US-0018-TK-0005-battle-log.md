---
epic: EP-maintenance
story: EP-maintenance-US-0018-tactical-battle-hud
ticket: EP-maintenance-US-0018-TK-0005-battle-log
title: Панель журнала боя
stage: approved
approval_kind: workflow
implementation_status: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-maintenance-US-0018-TK-0004-record-movement]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleLogTab.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
supporting_changes:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
serves: [AC5]
created: 2026-11-13T14:20:13.611Z
revision: 1
---

# Панель журнала боя

[История](../EP-maintenance-US-0018-tactical-battle-hud.md)

## Why

Справа отображается прокручиваемый журнал за весь бой. Выстрелы, повреждения и гибель появляются по мере проигрывания залпа. Карточка сборки убрана из экрана.

## Decisions and assumptions

Журнал занимает правую колонку под фазой, с местом для краткой карточки под курсором и сводки залпа.

## Code context and implementation

TacticalBattleLogTab форматирует проекцию журнала. Подключить отрисовку и прокрутку, удалить показ BuildCard и локализовать заголовок и перемещение. Проекция автоматического залпа сохраняет уже завершённые события движения до первого попадания.

## Public API

Без изменения публичного API.

## Invariants and out of scope

Авторитетные движение, стрельба и урон сохраняют существующую семантику. Во время воспроизведения журнал следует событиям текущего кадра.

## Tests and definition of done

OverlayShellTests и SidesAndLogTests проверяют место панели, ввод и журнал во время залпа.
Результаты проверок и сборки записываются перед публикацией.

## Self-containment check

Пути в files_touched задают границы реализации. Проверки используют существующие фикстуры слоя.
