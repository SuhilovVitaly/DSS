---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0012-build-card-ui
ticket: EP-0020-US-0012-TK-0002-build-card-render
title: Отрисовка карточки и силуэтов рам
stage: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0012-TK-0001-build-card-model]
files_touched: 6
serves: [US12-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Отрисовка карточки и силуэтов рам

## Why

Вид карточки задан UiSpec и переносится буквально.

## Decisions

Применяются [D-34–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-37: силуэт выбирается по `FrameId`; неизвестная рама рисуется силуэтом `light`. Строки RU/EN — UiSpec §26.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCard.cs` | нет | Рисование §22–§24. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | примитивы | `SlotCell`, `EmptySlot`, `FrameSilhouette`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | занятые прямоугольники панелей | Высота правой колонки с карточкой. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | `TacticalBattle.*` | §26. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | `TacticalBattle.*` | §26. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BuildCardRenderTests.cs` | нет | Пробы §25. |

## Public API after the change

`BuildCard.Draw(SKCanvas, BuildCardModel, float rightBottom)`.

## Implementation steps

1. Шапка, силуэт, ячейки, итоги.
2. Занятая область для подписей.
3. Строки.
4. Пробы §25.

## Out of scope

Регрессия вида (TK-0003).

## Invariants

Карточки противника и аномалии не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BuildCardRenderTests.Card_probes_match_ui_spec`
- `BuildCardRenderTests.Silhouette_per_frame`
- `LocalizationCoverageTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BuildCardRenderTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us12-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCard.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US12-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Вид задан UiSpec §22–§24, §26.

## Исполнение

В процессе. Scope расширен на HoverCard для production bridge, старые HoverCardTests для утверждённой замены own hover, UI spec уточнение актуальной фазы M3: первая стрельба seed1 Turn4, Марта выбирается реальным Tab. RU/EN уже поставлены TK1; примитивы размещены в BuildCard, чтобы не расширять общий BattleDesign без необходимости.

Завершено. Client28PASS: BuildRender7/Model8/Hover5/Localization8; сборкаPASS, scoped format/diffPASS. UiSpec§25:5полосок≥90%, light/heavy силуэт≥50%,2красные рамки≥50%, модель пустогоShield2; все3силуэта+unknown, обе локали. Overlay bounds300, label collision и oldenemy104px проверены. Растр выбранной Марты визуально осмотрен: шапка/ячейки/итоги помещаются без перекрытий. Self-review: Slots authority, read-only отображение и input bounds сохранены. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us12-tk2/focus/tests.trx`. US12-AC2 покрыт; baseline замена только TK3.
