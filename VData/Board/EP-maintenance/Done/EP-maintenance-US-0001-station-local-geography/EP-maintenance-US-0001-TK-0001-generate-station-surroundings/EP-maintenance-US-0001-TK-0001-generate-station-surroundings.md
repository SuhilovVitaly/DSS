---
epic: EP-maintenance
story: EP-maintenance-US-0001-station-local-geography
ticket: EP-maintenance-US-0001-TK-0001-generate-station-surroundings
title: Сгенерировать окрестности и географию
stage: approved
implementation_status: implemented
depends_on: [EP-maintenance-US-0001-TK-0004-factory-object-type, EP-maintenance-US-0001-TK-0005-solar-distance-scale]
---

# Сгенерировать окрестности и географию

[Критерии истории](../EP-maintenance-US-0001-station-local-geography.md).

Проверка. Шесть сценариев прошли проверку количества объектов, расстояний и совпадения скоростей.

## Область изменений

Engine. Файлы `SimulationEngine.cs`, `StationResourceFields.cs`, `StationClusterGenerator.cs` и `StationGeographyV2.cs`.
Размещение дистриктов также использует `SolarSystemGenerator.cs` и `ScenarioGroupPlacement.cs`.

## Реализация и проверка

Станции занимают четыре радиальных ряда с детерминированным разбросом
радиусов и фаз. Дистрикт шире поперёк орбиты и короче вдоль неё. Общий период
сохраняет форму группы. Генерация резервирует ширину радиальной полосы
и проверяет зазоры между крайними орбитами соседних групп. Фаза следующего
дистрикта рассчитывается по фактическим радиусам центров. Стартовая привязка
сохраняет разброс радиусов.

39 целевых проверок Engine прошли. В набор входят 600 вариантов генерации
по шести сценариям с проверкой расстояний, радиальных полос и связности.
Результаты: `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-district-spread/engine-checks.log`.

Для раскладки в четыре ряда повторно прошли 39 проверок Engine, включая
600 вариантов генерации и измерение поперечного и продольного размаха.
Прошли также 19 клиентских проверок, включая запуск сценариев, Save20
и растровые проверки карты. Запуск в окне не проводился.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-wider-districts/engine-checks.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-wider-districts/client-checks.log`

Материализовать один ресурсный астероид основного вида для профиля станции и пять заводов в компактной группе. Заводы размещаются с разными радиусами и угловыми промежутками. Опорный радиус равен середине прежнего диапазона ресурсного поля. Для каждого объекта использовать постоянное мировое смещение от станции. Проверить расстояния, угловые промежутки и векторы скорости во всех сценариях поставки.
