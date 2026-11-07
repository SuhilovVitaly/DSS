---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0007-launch-and-setup
ticket: EP-0015-US-0007-TK-0003-setup-screen
title: setup-screen
stage: done
layer: client
depends_on: ["EP-0015-US-0007-TK-0002-battle-key-and-hint"]
files_touched: 5
serves: [AC-03]
created: 2026-11-05
revision: 3
---

# setup-screen

## Why
Подготовка боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSetupScreen.cs` | new | Форма подготовки |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSetupLayout.cs` | new | Раскладка |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SetupScreenTests.cs` | new | Команда start из формы; сид; недоступные данные; раскладка 1280×720 и 1920×1080 |

## Public API after the change
Экран вызывает `StartTacticalBattleAsync` через `GameSessionHandle`.

## Implementation steps
1. Опции из `GetTacticalBattleOptionsAsync`.
2. Асинхронный вызов — по образцу `TradeScreen._quoteTask` (`TradeScreen.cs:151`).
3. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Five Client code files: three planned plus SkiaWindow.cs to instantiate the concrete setup and BattleFormat.cs to distinguish percentage points from probabilities (Engine chance/goal/rear-kill are ratios). Actual choices/skills/goal derive solely from captured Engine options. The setup hit formula names the authoritative terms without hardcoding base/range/rear balance numbers; numeric rule terms are available from the battle snapshot for forecast. Source Markdown describes kit help; no simulator/HTML copied. Layout uses logical UI scaling, fits 1280x720 at1x and keeps sticky actions with scroll/focus reveal for larger scale or more rows; popup remains viewport-bounded. Existing Flow owns real asynchronous options/start, typed errors and modal navigation. Pending/unavailable states use the already verified non-startable base presentation. Seed accepts ASCII digits and minus, normalizes Unicode minus/blank/zero and preserves valid long; random seed range1..99999 is UI only. New battle prefills last accepted setup when IDs still exist. Required wording localized RU/EN.

## Execution evidence — 2026-11-06

AC-03: setup reads scenario, goal, owned roster, equipment and tactics from session options, starts with exact selected IDs and signed seed. Engine accepted modified equipment/tactics and Int64.MinValue. Invalid seed prevents Start; blank and zero normalize to 1. Keyboard and scrolling cover larger UI scale. Unavailable options keep Start disabled. Reviewed CPU raster outputs for 1280×720 at 1× and 1.5×; the latter uses scroll and sticky actions. 1920×1080 at both scales was raster-tested. Human/native acceptance remains open.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-setup-screen-01/: format/build/diff/self-review PASS; 115 Client + 378 Engine PASS, logs, TRX and renders. Separate immediate origin publication authorized.
