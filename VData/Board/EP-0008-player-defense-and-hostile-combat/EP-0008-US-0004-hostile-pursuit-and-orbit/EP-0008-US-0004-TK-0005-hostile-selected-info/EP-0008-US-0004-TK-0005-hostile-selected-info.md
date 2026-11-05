---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0004-hostile-pursuit-and-orbit
ticket: EP-0008-US-0004-TK-0005-hostile-selected-info
title: "Подтвержденная фаза и радиус в информации выбранного врага"
stage: implemented
layer: client
depends_on: ["EP-0008-US-0004-TK-0004-combat-path-integration"]
files_touched: 3
serves: ["AC-0004"]
created: 2026-10-11
revision: 1
---

# Подтвержденная фаза и радиус в информации выбранного врага

Добавлен при execution review US-0004: Q45 и AC-0004 требуют selected-info, но исходные 44 тикета не назначали реализацию этого UI. Разрешение: [EpicExecutionPrompt](../../../EpicExecutionPrompt.md). Источники: [история](../EP-0008-US-0004-hostile-pursuit-and-orbit.md), [контракт](../../Documentation.md), Q45 в [Decisions](../../Decisions.md).

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs`: проекция подтвержденного HostileCombat только для известного Enemy NPC в выбранной информации.
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs`: строки фазы и радиуса, существующие перенос/scroll.
- `tests/DeepSpaceSaga.Client.Tests/HostileSelectedInfoTests.cs`: реальная проекция через screen, все фазы, маскирование, узкая панель.

## Implementation and acceptance

- Показать Сближение / Выход на орбиту / Удержание орбиты и желаемый радиус в км только в info выбранного известного Enemy; игрок/Friend/Neutral/unknown не получают эти строки.
- Значения исключительно из authoritative snapshot; не рассчитывать решения AI в Client и не добавлять надписи на карте.
- Смена выбора/фазы и pause обновляют/сохраняют правильные значения. Длинные строки используют существующий перенос и прокрутку панели.
- Targeted Client tests, Client build, scoped format verify, diff check; self-review, evidence, отдельные commit/push перед US-0005.
- Native gate отдельно в US-0012, не выдавать headless за native. Current-format persistence в US-0011.

## Execution evidence — 2026-10-11

Dependency `56f5b53c1425e0fc603ad2d2e2a19dd8aea627dc` published/remote verified. Only selected known Enemy NPC projects authoritative phase/radius into ObjectInfoPanel; hover-only, player, unknown and Friend/Neutral rows do not. Uses existing measured wrapping/scroll layout. Tests cover phase changes, configured radius changes, paused renders, selection changes, concealment and small panel content.

Affected Client **58/58 PASS** (`ep8-us4-tk5-client.trx`), final expanded review **10/10 PASS** (`ep8-us4-tk5-client-review.trx`); Client build 0 warnings/errors; scoped format verify/diff PASS. Initial test fixture omitted RenderObjectType and was corrected to represent known NPC; no production visibility guard weakened. Native acceptance remains NOT RUN until US-0012; self-review is not user approval.
