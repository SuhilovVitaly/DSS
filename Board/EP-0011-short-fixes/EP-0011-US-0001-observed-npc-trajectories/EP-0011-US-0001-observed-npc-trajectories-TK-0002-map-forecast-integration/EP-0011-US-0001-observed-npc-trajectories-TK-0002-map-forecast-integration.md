---
epic: EP-0011-short-fixes
story: EP-0011-US-0001-observed-npc-trajectories
ticket: EP-0011-US-0001-observed-npc-trajectories-TK-0002-map-forecast-integration
title: Интеграция наблюдаемого прогноза в карту
stage: approved
layer: client
depends_on: [EP-0011-US-0001-observed-npc-trajectories-TK-0001-observation-history]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07]
created: 2026-10-18T07:47:19+00:00
revision: 1
---

# Интеграция наблюдаемого прогноза в карту

## Why

Подключить историю к фактически рисуемой будущей линии NPC.

## Decisions

D-01/D-02/D-03 и точные сообщения пользователя: [Documentation](../../Documentation.md). Вариант А подтверждён; requirements-engineer bypass разрешён.

## Assumptions

A-01–A-08 из Documentation.md применяются полностью и отдельно от решений пользователя.

## Code context

Точный allowlist ниже. Matching test project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Только перечисленные новые файлы разрешено создавать.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/SnapshotBuffer.cs` | `:88 Update`, `:186 LatestPrediction` | Владение history, legacy observation и internal rate accessor |
| `src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs` | `:73 UpdatePresentation`, `:230 PreparePresentation`, `:269 _presented` | Initial/reset baseline и наблюдение каждого применённого frame |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | `:1828 ProjectViewportInto`, `:1933` player helper | Передать rate NPC в реальный render path, сохранить игрока |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs` | `:42 ProjectInto`, `:75 ProjectViewportInto` | Ранний NPC branch, геометрия по observed rate |
| `tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs` | Новый файл; существующая поверхность `tests/DeepSpaceSaga.Client.Tests/FutureTrajectoryTests.cs:11` | Интеграция buffer/history/проекции |

## Public API after the change

SnapshotBuffer владеет internal history; добавить internal `double GetNpcObservedTurnRate(string objectId)`. В Project, ProjectInto и ProjectViewportInto internal-класса FutureTrajectoryProjector добавить optional `double npcTurnRateDegPerSec = 0`; старые NPC вызовы дают безопасную прямую, карта передаёт rate из buffer. Player calls/semantics сохраняются. NPC branch определяется RenderObjectType==SpaceObjectType.NpcShip, не HostileCombat. No public transport API change.

## Implementation steps

1. Подключить helper TK-0001 к buffer. Наблюдать полный перечень eligible NPC применённого frame; presentation ingress/будущие full snapshots исключить.
2. В PreparePresentation обработать все применяемые промежуточные revisions, initial baseline, removals, new session/reset. Legacy Update наблюдает фактически доступный snapshot, только вне presentation mode.
3. Передать rate из buffer на реальном вызове карты. NPC branch должен предшествовать hidden/linear/discrete paths в обеих проекциях; скрытый predictor нельзя вызывать даже для terminal/extension.
4. Формулы A-04/A-05, старт в отображаемой позе; stationary не движется. Сохранить стили и путь игрока, не менять положение самого NPC.
5. Добавить integration tests с controlled timestamps/sequence, без wall-clock sleeps.

## Out of scope

Любые файлы вне allowlist; Engine/Contracts/Motion, AI/RNG, guidance/navigation, saves, combat/launch forecasts, оформление/локали, commit/push и запуск игры. Board при execution не менять без отдельного поручения. Не исправлять production defects в test-only тикете: возвращать в зависимый implementation ticket с явным пересмотром scope.

## Invariants

EngineRequirements §§21–23: визуализация не меняет Engine. `src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs:230` определяет показанную подтверждённую последовательность; будущие ingress frames не наблюдались. Обе поверхности `src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs:42` и `:75` должны следовать одному NPC rate. Immutable snapshots не мутировать; player и combat forecasts сохраняются; хранение bounded по A-07.

## Tests

Новые named tests: `HiddenControlsDoNotChangeObservedNpcLine`, `NpcWithoutHostileCombatUsesSameObservationRule`, `UnconfirmedNpcUsesCurrentCourse`, `PresentedFramesOnlyAffectTrend`, `CatchUpConsumesEveryPresentedFrame`, `LegacyAndPresentationObserveEquivalentMovement`, `BothProjectionEntrypointsUseObservedRate`, `ObservedArcStartsAtDisplayedPose`, `StraightNpcExtendsToViewportWithoutHiddenTerminal`.

Сравнить одинаковые observations при разных phase/segment/active commands. Проверить очередь будущих frames, начало линии, right/left arcs, 1 км/с=10 units/с, zero speed. Oracle не вызывает production classifier.

После реализации из `D:/DeepSpaceSaga/DSS`:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~NpcObservedTrajectoryTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -m:1
dotnet format tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/SnapshotBuffer.cs src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs
git diff --check
```

## Definition of Done

- Steps и served AC выполнены только в allowlist и покрыты named tests; API/units/reset соответствуют контракту.
- Commands Tests реально выполнены; observed результаты записаны, исходные failures отдельно.
- Нет скрытых assumptions/работы вне Code context; dependencies реализованы и проверены.
- Headless evidence не заменяет native/human gate. Запуск игры NOT RUN до отдельного разрешения.
- Статус на момент создания: NOT STARTED. Команды выше — план проверок, а не результаты.

## Execution 2026-10-18

Implementation and self-review complete. All nine named integration scenarios implemented (11 theory cases). Pre-fix UnconfirmedNpcUsesCurrentCourse failed: expected X=2100, actual X=100; same test now PASS. Combined history/integration/FutureTrajectoryTests command: PASS 50/50. Client build PASS 0 warnings/errors; scoped format verification and diff check PASS. History observes bootstrap and every admitted intermediate revision; raw ingress cannot authorize observations. Both NPC projection entrypoints bypass the predictor including terminal/viewport extension. Stable sinc integral implements the planned arc formula without cancellation near zero. Existing player paths unchanged. Native NOT RUN.
