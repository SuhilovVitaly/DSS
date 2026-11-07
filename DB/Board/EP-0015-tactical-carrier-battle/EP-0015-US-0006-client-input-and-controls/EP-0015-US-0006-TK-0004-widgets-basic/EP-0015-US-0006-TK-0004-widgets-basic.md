---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0006-client-input-and-controls
ticket: EP-0015-US-0006-TK-0004-widgets-basic
title: widgets-basic
stage: done
layer: client
depends_on: ["EP-0015-US-0006-TK-0003-mouse"]
files_touched: 4
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# widgets-basic

## Why
Элементы, которых нет в Client.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Controls/Slider.cs` | new | Ползунок |
| `src/DeepSpaceSaga.Client/UI/Controls/Toggle.cs` | new | Переключатель |
| `src/DeepSpaceSaga.Client/UI/Controls/TabStrip.cs` | new | Вкладки |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/WidgetsBasicTests.cs` | new | Ввод, uiScale |

## Public API after the change
Классы элементов.

## Implementation steps
1. По стилю существующих Controls.
2. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests (raster + ввод). Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Implementation boundary — 2026-11-05

Four planned files only. Stateful general Controls own bounds/focus/enabled/value and UI gestures, no battle rules. Slider captures exact programmatic values (untouched AI must stay unrounded), snaps only manual input to its min-anchored0.5 step with ToEven, clamps endpoints/current range, supports drag cancellation and keyboard. Toggle supports click/Space/Enter with no repeated activation. TabStrip uses stable IDs, mouse and wrap keyboard selection. Draw uses existing MenuStyle Verdana/palette with scale-aware geometry/text and clipping; private shared drawing helpers stay inside TabStrip.cs. Owning screens provide localized labels/focus routing and consume ResetInput on focus loss. Raster checks are CPU/Skia evidence, not native/input acceptance. Window minimum/right-panel geometry belongs to battle-map layout ticket.

## Execution evidence — 2026-11-05

Basic AC-04 controls implemented: precise untouched suggestions, manual0.5/ToEven slider, bounds/keyboard/focus/disabled/drag reset, single-activation toggle and stable-ID tabs. No battle rules or commands. Programmatic range/value updates do not emit user edits unless explicitly requested. Existing palette/Verdana and scale-aware clipping; owning screen supplies localized labels. No intrinsic prose/locale keys needed. Lists, scrolling text and shortcut tooltip button are the dependent TK-0005 portion.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-widgets-basic-01/: build 0 warnings/errors; scoped format/diff/self-review PASS; 79/79 TacticalBattle Client cases PASS. Four raster PNGs in renders/ at0.75/1/1.5/2 scales; widgets-basic-2.png visually inspected, readable and unclipped in the fixture. Raster is CPU/Skia, not native GPU/physical acceptance. Independent review NOT RUN. Separate immediate origin publication authorized.
