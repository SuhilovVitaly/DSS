---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0002-comms-panel
ticket: EP-0028-US-0002-TK-0003-target-highlight
title: Подсветка цели и стрелка
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0002-TK-0002-comms-panel]
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialHighlight.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHighlightTests.cs
serves: [US-0002/AC-03]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Подсветка цели и стрелка

[История](../EP-0028-US-0002-comms-panel.md) · [Эпик](../../Documentation.md)

## Цель

«Показывать, а не описывать»: элемент, о котором говорит реплика, получает мягкую пульсирующую рамку и стрелку. Цель находится по якорю шага в `BattleLayout.Rects` текущего кадра.

## Decisions

D-02 (подсветка не перехватывает ввод), D-03 (якоря размечены под Tabletop) — [эпик](../../Documentation.md#решения-пользователя-обязательные).

## Assumptions

- Подсветка рисуется только при развёрнутой реплике с якорем и включённых подсказках; не входит в `Bounds` (клики проходят сквозь неё).
- Пульс от `nowMs` наблюдения: альфа `0.55 + 0.45·sin(2π·t/1200)`; рамка = цель, расширенная на 4, ширина 2, цвет `BattleDesign.Yellow` (`BattleDesign.cs:80`). Стрелка — треугольник 12 px над целью остриём вниз; если верх цели < 40 — под целью остриём вверх.
- Если цель не найдена (элемент не нарисован в этом кадре или скин не Tabletop, A-07) — ничего не рисуется и не логируется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialHighlight.cs` | Нет файла. Ключи якорей и их источники: `pilot:<unitId>` (`PilotCardColumn.cs:208`), `card-command:<action>` (`PilotCardColumn.cs:48`), `endpoint:<unitId>` (`BattleMapRenderer.Movement.cs:35`, `BattleMapRenderer.Templates.cs:49`), `reachable` (`BattleMapRenderer.Templates.cs:23`), `candidate:<unitId>` (`BattleMapRenderer.Fire.cs:194`, `BattleMapRenderer.FireTemplates.cs:395`), `fire-chevron:<unitId>` (`BattleMapRenderer.FireTemplates.cs:284`), `evade-ring:<unitId>` (`BattleMapRenderer.cs:169`), `button:primary`/`button:replay` (`PhasePanel.cs:61`), `minimap` (`TacticalBattleMinimap.cs:56`) | Новый резолвер + отрисовка |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | После TK-0002 рисует панель | Перед панелью рисовать подсветку для `Director.Current` |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHighlightTests.cs` | Нет файла | Новые тесты |

## Public API after the change

```csharp
internal static class TutorialHighlight
{
    // anchor: "<prefix>:{own|selected|any|signal}" | "<prefix>:<literal>" | "<literal>"
    internal static string? ResolveKey(string? anchor, BattleLayout layout, TutorialObservation observation, string? signalUnitId);
    internal static void Draw(SKCanvas c, BattleLayout layout, SKRect target, long nowMs);   // пишет tutorial:highlight, tutorial:arrow
}
```

Разрешение шаблонов (порядок — порядок `observation.Battle.Units`, без сортировок):
- `{own}` — первый свой дрон не в `Destroyed`, для которого есть ключ `<prefix>:<id>`; если выбран свой дрон с ключом — он.
- `{selected}` — `observation.SelectedDroneId`.
- `{any}` — первый юнит (любой стороны), для которого есть ключ.
- `{signal}` — `signalUnitId` реплики (US-0001-TK-0003).
- без шаблона — ключ как есть.

## Implementation steps

1. Резолвер и отрисовка в `TutorialHighlight`.
2. `TutorialSession.Draw`: если `Director.Current` с якорем и подсказки включены — `ResolveKey`; при найденном ключе — `TutorialHighlight.Draw` по `layout.Rects[key]`, затем панель (панель поверх).
3. Сохранять последнее наблюдение в сессии для резолвера.

## Acceptance criteria

1. Каждый вид шаблона разрешается в ожидаемый ключ на живом Tabletop-кадре (Move: `pilot:{own}`, `endpoint:{selected}`, `reachable`, `button:primary`; Fire: `candidate:{any}`, `card-command:hold`).
2. Рамка и стрелка записаны в `Layout.Rects` (`tutorial:highlight`, `tutorial:arrow`) и не захватывают клики.
3. Отсутствующий якорь не рисует ничего и не бросает исключений.
4. Альфа пульса детерминирована временем `ClockMilliseconds` экрана.

## Verification signal

Первым красным — `TutorialHighlightTests.ResolvesEachTemplateAgainstLiveTabletopLayout`; затем `HighlightDoesNotCaptureClicks`, `MissingAnchorDrawsNothing`, `PulseDependsOnlyOnInjectedClock`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0002-tk0003-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial" --results-directory "$RUN" --logger "trx;LogFileName=highlight.trx"
```

Живой бой — `BattleNavigationTests.Fixture` + Tabletop из `TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"))`; часы — `screen.ClockMilliseconds` (`TacticalBattleScreen.cs:45`). Process-tree cap 300000 ms.

## Out of scope

Новые ключи `Layout.Rects` в файлах `UI/Screens/TacticalBattle/` (используются только существующие); тексты уроков.

## Invariants

- Только чтение `Layout.Rects`; код в `UI/Screens/Tutorial/`.
- Подсветка не меняет `Bounds` сессии.

## Known fallout

Нет.

## Definition of Done

- Шаги выполнены; named tests зелёные; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Все ключи якорей указаны с источником `path:line`; правила шаблонов и визуальные параметры заданы.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
