---
epic: EP-maintenance
story: EP-maintenance-US-0014-xenon-battle-background
ticket: EP-maintenance-US-0014-TK-0001-restore-grid
title: Вернуть классическую сетку на чёрном фоне Xenon Star
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: []
files_touched: 4
---

# Вернуть классическую сетку на чёрном фоне Xenon Star

[История](../EP-maintenance-US-0014-xenon-battle-background.md) · [Эпик](../../../Documentation.md) · [Контекст EP-0022](../../../../EP-0022-tactical-battle-xenon-star/ExecutionStatus.md)

Xenon Star задаёт изображение планет через `field.atmosphere` и собственный цвет сетки через `field.grid`. При отсутствии последнего TacticalBattleMapRenderer использует прежний цвет Classic. Выбран однотонный чёрный фон. Звёзды необязательны по запросу.

## Code context

- `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` — чёрный фон, убрать атмосферу и переопределение сетки.
- `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` — убрать неиспользуемый срез фона из сборки.
- `src/DeepSpaceSaga.Client/Skins/README.md` — привести список срезов к поставке.
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` — проверить фон и сетку на отрисованной карте с реальным пакетом.

## Проверка

Собрать с XenonStarSource и проверить загрузку пакета, видимость сетки и чёрный фон между линиями. Существующие тесты сцен должны сохранить одинаковый layout для Classic и Xenon Star.

## Результат

Debug-сборка с локальным XenonStarSource прошла. В новом пакете девять PNG, фон с планетами исключён. Прошли 26/26 тестов Ep0022SkinScenesTests, ShippedSkinTests и AnomalyFieldLayerTests. На кадре фазы движения проверены фон, сетка и видимость аномалий. Проверенный манифест установлен в локальный Debug-клиент. Игровое окно для этой проверки не запускалось.

Доказательства: `D:/DeepSpaceSaga/artifacts/validation/xenon-battle-background/20261111-084056/xenon-battle.png`, `D:/DeepSpaceSaga/artifacts/validation/xenon-battle-background/20261111-084056/test.log` и TRX из `D:/DeepSpaceSaga/artifacts/validation/xenon-battle-background/20261111-084056/tests/`.
