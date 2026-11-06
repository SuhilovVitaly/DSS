---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0012-smaller-disabled-medical-cross
stage: done
layer: client
depends_on: [EP-0015-US-0019-TK-0009-disabled-drone-cross]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Уменьшить красный крест выведенного из строя дрона

## Контракт AC-12

Индикатор ранения выглядит как тонкий красный плюс примерно 10 логических пикселей слева от имени. Серый X сбитого тэга сохраняется.

## Code context и реализация

Использовать отдельный символ + со штрихом 1.5 логического пикселя; сохранить позицию и цвета остальных маркеров.

## Проверка

В Dss-Main запустить проверки DeepSpaceSaga.Client.Tests `BattleMapRenderTests` и `git diff --check`. Каждый тикет опубликовать отдельным коммитом. Ручную приёмку фиксировать отдельно.

## Исполнение

Коммит `340159d2` опубликован. 10/10 проверок карты. Растр подтверждает меньший тонкий плюс с прямыми лучами в движении и стрельбе. Ручная приёмка открыта.
