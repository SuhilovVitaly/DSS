---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0004-linux-distribution
ticket: EP-0015-US-0004-TK-0004-external-settings-content-root
title: "Контент при внешнем Settings.json"
stage: draft
acceptance_status: open
execution_status: implemented
layer: engine
depends_on: []
serves: [AC-01, AC-02]
---

# Контент при внешнем Settings.json

История: [US-0004](../EP-0015-US-0004-linux-distribution.md).

Перенос Settings.json в XDG иначе заставляет Engine искать Data, Scenarios и портреты в пользовательском каталоге. Добавить необязательный contentRoot с разрешением относительно Settings.json; без поля сохранить текущую семантику. Все загрузчики registry, default scenario, solar generation, station resources, combat rules и crew portraits используют единый корень. Save schema не меняется.

Code context / files_touched: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs; tests/DeepSpaceSaga.Engine.Tests/ExternalSettingsContentTests.cs. Validation: реальный комплект контента с отдельным Settings.json, создание Engine, portraits, solar config, save/load и существующие content-loader tests. Отдельный commit/push перед client AppPaths. Нативные UI gates не доказываются Engine тестами.

Validation: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-external-content/external-content-final.trx: 93/93 passed, including absolute/relative external configuration, exact portable portrait references, generation config, real bootstrap and save/load; whitespace contentRoot rejected. First test run exposed ImmutableArray reference-equality assertion; replaced with exact sequence comparison, no product assertion removed. tests-final.log C#12 build zero diagnostics; scoped format.log exit0.

Full Engine corpus follow-up: DialoguePortraitGenerationTests reproduces a compatibility regression: internal LoadCrewPortraits previously discovered the directory even when its Settings.json path was only a location anchor. Preserve that helper contract when the settings file is absent; existing files still parse contentRoot strictly and public engine bootstrap still rejects missing settings through registry validation. Existing independent portrait-generation/save-load assertions remain unchanged. Scope remains EngineContentLoader.cs; targeted real existing test is the regression proof.

Regression fix verified in D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-content-compatibility/portrait-compat.trx1/1, unchanged independent portrait generation/catalog-extension/save-load assertions. Compiled outputs were isolated under the same evidence run to avoid mutating the still-running full Engine binary; C#12 zero diagnostics. format.log scoped exit0. Public bootstrap still rejects missing Settings.json before discovery.
