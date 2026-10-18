---
epic: EP-0011-short-fixes
story: EP-0011-US-0001-observed-npc-trajectories
ticket: EP-0011-US-0001-observed-npc-trajectories-TK-0003-regression-coverage
title: Регрессии и жизненный цикл прогноза
stage: approved
layer: client
depends_on: [EP-0011-US-0001-observed-npc-trajectories-TK-0001-observation-history, EP-0011-US-0001-observed-npc-trajectories-TK-0002-map-forecast-integration]
files_touched: 2
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-08]
created: 2026-10-18T07:47:19+00:00
revision: 1
---

# Регрессии и жизненный цикл прогноза

## Why

Доказать границы наблюдаемой модели и lifecycle без регрессий соседних прогнозов.

## Decisions

D-01/D-02/D-03 и точные сообщения пользователя: [Documentation](../../Documentation.md). Вариант А подтверждён; requirements-engineer bypass разрешён.

## Assumptions

A-01–A-08 из Documentation.md применяются полностью и отдельно от решений пользователя.

## Code context

Точный allowlist ниже. Matching test project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Только перечисленные новые файлы разрешено создавать.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs` | Создан TK-0002; proposed integration API в его контракте | Добавить lifecycle/adversarial regressions |
| `tests/DeepSpaceSaga.Client.Tests/FutureTrajectoryTests.cs` | `:11 FutureTrajectoryTests` | Неизменность player/orbit/viewport; старые assertions не ослаблять |

## Public API after the change

No API change. Test-only ticket, production layer client и matching Client.Tests. Найденные production defects возвращать в TK-0001/TK-0002, не расширять этот allowlist.

## Implementation steps

1. Закрепить turn→straight, straight→turn, sign reversal, unequal turns, pause/resume/speed changes, duplicate/gap и независимость от FPS.
2. Session/load/new buffer, rollback, disappearance/reappearance, unknown/destroyed; camera/selection сохраняют trend. Повторно использованный ObjectId не наследует чужую историю.
3. Доказать независимость NPC линии от hidden controls и сохранение поведения player/orbits/боевых прогнозов и cancellation fix.
4. Выполнить focused и полный Client headless набор, существующий Engine forecast subset. Native smoke оставить отдельным gate; игру не запускать самостоятельно.

## Out of scope

Любые файлы вне allowlist; Engine/Contracts/Motion, AI/RNG, guidance/navigation, saves, combat/launch forecasts, оформление/локали, commit/push и запуск игры. Board при execution не менять без отдельного поручения. Не исправлять production defects в test-only тикете: возвращать в зависимый implementation ticket с явным пересмотром scope.

## Invariants

EngineRequirements §§21–23: визуализация не меняет Engine. `src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs:230` определяет показанную подтверждённую последовательность; будущие ingress frames не наблюдались. Обе поверхности `src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs:42` и `:75` должны следовать одному NPC rate. Immutable snapshots не мутировать; player и combat forecasts сохраняются; хранение bounded по A-07.

## Tests

Новые named tests: `ChangedTurnImmediatelyReturnsToStraight`, `StraightThenTurnNeedsTwoIntervals`, `PauseAndSpeedDoNotInventSeconds`, `FuturePacketAndDuplicateCannotRevealAi`, `LoadRemovalUnknownAndDestroyedResetObservation`, `CameraAndSelectionPreserveTrend`, `EquivalentPresentedFramesIgnoreRenderFrequency`, `PlayerAndOrbitalProjectionRemainUnchanged`.

Дополнительно после focused checks:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "Category!=ForecastBudget"
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~CombatForecast"
```

Native smoke NOT RUN / требует отдельного разрешения: NPC две секунды поворачивает, линия становится дугой; несовпадающее наблюдение сразу даёт прямую; пауза не подтверждает trend; новая session сбрасывает его. Это ручной gate, не обязательство автоматически запустить игру.

После реализации из `D:/DeepSpaceSaga/DSS`:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~NpcObservedTrajectoryTests|FullyQualifiedName~FutureTrajectoryTests|FullyQualifiedName~CombatForecastCacheTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -m:1
dotnet format tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs tests/DeepSpaceSaga.Client.Tests/FutureTrajectoryTests.cs
git diff --check
```

## Definition of Done

- Steps и served AC выполнены только в allowlist и покрыты named tests; API/units/reset соответствуют контракту.
- Commands Tests реально выполнены; observed результаты записаны, исходные failures отдельно.
- Нет скрытых assumptions/работы вне Code context; dependencies реализованы и проверены.
- Headless evidence не заменяет native/human gate. Запуск игры NOT RUN до отдельного разрешения.
- Статус на момент создания: NOT STARTED. Команды выше — план проверок, а не результаты.

## Execution 2026-10-18

Implementation and self-review complete. All eight named regressions implemented, including real headless draw capture with independent 10 deg/s arc oracle, physical-clock/FPS, lifecycle, hidden packet/duplicate, player and station orbit checks. Focused command: PASS 62/62. Full Client headless command Category!=ForecastBudget: PASS 2057/2057. Engine CombatForecast: PASS 47/47. Client build PASS 0 warnings/errors; scoped format verification PASS; git diff --check PASS. Full suite initially found invalid Clockwise capitalization in the new station fixture; corrected to contractual clockwise and full suite rerun green. No existing assertion removed or weakened. TK-0002 cached diff check had flagged an extra EOF blank line in the previously untracked integration test; removed here (the earlier unstaged-only check omitted that file). Final diff checks include staged files. Native NOT RUN, human acceptance OPEN. See ../../ImplementationStatus.md and ../../evidence/automated-results.json.
