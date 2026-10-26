---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0004-external-settings-content-root
title: "Контент при внешнем Settings.json"
stage: draft
layer: engine
depends_on: []
serves: [AC-01, AC-02]
---

# Контент при внешнем Settings.json

История: [US-0004](../EP-0015-US-0004-linux-distribution.md).

Перенос Settings.json в XDG иначе заставляет Engine искать Data, Scenarios и портреты в пользовательском каталоге. Добавить необязательный contentRoot с разрешением относительно Settings.json; без поля сохранить текущую семантику. Все загрузчики registry, default scenario, solar generation, station resources, combat rules и crew portraits используют единый корень. Save schema не меняется.

Code context / files_touched: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs; tests/DeepSpaceSaga.Engine.Tests/ExternalSettingsContentTests.cs. Validation: реальный комплект контента с отдельным Settings.json, создание Engine, portraits, solar config, save/load и существующие content-loader tests. Отдельный commit/push перед client AppPaths. Нативные UI gates не доказываются Engine тестами.

Validation: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-external-content/external-content-final.trx: 93/93 passed, including absolute/relative external configuration, exact portable portrait references, generation config, real bootstrap and save/load; whitespace contentRoot rejected. First test run exposed ImmutableArray reference-equality assertion; replaced with exact sequence comparison, no product assertion removed. tests-final.log C#12 build zero diagnostics; scoped format.log exit0.
