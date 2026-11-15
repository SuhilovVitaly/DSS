---
epic: EP-maintenance
story: EP-maintenance-US-0007-scenario-orbits-and-poi
ticket: EP-maintenance-US-0007-TK-0002-hide-poi-by-default
stage: approved
implementation_status: implemented
depends_on: []
---

# Скрыть точки интереса при открытии карты

Client. `GameSessionScreen.MapView.cs` и `GameSessionScreen.cs`.

По умолчанию выключить слой POI. Скрытые точки не рисуются и не участвуют
в выборе. Проверить включение кнопкой POI.
