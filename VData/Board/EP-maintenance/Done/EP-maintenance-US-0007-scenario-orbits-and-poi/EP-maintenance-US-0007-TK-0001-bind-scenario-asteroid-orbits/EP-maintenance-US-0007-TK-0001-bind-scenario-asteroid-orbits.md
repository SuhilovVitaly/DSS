---
epic: EP-maintenance
story: EP-maintenance-US-0007-scenario-orbits-and-poi
ticket: EP-maintenance-US-0007-TK-0001-bind-scenario-asteroid-orbits
stage: approved
implementation_status: implemented
depends_on: []
---

# Назначить орбиты сценарным астероидам

Engine. `SolarSystemGenerator.cs` и `SimulationEngine.cs`.

После финального размещения строить круговую орбиту вокруг Солнца из
стартовой позиции и скорости. Выбирать ближайшее к прежнему курсу направление
касательной. Проверить первый снимок Default и Default_500.
