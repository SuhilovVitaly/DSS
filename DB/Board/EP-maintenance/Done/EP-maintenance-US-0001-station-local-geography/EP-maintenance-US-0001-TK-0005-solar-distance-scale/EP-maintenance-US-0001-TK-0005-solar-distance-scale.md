---
epic: EP-maintenance
story: EP-maintenance-US-0001-station-local-geography
ticket: EP-maintenance-US-0001-TK-0005-solar-distance-scale
title: Масштабировать географию нового мира
stage: approved
implementation_status: implemented
depends_on: []
---

# Масштабировать географию нового мира

[Критерии истории](../EP-maintenance-US-0001-station-local-geography.md).

Проверка. Проверены шесть сценариев поставки и масштабированные стартовые и межстанционные расстояния.

Engine и его конфигурация. Файлы `SolarSystemGeneration.cs`, `SolarSystemGenerator.cs` и `Data/Maps/solar-system.json`.

Масштабировать новые системы и межстанционные расстояния через `distanceScale`. Проверить опорные расстояния и возможность создания мира для всех включённых сценариев.
