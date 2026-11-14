---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0001-hint-framework
ticket: EP-0028-US-0001-TK-0004-screen-integration
title: Подключение обучения к экрану боя
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0001-TK-0003-battle-signals]
implementation_files: 5
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs
serves: [US-0001/AC-04, US-0001/AC-02]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Подключение обучения к экрану боя

[История](../EP-0028-US-0001-hint-framework.md) · [Эпик](../../Documentation.md)

## Цель

Дать экрану боя необязательную `TutorialSession`: она получает наблюдение каждого отрисованного кадра планирования, рисует поверх оверлея и получает первой ввод, который относится к её областям. Создаётся только в бою обучения. Все последующие визуальные тикеты меняют только `UI/Screens/Tutorial/`.

## Decisions

D-02 (ввод боя не перехватывается), D-07 (наблюдение без Engine) — [эпик](../../Documentation.md#решения-пользователя-обязательные). A-01: только `IsTutorial`.

## Assumptions

- Наблюдение происходит в `RenderFrame` после `DrawOverlay`: к этому моменту `Layout.Rects` содержит все якоря кадра, а `PhasePanel.Phase` актуален.
- В этом тикете `TutorialSession.Bounds` пуст, а `OnMouseDown/OnKeyDown/OnMouseWheel` возвращают false; хуки нужны, чтобы US-0002 не трогал файлы `UI/Screens/TacticalBattle/`.
- Пока файла `battle-tutorial.json` нет (до US-0003), сессия обучения пуста, а в InterfaceLog одна строка `TUTORIAL: … not found`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | Нет файла | Новый класс: директор + хуки (API ниже) |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | `RenderFrame` (`:153-178`), `OnKeyDown` (`:180-240`, `BlockWorldKey` на `:188`), `OnMouseDown` (`:298-328`), `OnMouseWheel` (`:352-361`), `OnDeactivated` (`:139-146`), `ClockMilliseconds` (`:45`), `Layout` (`:49`) | Свойство `Tutorial`; вызов `Observe`+`Draw` после `DrawOverlay` (`:174`); хуки ввода; `Tutorial?.Release()` в `OnDeactivated` |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | `OverlayRects()` (`:21-33`), `DrawOverlay` (`:35-50`) | `OverlayRects` отдаёт `Tutorial.Bounds` |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | Перетаскивание слайдера хранится в приватном `_slider` (`:15,62-82`) | Только `internal bool SliderActive => _slider is not null;` |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `CreateTacticalBattleView(flow)` (`:1208-1209`); обучение — `StartTutorialAsync` (`:1080-1121`), флаг `IsTutorial = true` (`:1102`); образец логгера — `LogSkinWarning` (`:1393-1398`) | Для `flow.IsTutorial` создать `Tutorial = new TutorialSession(TutorialScriptLoader.Load(AppContext.BaseDirectory, LogTutorialWarning))`; новый `LogTutorialWarning` |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs` | Нет файла. Образец живого боя: `BattleNavigationTests.Fixture.Create()` + F8 + `setup.Flow.StartAsync(setup.BuildStartRequest())` (`tests/.../TacticalBattle/LocalizationCoverageTests.cs:61-65`); каталог скинов из выхода — `TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"))` (`SkinBattleIntegrationTests.cs:27`) | Новые тесты |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs` | Сценарии EP-0027 (`:24-222`), фикстура (`:264-360`) | Добавить проверку, что бой обучения получил `Tutorial`, а бой F8 после него — нет; существующие тесты не ослаблять |

## Public API after the change

```csharp
internal sealed class TutorialSession(TutorialScript script)
{
    internal TutorialScript Script { get; } = script;
    internal TutorialDirector Director { get; }
    internal IEnumerable<SKRect> Bounds { get; }                    // области, захватывающие ввод (дизайн-координаты)
    internal void Observe(TutorialObservation observation);
    internal void Draw(SKCanvas canvas, BattleLayout layout, float designWidth, long nowMs);
    internal bool OnMouseDown(SKPoint designPoint, MouseButton button);
    internal bool OnKeyDown(Key key, KeyModifiers modifiers, bool repeat);
    internal bool OnMouseWheel(SKPoint designPoint, float delta);
    internal void Release();                                         // освободить ресурсы отрисовки (портрет — US-0002)
}
// TacticalBattleScreen
internal TutorialSession? Tutorial { get; init; }
```

## Implementation steps

1. `TutorialSession` (пустые `Bounds`, хуки возвращают false, `Draw` ничего не рисует).
2. `RenderFrame`: после `DrawOverlay(canvas, frame)` и до `finally` — `Tutorial.Observe(TutorialObservation.From(frame, _phasePanel.Phase, ClockMilliseconds(), interacting: frame.AimingMovement || _camera.Panning || _pilotColumn.SliderActive, busy: frame.Pending || ConfirmExit))`, затем `Tutorial.Draw(canvas, Layout, _viewport.Wd, ClockMilliseconds())`. Без сессии путь кадра не меняется.
3. `OnKeyDown`: сразу после `BlockWorldKey` — `if (!ConfirmExit && Tutorial?.OnKeyDown(key, modifiers, repeat) == true) return ScreenEvent.None;`.
4. `OnMouseDown`: после `if (ConfirmExit) …` и `DesignPoint` — `if (Tutorial?.OnMouseDown(p, button) == true) return ScreenEvent.None;` (до `TurnBusy`, чтобы «Понял» работал во время анимации).
5. `OnMouseWheel`: первым после `DesignPoint` — `if (!ConfirmExit && Tutorial?.OnMouseWheel(p, delta) == true) return ScreenEvent.None;`.
6. `OverlayRects`: `if (Tutorial is { } tutorial) foreach (var rect in tutorial.Bounds) yield return rect;`.
7. `PilotCardColumn.SliderActive`; `SkiaWindow.CreateTacticalBattleView` и `LogTutorialWarning` (`InterfaceLog.Write("TUTORIAL: " + message)` с тем же перехватом IO, что `LogSkinWarning`).
8. Правки в `UI/Screens/TacticalBattle/` без `Math.Min/Max/Clamp`, сортировок, `Distinct`, `HashSet<`, `F0/N0`, целочисленного `/`.

## Acceptance criteria

1. Бой из «Новой игры» без сохранений имеет `Tutorial != null`; бой F8 (в том числе после обучения) — `null`.
2. В живом бою с подставленной сессией отрисовка кадров кормит директор: после Tab в Move шаг `pilot-selected` завершается.
3. Enter, Tab, стрелки, клики по карте и карточкам дают тот же результат, что без сессии (сессия возвращает false).
4. `ForbiddenCallsTests` — без новых находок относительно базы.

## Verification signal

Первым красным — `TutorialScreenIntegrationTests.RenderedFramesFeedTheDirectorAndPilotSelectionCompletesTheStep` (сессия с синтетическим сценарием из одного шага `pilot-selected`, Tabletop, Tab → Render → `StateOf == Completed`). Затем `SessionDoesNotChangeBattleInputOutcome` и в `TutorialFlowTests` — `TutorialBattleCarriesTutorialSessionAndF8BattleDoesNot`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0001-tk0004-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
# база ForbiddenCalls — ДО первой правки
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --filter "FullyQualifiedName~ForbiddenCallsTests.ProductionTacticalBattleSourceUsesOnlyApprovedOperations" --results-directory "$RUN/forbidden-base" --logger "trx;LogFileName=forbidden.trx"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests|FullyQualifiedName~MouseRoutingTests|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~OverlayBoundaryTests" --results-directory "$RUN" --logger "trx;LogFileName=integration.trx"
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --filter "FullyQualifiedName~ForbiddenCallsTests.ProductionTacticalBattleSourceUsesOnlyApprovedOperations" --results-directory "$RUN/forbidden" --logger "trx;LogFileName=forbidden.trx"
```

Сравнить находки ForbiddenCalls «до/после» как мультимножество `<файл>: <причина>` (номера строк игнорировать): новых нет. Process-tree cap 300000 ms на процесс.

## Out of scope

Отрисовка панели, подсветка, журнал (US-0002); принудительный Tabletop (US-0005); любые изменения Engine/Contracts/Save.

## Invariants

- Без сессии кадр, `Layout` и ввод идентичны базе.
- Сессия не вызывает Engine и не меняет `_orders`/`_fireOrders`.
- Новых находок ForbiddenCalls нет ([размещение](../../Documentation.md#архитектурное-размещение)).

## Known fallout

`TutorialFlowTests` (EP-0027) должны остаться зелёными; при падении — разобрать причину, не ослабляя assertions. Возможная регрессия маршрутизации — `MouseRoutingTests`, `KeyboardRoutingTests`, `OverlayBoundaryTests` (запускаются, не меняются).

## Definition of Done

- Шаги выполнены в разрешённых файлах; named tests зелёные; сравнение ForbiddenCalls записано в «Журнал».
- Сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Все точки вставки указаны с `path:line`; API сессии задан; тестовая фикстура и каталог скинов указаны.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
