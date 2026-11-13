---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0002-ship-sprites
ticket: EP-0024-US-0002-TK-0001-sprite-renderer
title: Отрисовка модели: кадр, масштаб, тень, маркеры
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0001-TK-0002-tabletop-mode]
files_touched: 4
serves: [US2-AC1, US2-AC2]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Отрисовка модели: кадр, масштаб, тень, маркеры

## Why

Модели — главное отличие нового поля (D-01).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-04, A-05.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs` | — | Новый файл по образцу `DB/Board/EP-0024-battle-field-redesign/Materials/Prototype/src`/SkinShipSprites.cs: `Draw(canvas, unit, point, own, course, alpha, factor?, shadow)` по UiSpec §2.2–§2.4; множитель — §2.3. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | :115-140 юниты, :152-157 обломки | В режиме tabletop: модель вместо силуэта/`Hex`; кольцо выбора r 30 `Yellow` 1,6; `EvasionRing` с радиусом 34; `DecoyMark` и `DecoyPoint` без изменений (24, как на эталоне); обломки — модель disabled альфа 170. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | :68-73 `EvasionRing` | Параметр радиуса со значением по умолчанию 15. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopSpriteTests.cs` | — | Новый файл: кадр по курсу, множитель при 720/1080/1440, привязка к сетке, запасной силуэт. |

## Public API after the change

`internal static class TacticalShipSprites { bool Draw(SKCanvas c, TacticalUnitSnapshot u, SKPoint p, bool own, double course, byte alpha, int? factor, bool shadow); int Frame(double course); int Factor(float scale); }`.

## Implementation steps

1. Реализовать выбор модели, стороны и кадра.
2. Реализовать масштаб, привязку и тень.
3. Подключить в рендер юнитов и обломков; маркеры выбора, уклонения и ловушки.
4. Тесты UiSpec §10.6.

## Out of scope

Подписи, наведение (TK-0002).

## Invariants

В Classic рендер юнитов не меняется.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopSpriteTests`
- `Ep0019ViewTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopSpriteTests|FullyQualifiedName~Ep0019View" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us2-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC1, US2-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все числа — UiSpec §2; прототип показывает готовую реализацию отрисовки.

## Исполнение

Не начато.
