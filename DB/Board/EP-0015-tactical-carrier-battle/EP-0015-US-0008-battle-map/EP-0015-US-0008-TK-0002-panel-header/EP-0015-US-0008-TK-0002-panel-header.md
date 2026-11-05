---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0008-battle-map
ticket: EP-0015-US-0008-TK-0002-panel-header
title: panel-header
stage: done
layer: client
depends_on: ["EP-0015-US-0008-TK-0001-battle-screen-and-camera"]
files_touched: 3
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# panel-header

## Why
Верх панели Display §5.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattlePanelHeader.cs` | new | Заголовок, значок фазы, потери, баннер, ошибка, «Новый бой» |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PanelHeaderTests.cs` | new | Значения до залпа во время полёта; отказ показывается |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | existing frame and panel | Connect immutable presentation mode and header; place buttons below it |

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

## Scope correction before implementation — 2026-11-06

Frame gains an immutable presentation mode so the header can use the captured pre-salvo units while a shot is in flight, and switch atomically to resolved losses/outcome after arrival. The timeline itself belongs to US-0009. Tests exercise that presentation boundary against real accepted salvo snapshots. Existing battle buttons and world-key notice are routed through the new full-height panel.

## Execution evidence — 2026-11-06

AC-02: right panel shows scenario/turn, phase marker, player-side losses, exact seed, localized outcome/reason, typed refusal and New Battle when permitted. Immutable SalvoFlight/SalvoResults modes are tested with actual accepted salvo snapshots: the first loss is absent in flight and present on arrival; end-of-battle result and New button are hidden in flight. US-0009 will drive the timeline. A real stale-revision refusal rendered at 1280x720 and 1.5x scale was visually inspected.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-panel-header-01/: format/build/diff/self-review PASS; Client160, Engine378, Motion source guard19 PASS; logs, TRX and panel PNG retained.
