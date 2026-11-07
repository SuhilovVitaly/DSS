---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0007-launch-and-setup
ticket: EP-0015-US-0007-TK-0002-battle-key-and-hint
title: battle-key-and-hint
stage: done
layer: client
depends_on: ["EP-0015-US-0007-TK-0001-modal-navigation"]
files_touched: 5
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# battle-key-and-hint

## Why
Вход в бой.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | OnKeyDown | F8 (из TacticalBattleInputSettings) → ScreenEvent.OpenTacticalBattle |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` | отрисовка карты | Подсказка F8 |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleKeyTests.cs` | new | Событие, подсказка, недоступные данные |

## Public API after the change
—

## Implementation steps
1. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Five Client code files: three planned plus SkiaWindow.cs to inject the same captured settings into both new/loaded map screens, and BattleNavigationTests.cs to extend the real headless fixture for remapping/availability observations. Add scale-aware clickable launch row above existing map toolbar, retain old button positions and reserve right-column space. Use localized configured-key caption; if fallback F8 conflicts, hide key hint and keep clickable launch. Options remain loaded only after opening modal, so content-error path stays reachable and Start is absent. Test observable UI error/absence using structured failed options; real launch/nav remains actual LocalGameSessionConnection. Existing navigation tests assert modal protocol rather than concrete base type so planned rich subclasses preserve the same behavior.

## Execution evidence — 2026-11-06

AC-02 implemented: captured F8/custom binding injected into both new/loaded map screens; localized RU/EN caption and clickable scale-aware map entry. Collision fallback leaves the clickable entry without a misleading F8 hint. Opens opaque paused preparation through the actual window router and local connection. Structured content issue shows file/field/reason, hides Start and blocks even a queued Start; logs reason in InterfaceLog. Backend content-error isolation was independently exercised by US-0001 loader tests; this UI test injects the structured unavailable reply to check presentation. AC-03 of US-0001 now has its Client evidence.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-launch-key-01/: build and full uninterrupted2428/2428 Client PASS before final diagnostic-only logging addition; 106 focused PASS. Final code run D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-launch-key-02/: build0 warnings/errors, verify-format/diff/self-review PASS;106/106 TacticalBattle Client PASS including new log assertion, retained Interface.log. Prior modal code6ac53a85 Linux workflow42116152154 PASS; this commit joins next run. Native/physical/human acceptance still open. Separate immediate origin publication authorized.
