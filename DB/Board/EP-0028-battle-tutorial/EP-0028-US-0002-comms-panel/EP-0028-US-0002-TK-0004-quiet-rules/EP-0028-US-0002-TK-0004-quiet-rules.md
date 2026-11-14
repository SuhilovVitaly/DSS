---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0002-comms-panel
ticket: EP-0028-US-0002-TK-0004-quiet-rules
title: Правила «не перебивать»
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0002-TK-0002-comms-panel]
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialDirectorTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs
serves: [US-0002/AC-04]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Правила «не перебивать»

[История](../EP-0028-US-0002-comms-panel.md) · [Эпик](../../Documentation.md)

## Цель

Офицер не говорит, пока игрок принимает решение руками; новые реплики появляются в естественных паузах, не чаще заданного темпа, и сами сворачиваются, если игрок их не трогает.

## Decisions

D-02 — ненавязчивость без блокировки ([эпик](../../Documentation.md#решения-пользователя-обязательные)). Параметры темпа — `limits` сценария (US-0001-TK-0001).

## Assumptions

- «Момент речи» открывается: при смене фазы или хода; при закрытии реплики (Completed/Dismissed/Expired). Он остаётся открытым, пока реплику нельзя показать по другим правилам, и закрывается показом.
- Свёрнутая по таймеру реплика остаётся Shown (ждёт действия игрока), панель показывает значок; клик по значку разворачивает её снова.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs` | После US-0001-TK-0003: показывает реплику при пустом `Current`, `Busy` откладывает показ | Правила ниже; свойство `Collapsed`; метод `Expand()` |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | После TK-0003 рисует панель и подсветку | Свёрнутая реплика → значок без подсветки; клик по значку → `Director.Expand()` |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialDirectorTests.cs` | Из US-0001 | Тесты правил |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs` | Из US-0001/US-0002 | Тест тишины при перетаскивании пути в живом бою |

Сигнал «игрок действует руками» уже приходит в наблюдении: `Interacting = frame.AimingMovement || _camera.Panning || _pilotColumn.SliderActive` (US-0001-TK-0004; `TacticalBattleScreen.cs:132`), `Busy = frame.Pending || ConfirmExit`.

## Public API after the change

```csharp
internal bool Collapsed { get; }   // TutorialDirector
internal void Expand();            // TutorialDirector: Collapsed → false, таймер сворачивания перезапускается
```

## Implementation steps

Правила `Observe` (все проверяются до показа новой реплики):
1. **Тишина при вводе:** при `Interacting` или `Busy` новая реплика не показывается; текущая не меняется и не сворачивается.
2. **Момент речи:** новая реплика — только при открытом моменте речи (см. Assumptions).
3. **Одна за раз и приоритет:** уже есть с US-0001; не менять.
4. **Интервал:** с момента закрытия предыдущей реплики прошло ≥ `limits.minGapMs`.
5. **Предел на фазу:** в паре (фаза, ход) показано < `limits.maxLinesPerPhase` реплик; сигнальные шаги итога (`outcome-*`) из предела исключены.
6. **Сворачивание:** реплика, показанная ≥ `limits.autoCollapseMs` назад без `Interacting`, получает `Collapsed = true`. Шаг с `complete: acknowledged` при сворачивании переходит в Expired и освобождает канал (ему нечего ждать); шаг с действием остаётся Shown и свёрнутым до выполнения или конца фазы.
7. **Одноразовость:** закрытый шаг никогда не показывается снова (проверка регрессии).

## Acceptance criteria

1. Во время перетаскивания конца пути новая реплика не появляется; после отпускания — появляется в момент речи.
2. Две реплики не появляются ближе `minGapMs`; в одной фазе — не больше `maxLinesPerPhase`.
3. Через `autoCollapseMs` панель сворачивается в значок; клик по значку возвращает реплику.
4. Ни одно правило не влияет на ввод боя.

## Verification signal

Первым красным — `TutorialDirectorTests.NoNewLineWhileInteractingThenSpeaksAtNextSpeechMoment`; затем `MinGapAndPerPhaseCapAreRespected`, `LineCollapsesAfterTimeoutAndExpandRestoresIt`, `ClosedStepNeverReturns`, `TutorialScreenIntegrationTests.DraggingPathKeepsOfficerQuiet`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0002-tk0004-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial" --results-directory "$RUN" --logger "trx;LogFileName=quiet.trx"
```

Время — `TutorialObservation.NowMs`/`screen.ClockMilliseconds`. Process-tree cap 300000 ms.

## Out of scope

Изменение схемы `limits` и загрузчика (уже в US-0001-TK-0001), журнал, сохранение переключателя.

## Invariants

- Детерминизм по времени наблюдений; без системных часов.
- Код в `UI/Screens/Tutorial/`.

## Known fallout

Нет.

## Definition of Done

- Правила 1–7 реализованы и покрыты named tests; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Все правила и источник флагов наблюдения заданы; изменяются только файлы обучения.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
