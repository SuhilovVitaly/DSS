---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0007-launch-and-setup
ticket: EP-0015-US-0007-TK-0001-modal-navigation
title: modal-navigation
stage: done
layer: client
depends_on: ["EP-0015-US-0006-TK-0006-text-and-numbers"]
files_touched: 5
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# modal-navigation

## Why
Навигация между картой, подготовкой и боем.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | PushModalAsync/PopModalAsync, отрисовка стека | События боя, замена внутри модального уровня, непрозрачный модальный экран |
| `src/DeepSpaceSaga.Client/UI/ScreenStack.cs` | Push/Pop/overlay | Признак непрозрачности, Replace |
| `src/DeepSpaceSaga.Client/UI/Screens/IScreen.cs` | `enum ScreenEvent` (`:21`) | `OpenTacticalBattle`, `StartTacticalBattle`, `NewTacticalBattle`, `CloseTacticalBattle` |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | new | Пауза один раз, замена, восстановление скорости, нижние экраны не рисуются |

## Public API after the change
События и `ScreenStack.Replace`.

## Implementation steps
1. Существующие модальные экраны ведут себя как раньше.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

ScreenStack.Replace already exists; reuse it, add only opaque-visible-stack selection and IScreen.IsOpaque. Add fifth file Screens/TacticalBattle/TacticalBattleFlow.cs: shared real connection controller and concrete modal screen base. It provides a playable default-scenario/AI-orders phase view from actual options/snapshots, making start/move/fire/new/exit routes executable now rather than mocking absent future screens. TK-0003 derives the full setup form; US-0008 derives the map view, reusing this flow/lifecycle and replacing the compact presentation. No fabricated battle data or expected outcomes. SkiaWindow creates setup/battle views through two small methods, replaced by concrete rich views when those tickets land. Move/fire events added alongside planned navigation events so the base uses the real API. One modal depth/pause, replacement inside it, actual exit before speed restore, duplicate pending requests suppressed. Options load only after authoritative modal pause, outside Render. Opaque stack skips map/report rendering and map auto-transitions. Physical/native and full setup/map presentation remain later tickets; this slice proves real navigation and connection lifecycle.

## Execution evidence — 2026-11-05

AC-01 navigation implemented against real local Engine connection: pause before opaque setup/options, replace within same modal level on Start/New, confirmed Exit before Pop/resume, preserved original map camera and speed, lower map/report draw/auto-transition suppressed. Real default phase view executes both move and fire through transport to outcome; rich setup/map presentation follows planned tickets. While options are pending, Escape cancels query and restores speed; options loading does not hold transition lock. Tests instrument actual forwarded speed requests: exactly Speed0 once and original Speed2 once across full battle and New. Opaque render list skips lower draws; raster confirms modal background covers old pixels. World defeat not changed. F8/map entry, complete setup form and explicit world hotkey feedback remain dependent TK-0002…4.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-navigation-01/: build0 warnings/errors; scoped format/diff/self-review PASS; 114/114 Client (four new real navigation/opacity/cancellation cases plus tactical and old defeat/session navigation) and378/378 Tactical Engine PASS, including source guards. Headless native-handler routing is not physical/native window or human acceptance. Separate immediate origin publication authorized.
