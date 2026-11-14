---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0001-hint-framework
ticket: EP-0028-US-0001-TK-0003-battle-signals
title: Контекстные сигналы боя
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0001-TK-0002-director-core]
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSignals.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialSignalsTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialDirectorTests.cs
serves: [US-0001/AC-03]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Контекстные сигналы боя

[История](../EP-0028-US-0001-hint-framework.md) · [Эпик](../../Documentation.md)

## Цель

Вычислять ситуации для контекстных уроков (под огнём, урон, низкое сознание, аномалия, потеря дрона, итог) из показанного снимка кадра и подключить их к условию `when.signal` директора. Сигнал также даёт ID юнита для якоря `{signal}`.

## Decisions

D-07: только данные принятого/показанного снимка, без RNG и будущих FireSteps ([04-Engineering/TacticalBattle.md](../../../../Documentation/04-Engineering/TacticalBattle.md#снимки-и-воспроизведение)).

## Assumptions

- Сигналы считаются по `TutorialObservation.Battle` — это `frame.Battle`, т.е. проекция, которую видит игрок (`TacticalBattleScreen.cs:104-108`), а не `Flow.State`. Во время залпа будущие шаги скрыты проекцией.
- «Под огнём» — легальное текущее знание: в фазе Fire снимок публикует `Incoming` (`src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:70,135`), его уже рисует карта (`BattleMapRenderer.FireTemplates.cs:335`).
- Порог сознания — `battle.Rules.Evasion.MinConsciousness` (так же, как `TacticalBattleFireOrders.cs:49`).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSignals.cs` | Нет файла. Поля: `TacticalUnitSnapshot.Side/Kind/State/Hull/MaxHull/Armor/MaxArmor/Consciousness/InDust` (`TacticalBattleSnapshot.cs:46-56`), `Incoming(ShooterId, TargetId, …)` (`:70`), `Anomalies` (`:144`), `Outcome.Kind` (`TacticalBattleOutcomeCode { A, B, Draw, Timeout }`, `:9,115`), `PlayerSide` (`:136`) | Новый статический вычислитель |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs` | После TK-0002: сигналы, кроме None, не проходят | Подключить `TutorialSignals`; хранить ID юнита сигнала у показанной реплики |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialSignalsTests.cs` | Нет файла | Новые тесты |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialDirectorTests.cs` | Из TK-0002 | Добавить тест сигнального шага |

## Public API after the change

```csharp
internal readonly record struct TutorialSignalHit(TutorialSignal Signal, string? UnitId);
internal static class TutorialSignals
{
    internal static TutorialSignalHit Find(TacticalBattleSnapshot battle, TutorialSignal signal, string phase);
}
// TutorialLine получает string? SignalUnitId (ID для якоря {signal})
```

Правила (свои = `Side == PlayerSide`, дроны = `Kind == Drone`):

| Signal | Условие | UnitId |
|---|---|---|
| `under-fire` | `phase == "Fire"` и есть `Incoming` с целью — свой дрон в `Air` | первая цель по порядку `Incoming` |
| `own-damaged` | свой дрон с `Hull < MaxHull` или `Armor < MaxArmor`, не `Destroyed` | первый по порядку `Units` |
| `low-consciousness` | свой дрон в `Air`, `Consciousness < Rules.Evasion.MinConsciousness` (при `Evasion == null` сигнала нет) | первый |
| `anomaly-present` | `Anomalies` не пусто | свой дрон с `InDust`, иначе null |
| `drone-lost` | свой дрон в `Disabled` или `Destroyed` | первый |
| `outcome-victory` | `Outcome.Kind` — код стороны игрока (`A` при `PlayerSide == "A"`, `B` при `"B"`) | null |
| `outcome-defeat` | `Outcome.Kind` — код противника | null |
| `outcome-other` | `Outcome.Kind` ∈ {Draw, Timeout} | null |

## Implementation steps

1. Реализовать `TutorialSignals.Find` по таблице; порядок — исходный порядок коллекций снимка (без сортировок).
2. В директоре условие `when.signal != None` выполняется, когда `Find(...).Signal != None`; `SignalUnitId` сохраняется при показе реплики.
3. Тесты на каждый сигнал: положительный и отрицательный снимок; тест, что во время Salvo-проекции сигнал `drone-lost` не срабатывает раньше, чем проекция показала потерю.

## Acceptance criteria

1. Восемь сигналов вычисляются по таблице; нет сигнала — `TutorialSignal.None`.
2. Директор показывает сигнальный шаг только при активном сигнале и передаёт `SignalUnitId`.
3. Вычисление не трогает `Flow.State`, RNG и `FireSteps` будущих шагов.

## Verification signal

Первый красный тест — Theory `TutorialSignalsTests.FindsEachSignalFromTheVisibleSnapshot(signal)`; затем `NoSignalOnCleanSnapshot`, `DroneLostFollowsTheProjectionNotTheFinalState`, `TutorialDirectorTests.SignalStepWaitsForItsSignalAndCarriesUnitId`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0001-tk0003-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial" --results-directory "$RUN" --logger "trx;LogFileName=signals.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Тексты уроков (US-0003-TK-0004/0005), отрисовка, правки экрана боя.

## Invariants

- Код вне `/TacticalBattle/`; без RNG и Engine.
- Порядок — порядок данных снимка (детерминизм).

## Known fallout

Нет.

## Definition of Done

- Шаги выполнены в разрешённых файлах; named tests зелёные; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Таблица задаёт каждое условие через поля Contracts с `path:line`.

## Результат

Реализован `TutorialSignals.Find` (+ `TutorialSignalHit`) по таблице правил; директор пропускает шаг с `when.signal != None` только при активном сигнале и сохраняет `TutorialLine.SignalUnitId` в момент показа. Вычисление читает только переданный снимок (`TutorialObservation.Battle` — проекция кадра), без `Flow.State`, RNG, `FireSteps`/`BeforeSalvoUnits`; порядок — порядок коллекций снимка.

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0001-tk0003-20261114T173344/`):

- Красный старт: `TutorialSignals`, `TutorialSignalHit`, `TutorialLine.SignalUnitId` отсутствовали — `../us0001-tk0003-red/build-red.log`.
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial"` (`DSS_VALIDATION_ROOT` = папка прогона) — **98/98**; новые: Theory `TutorialSignalsTests.FindsEachSignalFromTheVisibleSnapshot` ×8, `NoSignalOnCleanSnapshot`, `UnderFireOnlyInFirePhase`, `LowConsciousnessNeedsEvasionRules`, `OutcomeFollowsThePlayerSide`, `DroneLostFollowsTheProjectionNotTheFinalState`, `TutorialDirectorTests.SignalStepWaitsForItsSignalAndCarriesUnitId` — `signals.trx`, `test.log`.
- `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` — 0 предупреждений, 0 ошибок — `build.log`.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие допущения исполнителя: `Consciousness == null` не считается низким; `own-damaged` включает `Disabled` (исключён только `Destroyed`, как в таблице); исходы не зависят от имени фазы; при `PlayerSide` вне {A, B} код A/B трактуется как поражение. `DroneLostFollowsTheProjectionNotTheFinalState` проверен на синтетических снимках: будущие `FireSteps[].UnitsAfter` и `BeforeSalvoUnits` с потерей не дают сигнала, пока `Units` проекции её не показали.
