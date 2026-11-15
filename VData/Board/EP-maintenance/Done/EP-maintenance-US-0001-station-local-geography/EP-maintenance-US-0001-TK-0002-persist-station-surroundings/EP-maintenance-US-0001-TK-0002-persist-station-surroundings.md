---
epic: EP-maintenance
story: EP-maintenance-US-0001-station-local-geography
ticket: EP-maintenance-US-0001-TK-0002-persist-station-surroundings
title: Сохранить формацию
stage: approved
implementation_status: implemented
depends_on: [EP-maintenance-US-0001-TK-0001-generate-station-surroundings]
---

# Сохранить формацию

[Критерии истории](../EP-maintenance-US-0001-station-local-geography.md).

Проверка. Save20 round-trip и проверки legacy-полей прошли.

## Область изменений

Engine. Файлы `ScenarioLoader.cs`, `SolarSystemGeneration.cs` и `StationClusterSaveValidation.cs`.

## Реализация и проверка

Save20 сохраняет отдельные орбиты станций. При загрузке проверяются границы
радиальных полос и общий угловой темп. Прежняя геометрия с одним радиусом
загружается в сохранённых координатах.

Проверены сохранение и повторная загрузка новой геометрии в Save20.
Результаты: `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-district-spread/client-checks.log`.

Загрузить новый тип и проверять сохранённые смещения, сохранив поддержку прежних полей. В Save20 записывается опорный радиус компактной группы. Допуск включает старую компактную и новую асимметричную расстановки. При отсутствии поля используется прежний удвоенный радиус астероида. Проверить Save20 round-trip.
