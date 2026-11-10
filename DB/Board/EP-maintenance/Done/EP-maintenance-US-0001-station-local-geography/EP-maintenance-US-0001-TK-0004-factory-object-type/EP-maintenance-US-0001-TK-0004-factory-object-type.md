---
epic: EP-maintenance
story: EP-maintenance-US-0001-station-local-geography
ticket: EP-maintenance-US-0001-TK-0004-factory-object-type
title: Выделить тип орбитального завода
stage: approved
implementation_status: implemented
depends_on: []
---

# Выделить тип орбитального завода

[Критерии истории](../EP-maintenance-US-0001-station-local-geography.md).

Проверка. Снимки всех шести сценариев содержат отдельный тип завода.

Contracts. Добавить `SpaceObjectType.Factory` в `SpaceObjectType.cs`. Проверить, что снимки выдают отдельный тип для заводов.
