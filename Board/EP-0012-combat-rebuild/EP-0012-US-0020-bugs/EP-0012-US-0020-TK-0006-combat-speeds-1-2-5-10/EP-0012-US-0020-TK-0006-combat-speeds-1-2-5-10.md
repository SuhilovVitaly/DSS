---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0020-bugs"
ticket: "EP-0012-US-0020-TK-0006-combat-speeds-1-2-5-10"
title: "Скорости боя 1×, 2×, 5× и 10×"
stage: "draft"
layer: "contracts-engine-client"
depends_on: []
files_touched: 16
serves: ["US20-AC6"]
created: "2026-10-21T12:18:30.532Z"
revision: 1
---

# TK-0006 — Скорости боя 1×, 2×, 5× и 10×

[История](../EP-0012-US-0020-bugs.md) · [Контракт](../../Documentation.md)

## Why / Decisions

D20-06: прямой запрос пользователя 2026-10-21. В активном бою доступны пауза и скорости 1×, 2×, 5×, 10×. Это реальные множители базового темпа: за 4 реальные секунды продвигается соответственно 1/2/5/10 физических секунд. Engine принимает эти значения, UI показывает выбранное подтверждённое значение; 20× и 100× в бою недоступны. Пауза не продвигает бой, AI и перезарядку. Переключение не меняет физические длительности/правила боя и порядок событий; countdown/ETA пересчитываются по текущему множителю. Загрузка боя с любой разрешённой скоростью корректна. После боя сохраняется прежнее правило восстановления добоевой скорости.

## Scope / Assumptions

Это один сквозной feature ticket по прямому поручению «сделай два тикета там же». A20-07: техническое исключение из типовой декомпозиции одного layer/<=5 файлов; используется конечный межслойный allowlist ниже с matching tests для каждого затронутого production проекта. Не размножать карточки без нового поручения. Все пути вне allowlist запрещены; недостающий consumer зафиксировать как gap до расширения реализации.

A20-06: новые 2×/10× — множители текущего базового x1, не физические секунды за реальную секунду. Вне боя сохранить старые уровни 1/5/20/100; новые значения также должны корректно отображаться после загрузки/выхода из боя. Pause resume сохраняет выбранный боевой темп. Вход в бой по-прежнему может переходить на x1; после этого доступны все четыре скорости.

## Code context

| File | Current state / planned responsibility | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Contracts/SimulationSpeed.cs](../../../../src/DeepSpaceSaga.Contracts/SimulationSpeed.cs) | Speed2=5, Speed3=20, Speed4=100; multiplier=(int)speed/4.0: добавить реальные значения 2 и 10, не переименовывать старые | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | 172: CombatSpeedLocked при speed>Speed1 — заменить точным allowed set | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs](../../../../src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs) | 213: clamp активного боя к Speed1 — согласовать с allowed set | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs) | StartEncounter и AdvanceBounded: расчёт границ по фактическому множителю вместо фиксированного x1 | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs) | 44: Active speed>Speed1 отклоняется; принять разрешённые боевые значения | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Engine/Scenario/PointCombatContinuationValidation.cs](../../../../src/DeepSpaceSaga.Engine/Scenario/PointCombatContinuationValidation.cs) | валидация продолжения/ResumeSpeed: проверить новые enum значения | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Engine/SimulationClock.cs](../../../../src/DeepSpaceSaga.Engine/SimulationClock.cs) | clock fractions / AdvanceBounded: проверить расчёт с сохранением четвертей | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | 229: SpeedLabels/SpeedValues; 781: pause resume; 2143: enablement кнопок | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Encounter.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Encounter.cs) | 28: IsCombatSpeedEnabled; 112: tooltip pause/x1 | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs) | ETA preview: использовать текущий physical multiplier | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs) | ETA противоракеты: текущий multiplier | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Client/Data/Locale/Russian.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/Russian.json) | локализованные подсказки разрешённых скоростей RU | Только изменения для US20-AC6 |
| [src/DeepSpaceSaga.Client/Data/Locale/English.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/English.json) | локализованные подсказки разрешённых скоростей EN | Только изменения для US20-AC6 |
| tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0020TK0006Tests.cs (NEW) | NEW: enum IDs / multipliers / roundtrip | Только изменения для US20-AC6 |
| tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0020TK0006Tests.cs (NEW) | NEW: actual speeds / switching / scheduler / save-load | Только изменения для US20-AC6 |
| tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0006Tests.cs (NEW) | NEW: кнопки / pause resume / ETA / locale | Только изменения для US20-AC6 |

## Public API after the change

Proposed: добавить SimulationSpeed.SpeedDouble=2 и SimulationSpeed.SpeedTen=10. Существующие Speed0=0/Speed1=1/Speed2=5/Speed3=20/Speed4=100 остаются. MotionTimeMultiplier соответственно 0.5 и 2.5. Команда выбора скорости сохраняет существующую публичную границу; применяется точный боевой allowed set {0,1,2,5,10}.

## Implementation steps

1. Перепроверить source signatures и consumers на текущем baseline; перечисленные proposed API не считать существующими.
2. Последовательно обновить Contracts + tests, затем Engine + tests, затем Client/localization + tests в рамках одной карточки.
3. Проверить save/load и доставку события/скорости; не менять save format без доказанной необходимости.
4. Выполнить named checks и отдельно native/audio/UI acceptance.

## Tests

- Combat_speed_exact_advancement: fake real clock +4000ms; physical increments 1000/2000/5000/10000ms для 1/2/5/10; calendar=physical×300.
- Combat_speed_switch_and_pause: 2 секунды x2 +2 секунды x10 дают 6000 physical ms; пауза даёт ноль; resume возвращает последнюю разрешённую боевую скорость.
- Combat_speed_validation_and_save_load: все четыре скорости приняты через публичную команду; 20/100 отклонены в Active; save/load каждого разрешённого значения сохраняет продолжение.
- Combat_speed_scheduler_parity: одинаковые физические input deadlines и seed при разных speed schedules дают одинаковые RNG/outcomes; не требовать одинаковых результатов при разных реальных моментах ручного ввода.
- Combat_speed_ui_and_eta: видны/доступны 1/2/5/10 и pause, подтверждённый выбор подсвечен; reload 10physical s означает 40/20/8/4 real s; старые уровни вне боя не теряются.

План команд: для каждого проекта DeepSpaceSaga.Contracts.Tests, DeepSpaceSaga.Engine.Tests, DeepSpaceSaga.Client.Tests выполнить dotnet test tests/<project>/<project>.csproj --no-restore --filter FullyQualifiedName~PointCombatUS0020TK0006Tests. Каждый процесс ограничить 300000ms с завершением дерева. Затем релевантные clock/encounter/report/serialization regressions, owning build, scoped format и git diff --check.

Native: начать бой, последовательно выбрать 1×/2×/5×/10×, pause/resume; проверить фактический темп, подписи/подсветку, ETA и отсутствие возврата к x1 при обычных snapshot updates.

## Out of scope / Invariants

Не менять физические duration/range/damage, AI decisions, RNG или балансные значения. Engine остаётся владельцем фактов/часов. Старые enum IDs и допустимые save данные не переосмысливаются. Human/audio/performance acceptance не доказываются headless тестами.

## Definition of Done

US20-AC6 покрыт named tests и документацией; build/scoped format/diff проходят, save/load и совместимость проверены. Сейчас draft; implementation NOT STARTED; automated/native/product NOT RUN. Planning не разрешает запуск игры или commit/push.
