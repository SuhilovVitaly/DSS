---
epic: EP-maintenance
story: EP-maintenance-US-0007-scenario-orbits-and-poi
title: Орбиты сценарных астероидов и видимость точек интереса
stage: approved
implementation_status: implemented
dependencies: [EP-maintenance-US-0001-station-local-geography]
---

# Орбиты сценарных астероидов и видимость точек интереса

В новом мире карта показывает орбиты сценарных астероидов с первого кадра.
Слой точек интереса при входе на карту выключен.

## Приёмка

- Сценарные астероиды получают солнечные орбиты после размещения в мире v2 и до первого снимка. Стартовые координаты и модуль заданной скорости сохраняются с точностью орбитального расчёта.
- Точки интереса по умолчанию скрыты и не участвуют в выборе. Переключатель POI включает слой по запросу.

[Контракт карты EP-0018](../../../EP-0018-map-geography-and-scale/ImplementationContract.md).

Сборка и девять целевых проверок прошли. Проверены первые снимки всех шести
сценариев, включая 500 астероидов в Default_500, стартовые позиции и скорости
в Default, загрузка Save20, скрытие POI и работа переключателя.

- `D:/DeepSpaceSaga/artifacts/validation/scenario-asteroid-orbits/20261110-first-frame/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/scenario-asteroid-orbits/20261110-first-frame/scenario-checks.log`
