---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0003-read-pilot-and-loadout
ticket: EP-0022-US-0003-TK-0002-restore-meter-contrast
title: Восстановить читаемость шкал пилотов
stage: implemented
layer: client
depends_on: [EP-0022-US-0003-TK-0001, EP-0022-US-0001-TK-0002]
files_touched: 2
serves: [AC-01]
---

# Восстановить читаемость шкал пилотов

[История](../EP-0022-US-0003-read-pilot-and-loadout.md) · [Эпик](../../Documentation.md)

На нативном кадре стрельбы заполненные шкалы слева выглядят почти пустыми. `PilotCardColumn.Draw` спрашивает цвет `ui.pilot-card.bar.<kind>`. Без собственного fill он наследует тёмный цвет панели через `TacticalBattleSkin.Find`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` | Цвет панели наследуется шкалами | Задать отдельные тона брони, корпуса и сознания. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | Покрыта длина общей шкалы | Проверить яркость и различие трёх полос на фактическом рендере карточки. |

## Acceptance criteria

Заполненные полосы различимы по светлым тонам. Подписи, значения, длина заполнения, координаты карточки и выбор пилота совпадают с исходным контрактом.

## Tests

Release `Ep0022SkinScenesTests`, `PilotCardColumnTests` и `ShippedSkinTests`. Нативный просмотр в 1920×1080 и 1280×720 с текущим манифестом, на проверенном меньшем мониторе. Scoped format, diff и хэши манифеста между исходником и publish.

## Definition of Done

Воспроизведение и исправление отражены в Board. Паритет и читаемость подтверждены указанными проверками. Отдельный commit и push.

## Execution

- Дефект виден на исходном нативном кадре стрельбы. Три явных роли палитры заменили наследование тёмной поверхности. Формулы и размеры шкал прежние.
- Release — 19/19 целевых тестов, включая отдельный растровый контроль яркости и разных цветов полос. Scoped форматирование и `git diff --check` прошли.
- Манифест локального publish совпал с исходником по SHA-256. Нативные прогоны с исправлением прошли в 1920×1080 и 1280×720 по 39 проверок, итог и RNG совпали с Classic. Пути к кадрам приведены в [отчёте исполнения](../../ExecutionStatus.md).
