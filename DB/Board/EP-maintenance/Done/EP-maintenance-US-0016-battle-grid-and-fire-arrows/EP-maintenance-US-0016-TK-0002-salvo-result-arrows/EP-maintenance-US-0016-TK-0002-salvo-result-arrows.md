---
epic: EP-maintenance
story: EP-maintenance-US-0016-battle-grid-and-fire-arrows
ticket: EP-maintenance-US-0016-TK-0002-salvo-result-arrows
title: Стрелки результатов залпа
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC2]
---

# Стрелки результатов залпа

[User story](../EP-maintenance-US-0016-battle-grid-and-fire-arrows.md)

Стрелки результатов в Tabletop тоньше и прозрачнее. Цвета различают попадания сторон и промахи. Цифры урона и подписи исхода на поле компактнее.

## Проверки

TabletopSalvoTests проверяет появление стрелок и подписей по времени попадания. Эталонные кадры показывают различия между попаданиями и промахами.

В Tabletop шрифт плашек попаданий уменьшен с 12,5 до 9,5 логических пикселя, а шрифт остальных плашек результатов — с 10,5 до 9. Обновлены эталоны обычного залпа и залпа с ловушкой.

Итоговый прогон боя и сетки прошёл 590 из 590 проверок. Форматирование и git diff --check прошли. Плашки сверены на растровом кадре. Окно игры не запускалось. Результаты — D:/DeepSpaceSaga/artifacts/validation/maintenance-salvo-badges/20261113-badges-01/final/salvo-badges.trx.
