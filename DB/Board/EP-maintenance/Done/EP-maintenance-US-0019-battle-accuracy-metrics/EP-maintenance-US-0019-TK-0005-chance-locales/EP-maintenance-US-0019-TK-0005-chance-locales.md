---
epic: EP-maintenance
story: EP-maintenance-US-0019-battle-accuracy-metrics
ticket: EP-maintenance-US-0019-TK-0005-chance-locales
title: Локализация факторов
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
serves: [AC5]
files_touched:
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
---

# Локализация факторов

[История](../EP-maintenance-US-0019-battle-accuracy-metrics.md)

Подписать факторы на русском и английском.

## Проверка

Целевые проверки проекта, регрессия снимка боя и эталоны Tabletop для видимых изменений.
