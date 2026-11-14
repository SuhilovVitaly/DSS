---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0001-hint-framework
ticket: EP-0028-US-0001-TK-0002-director-core
title: Директор шагов обучения
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0001-TK-0001-script-format]
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialObservation.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialDirectorTests.cs
serves: [US-0001/AC-02]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Директор шагов обучения

[История](../EP-0028-US-0001-hint-framework.md) · [Эпик](../../Documentation.md)

## Цель

Чистая детерминированная логика: по последовательности наблюдений кадра решить, какая реплика показывается, и закрыть шаг, когда игрок сам выполнил действие. Без отрисовки и без Engine.

## Decisions

D-02 (не блокировать, шаг завершается действием игрока), D-07 (только чтение кадра) — [эпик](../../Documentation.md#решения-пользователя-обязательные).

## Assumptions

- Шаг, условие завершения которого выполнено **до показа**, закрывается молча (игрок уже умеет) — статус Completed без события Shown.
- Показанный шаг, фаза которого закончилась без выполнения, переходит в Expired и больше не показывается (кроме `phase-left`, для которого уход из фазы и есть выполнение).
- Сигналы (`when.signal`) в этом тикете считаются отсутствующими (`TutorialSignal.None` проходит всегда, иные — никогда); вычисление — TK-0003.
- Правила тишины/интервала/сворачивания — US-0002-TK-0004; здесь только «одна реплика одновременно».

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialObservation.cs` | Нет файла. Источник данных — `TacticalBattleFrame` (`UI/Screens/TacticalBattle/TacticalBattleScreen.cs:11-17`): `Battle`, `SelectedDroneId`, `Movement` (`TacticalMovementFrame`, `TacticalOrderPreview.Touched` — `TacticalBattleOrders.cs:8-16`), `FirePlan` (`TacticalFirePlanRow.TargetId`, `Group.DefaultTargetId`, `Evade`, `Decoy` — `TacticalBattleFireOrders.cs:10-24`), `AimingMovement`; имя фазы — `PhasePanel.Phase` (`PhasePanel.cs:14`) | Новый record + фабрика |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs` | Нет файла; модель — TK-0001 | Новый класс |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialDirectorTests.cs` | Нет файла. Синтетические снимки можно строить как в существующих тестах карточек (`tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardModelTests.cs`) либо собирать `TacticalBattleFrame` вручную | Новые тесты |

## Public API after the change

```csharp
internal sealed record TutorialObservation(string Phase, int Turn, TacticalBattleSnapshot Battle, string? SelectedDroneId,
    TacticalMovementFrame? Movement, TacticalFirePlan? FirePlan, long NowMs, bool Interacting = false, bool Busy = false)
{
    internal static TutorialObservation From(TacticalBattleFrame frame, string phase, long nowMs, bool interacting, bool busy);
}
internal enum TutorialStepState { Waiting, Shown, Completed, Dismissed, Expired }
internal sealed record TutorialEvent(string StepId, TutorialStepState State, string Phase, int Turn, long AtMs);
internal sealed record TutorialLine(TutorialStep Step, long ShownAtMs);
internal sealed class TutorialDirector(TutorialScript script)
{
    internal bool Enabled { get; set; } = true;          // false: ничего не показывается, Current = null
    internal TutorialLine? Current { get; }
    internal IReadOnlyList<string> Heard { get; }        // id показанных шагов по порядку (для журнала)
    internal TutorialStepState StateOf(string stepId);
    internal void Observe(TutorialObservation observation);
    internal void Acknowledge();                          // «Понял»: Current → Completed (acknowledged) или Dismissed
    internal IReadOnlyList<TutorialEvent> DrainEvents();  // для метрик (US-0006)
}
```

## Implementation steps

1. `TutorialObservation.From` копирует факты кадра; `Turn = frame.Battle.Turn`.
2. Директор хранит состояние шагов (по `Id`), фазу/ход предыдущего наблюдения, выбранного пилота на входе в фазу (для `pilot-cycled`).
3. Условие `when`: `Phase` совпадает; `Turn` равен или `MinTurn` ≤ хода; `After` в состоянии Completed/Dismissed/Expired; сигнал — см. Assumptions.
4. Предикаты завершения:
   - `pilot-selected`: фаза Move и `SelectedDroneId != null`;
   - `path-edited`: `Movement.Orders.Any(o => o.Touched)`;
   - `phase-left`: имя фазы или ход отличаются от тех, при которых шаг стал Shown;
   - `target-chosen`: `FirePlan.Rows.Any(r => r.TargetId != r.Group.DefaultTargetId)`;
   - `pilot-cycled`: в пределах фазы выбранный пилот сменился с одного непустого ID на другой;
   - `evade-or-decoy`: `FirePlan.Rows.Any(r => r.Evade || r.Decoy)`;
   - `acknowledged`: только `Acknowledge()`.
5. Порядок `Observe`: (а) завершить Current, если предикат выполнен, или пометить Expired при смене фазы/хода; (б) молча завершить Waiting-шаги с выполненным предикатом при выполненном `when`; (в) если `Enabled` и Current пуст — показать подходящий шаг с максимальным `Priority`, при равенстве — первый по файлу. Каждый переход пишет `TutorialEvent`.
6. `Busy == true` (ожидание ответа/анимация/подтверждение выхода): новые реплики не показываются, завершения учитываются.

## Acceptance criteria

1. Шаг показывается только при выполненном `when`; одновременно не больше одного.
2. Шаг закрывается действием игрока (каждый предикат шага 4 — отдельный тест).
3. Выполненный до показа шаг не показывается; Expired не возвращается.
4. При `Enabled = false` `Current` всегда null, а завершения продолжают учитываться.
5. Директор не обращается к Engine и не влияет на ввод: нет методов, возвращающих решение о блокировке.

## Verification signal

Первым красным тестом — `TutorialDirectorTests.ShowsHighestPriorityEligibleStepAndCompletesWhenPlayerSelectsPilot`; затем Theory `EachCompletionPredicateClosesTheShownStep` (6 предикатов), `StepAlreadyDoneBeforeShowIsCompletedSilently`, `UnfinishedStepExpiresWhenPhaseEnds`, `DisabledDirectorShowsNothingButTracksCompletion`, `BusyObservationDefersNewLines`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0001-tk0002-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial" --results-directory "$RUN" --logger "trx;LogFileName=director.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Вычисление сигналов (TK-0003), интеграция в экран (TK-0004), тишина/интервал/сворачивание (US-0002-TK-0004), отрисовка.

## Invariants

- Детерминизм: результат зависит только от последовательности наблюдений (время — `NowMs` наблюдения, не системные часы).
- Нет RNG, нет вызовов Engine, нет мутации снимков.

## Known fallout

Нет.

## Definition of Done

- Шаги выполнены в разрешённых файлах; named tests зелёные; сборка Client без новых предупреждений.
- API совпадает с разделом выше.
- Отдельный коммит и `git push`.

## Self-containment check

Все предикаты определены через поля, указанные с `path:line`; поведение граничных случаев зафиксировано в Assumptions.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
