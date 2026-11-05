---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0008-battle-map
ticket: EP-0015-US-0008-TK-0005-movement-animation
title: movement-animation
stage: done
layer: client
depends_on: ["EP-0015-US-0008-TK-0004-movement-orders"]
files_touched: 5
serves: [AC-05]
created: 2026-11-05
revision: 3
---

# movement-animation

## Why
Анимация движения и ожидание ответа.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | из TK-0001 | Блокировка ввода, индикатор ожидания, анимация 900 мс, переходы фаз |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementAnimationTests.cs` | new | Кадры анимации на фиксированном времени; ввод заблокирован; конец боя в движении |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovementAnimation.cs` | new | Immutable frame interpolation over admitted Engine paths |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | serializes phase events | Revalidate at dispatch to stop a queued Enter during playback |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | headless fixture | Inject manual monotonic clock and a controlled pending connection response |

## Public API after the change
—

## Implementation steps
1. Время — реальное.
2. Асинхронный вызов — по образцу `TradeScreen._quoteTask`.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Animate only after an accepted Move with paths for that turn. Keep the admitted next frame in Flow, but publish a display snapshot interpolated from the pre-move units until 900 monotonic milliseconds have elapsed. Pending and playback both gate command dispatch; an event already queued behind the request is checked again when the window lock is acquired. Test the 0/450/899/900 ms boundaries and no-request behavior with the real window/connection and a controlled response delay. Salvo presentation remains US-0009.

## Execution evidence — 2026-11-06

AC-05: only an accepted Move starts the 900 ms monotonic timeline. UI reads the immutable pre-move snapshot and admitted paths until playback ends; the accepted engine state remains available to Flow. Enter, mouse orders and already queued Move/Fire are blocked until arrival. A real held response verifies pending, exact first frame, interpolation, 899/900 ms gate and absence of duplicate commands. A terminal response injected over real movement paths verifies the rare direct outcome presentation; this is Client branch coverage, not a claimed new engine scenario. US-0009 owns shot playback.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-movement-animation-01/: format/build/diff/self-review PASS; Client168, Engine378, Motion source guard19 PASS. Native/physical/human acceptance remains open.
