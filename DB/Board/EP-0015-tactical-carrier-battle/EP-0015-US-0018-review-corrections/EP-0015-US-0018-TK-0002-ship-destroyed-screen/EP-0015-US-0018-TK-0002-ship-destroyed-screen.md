---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0018-review-corrections
ticket: EP-0015-US-0018-TK-0002-ship-destroyed-screen
title: ship-destroyed-screen
stage: done
layer: client
depends_on: ["EP-0015-US-0018-TK-0001-engine-world-integrity"]
files_touched: 7
serves: [AC-02]
created: 2026-11-06
revision: 1
---

# ship-destroyed-screen

## Why
Находка ревью №1: после удаления `DefeatScreen` гибель корабля от службы безопасности станции оставляет игрока в замороженном мире без выхода.

## Decisions
D-USER-REVIEW-01: простой терминальный экран «Корабль уничтожен», кнопки «Загрузить» и «Главное меню», без боевой статистики. Contracts не меняются.

## Assumptions
Baseline после TK-0001. Признак гибели — `IsDestroyed` строки корабля игрока (`PlayerShipObjectId`) в принятом snapshot; строка остаётся в snapshot после уничтожения.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/ShipDestroyed/ShipDestroyedScreen.cs` | new | Терминальный экран: заголовок, «Загрузить» (`ScreenEvent.OpenLoadWindow`), «Главное меню» (`ScreenEvent.MainMenu`), клавиатура/мышь |
| `src/DeepSpaceSaga.Client/GameSessionHandle.cs` | нет признака гибели | `IsPlayerShipDestroyed` из принятого snapshot |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `PollGameSessionAutoTransition:430` без терминального пути | Переход на экран гибели; блок F5/скорости/сохранения при гибели; Load/MainMenu остаются |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | — | Ключи экрана |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | — | Те же ключи, черновой перевод (R-07) |
| `tests/DeepSpaceSaga.Client.Tests/ShipDestroyedScreenTests.cs` | new | Переход, кнопки, блокировки, локаль |
| `DB/Documentation/02-FirstRelease/Mechanics/Docking.md` | описывает безопасность станции | Одна фраза о терминальном экране |

## Public API after the change
`GameSessionHandle.IsPlayerShipDestroyed`; `ShipDestroyedScreen`.

## Out of scope
Механика безопасности станции, боевая статистика, Engine.

## Invariants
Client отображает принятые snapshot и не вызывает Engine из Render; тактический бой не затрагивается.

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. TDD. Сборка, scoped format, `git diff --check`. Process-tree cap 300000 ms, результаты — в `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/<unique-run>/`.

## Definition of Done
AC-02 выполнен и проверен; лимит files_touched соблюдён; результаты записаны в карточку; отдельный коммит тикета; push в `origin` (D-USER-14/15).

## Execution evidence — 2026-11-06

Baseline `eec6c24b`. Изменены все 7 разрешённых файлов.

- `ShipDestroyedScreen` (новый): непрозрачный терминальный экран, заголовок «КОРАБЛЬ УНИЧТОЖЕН», «ЗАГРУЗИТЬ» → `OpenLoadWindow`, «ГЛАВНОЕ МЕНЮ» → `MainMenu`; Tab/стрелки переключают фокус, Enter/Space выбирают, левая кнопка мыши — попадание по кнопке.
- `GameSessionHandle.IsPlayerShipDestroyed` — строка `PlayerShipObjectId` с `IsDestroyed` в принятом snapshot; `SetSpeedAsync` держит Speed0 без обращения к соединению, `SaveAsync` отклоняет `player_destroyed`.
- `SkiaWindow`: `PollGameSessionAutoTransition` запускает переход; `EnsureShipDestroyedScreen` один раз на сессию заменяет стек (`ReplaceAll`), обнуляет модальную глубину; пропускаются только None/MainMenu/Exit/Load/QuickLoad; `PushModalAsync` допускает лишь LoadScreen, `PopModalAsync` не снимает терминальный экран; F5, окно и слот сохранения заблокированы.
- Ключи `ShipDestroyed.Title/Load/MainMenu` в RU/EN; одна фраза в `Docking.md`.

Тесты `ShipDestroyedScreenTests` (5): переход, блокировки сохранения/скорости/мировых событий и возврат из Load, клавиатура/мышь, обе локали. Red на baseline — тип экрана и `IsPlayerShipDestroyed` отсутствуют (компиляция). Артефакты: `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/us0018-tk0203-20261106-green/`: сборка решения 0 warnings/0 errors, scoped format и `git diff --check` PASS, целевые 15/15 PASS, Client.Tests 1974/1975 — единственное падение `ResultsPhaseTests.ActualSalvoWaitsForArrivalAndSkipReplayContinueNeverSendAnotherShot` (`ReplaySalvoBounds` пуст) воспроизводится на неизменённом `eec6c24b` и к тикету не относится. Native/human не выполнялись.
