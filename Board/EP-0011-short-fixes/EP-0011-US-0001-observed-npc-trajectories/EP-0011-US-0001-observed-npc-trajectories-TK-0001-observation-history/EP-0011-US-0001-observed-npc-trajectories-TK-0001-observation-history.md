---
epic: EP-0011-short-fixes
story: EP-0011-US-0001-observed-npc-trajectories
ticket: EP-0011-US-0001-observed-npc-trajectories-TK-0001-observation-history
title: История наблюдений и определение тенденции
stage: approved
layer: client
depends_on: []
files_touched: 2
serves: [AC-01, AC-02, AC-03, AC-04, AC-06]
created: 2026-10-18T07:47:19+00:00
revision: 1
---

# История наблюдений и определение тенденции

## Why

Получить независимо проверяемую тенденцию из видимой истории без AI controls.

## Decisions

D-01/D-02/D-03 и точные сообщения пользователя: [Documentation](../../Documentation.md). Вариант А подтверждён; requirements-engineer bypass разрешён.

## Assumptions

A-01–A-08 из Documentation.md применяются полностью и отдельно от решений пользователя.

## Code context

Точный allowlist ниже. Matching test project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Только перечисленные новые файлы разрешено создавать.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/NpcMotionObservationHistory.cs` | Новый файл; текущий hidden branch `src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs:46` | Client-only DTO, bounded history и reset |
| `tests/DeepSpaceSaga.Client.Tests/NpcMotionObservationHistoryTests.cs` | Новый файл; xUnit convention `tests/DeepSpaceSaga.Client.Tests/FutureTrajectoryTests.cs:11` | Unit tests классификатора |

## Public API after the change

Предлагаемый internal API в namespace DeepSpaceSaga.Client, без transport API change:

```csharp
internal readonly record struct NpcMotionObservation(string ObjectId, double MotionTimeMs,
    double X, double Y, double SpeedKmS, double Direction);
internal sealed class NpcMotionObservationHistory
{
    internal void ObserveScene(Guid? sessionId, double motionTimeMs,
        IReadOnlyList<NpcMotionObservation> visibleAliveNpcs);
    internal double TurnRateDegPerSec(string objectId); // 0 без подтверждения
    internal void Clear();
}
```

ObserveScene принимает полный перечень eligible NPC одного показанного frame; отсутствующие IDs удаляются. Legacy null session ограничена lifetime одного SnapshotBuffer; load создаёт пустую history. DTO физически не содержит hidden controls.

## Implementation steps

1. Реализовать секундную сетку, bounded raw/sample storage A-01/A-02/A-07.
2. Signed deltas, допуск, 180° ambiguity и немедленный rate=0 при несовпадении по A-03.
3. Реализовать time/session/removal resets A-06, без новых samples на pause/duplicate.
4. Покрыть формулы и границы детерминированными тестами без Engine.

## Out of scope

Любые файлы вне allowlist; Engine/Contracts/Motion, AI/RNG, guidance/navigation, saves, combat/launch forecasts, оформление/локали, commit/push и запуск игры. Board при execution не менять без отдельного поручения. Не исправлять production defects в test-only тикете: возвращать в зависимый implementation ticket с явным пересмотром scope.

## Invariants

EngineRequirements §§21–23: визуализация не меняет Engine. `src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs:230` определяет показанную подтверждённую последовательность; будущие ingress frames не наблюдались. Обе поверхности `src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs:42` и `:75` должны следовать одному NPC rate. Immutable snapshots не мутировать; player и combat forecasts сохраняются; хранение bounded по A-07.

## Tests

Новые named tests: `TwoEqualTurnsConfirmRate`, `TwoStraightIntervalsStayStraight`, `UnconfirmedOrChangedTrendFallsBackImmediately`, `WrapAroundKeepsTurnSign`, `ToleranceAndHalfTurnBoundaries`, `OnlyPhysicalSecondsCount`, `ConfirmedInterpolationNeverExtrapolates`, `DuplicatePauseGapAndRollbackResetSafely`, `SessionAndObjectRemovalClearHistory`, `HistoryStorageIsBounded`.

Независимые примеры: 10→20→30 и 30→20→10, 359→0→1, 0→10→25, две позы вместо трёх, delta в/за пределами допуска. Assert число хранимых samples/IDs через internal test seam либо observable bounded state.

После реализации из `D:/DeepSpaceSaga/DSS`:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~NpcMotionObservationHistoryTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -m:1
dotnet format tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/NpcMotionObservationHistory.cs tests/DeepSpaceSaga.Client.Tests/NpcMotionObservationHistoryTests.cs
git diff --check
```

## Definition of Done

- Steps и served AC выполнены только в allowlist и покрыты named tests; API/units/reset соответствуют контракту.
- Commands Tests реально выполнены; observed результаты записаны, исходные failures отдельно.
- Нет скрытых assumptions/работы вне Code context; dependencies реализованы и проверены.
- Headless evidence не заменяет native/human gate. Запуск игры NOT RUN до отдельного разрешения.
- Статус на момент создания: NOT STARTED. Команды выше — план проверок, а не результаты.

## Execution 2026-10-18

Implementation and self-review complete. Client-only bounded history and all ten named test scenarios implemented (15 theory cases). Unit test command above: PASS 15/15. Client build PASS (0 errors/warnings). Scoped format verification PASS; diff check PASS. Initial sandbox format named-pipe failure recovered with approved execution outside sandbox. API matches plan; no Engine/Contracts/Motion changes. Native NOT RUN by explicit story restriction. Publication tracked in ImplementationStatus.md and Git.
