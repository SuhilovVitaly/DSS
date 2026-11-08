---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0001-reference-and-golden-harness
ticket: EP-0019-US-0001-TK-0003-scene-harness
title: Харнесс сцен и проверки вида
stage: blocked
layer: tests
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0001-TK-0002-bundle-battle-font]
files_touched: 6
serves: [US1-AC3, US1-AC4, US1-AC5]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Харнесс сцен и проверки вида

## Why

Главный критерий эпика проверяется автоматически. Харнесс нужен до переделки экрана, чтобы каждый следующий тикет видел, какие проверки DesignSpec уже выполняются.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-01, A-06. Мир на паузе. Приказы — предложения ИИ, цели — `DefaultTargetId`. Сцена `3-salvo-flight`: момент, когда первый выстрел пролетел 62 % (`TacticalBattleResults` даёт долю полёта; часы подбираются вычислением, не перебором кадров).

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs` | нет | Построение сцен на `BattleNavigationTests.Fixture`: Route Open/Start/Move/Fire, `Localization.UseLocaleForTests("Russian")`, `AdvanceAnimation`/`FinishPresentation`, установка наведения (до US-0006 — через internal-свойство экрана `HoverUnitId`, которое US-0006 подключит к мыши). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/RasterProbe.cs` | нет | Пробы DesignSpec §12.4: область в единицах макета → пиксели при `S = 1,5`, допуск 40 по каналу, доля; запись `probes.json`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019ViewTests.cs` | нет | Тесты данных (выполняются), раскладки, строк и проб семи сцен (`Skip` до готовности историй, `Trait("Category","Ep0019View")`). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/battle-data-seed1.txt` | нет | Копия `Assets/battle-data-seed1.txt` эпика. |
| `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj` | — | Копировать данные в output. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :44-57 internal test hooks | Добавить `internal string? HoverUnitId { get; set; }` (пока не рисуется) и `internal BattleLayout Layout` — модель раскладки последнего кадра (прямоугольники и камера, DesignSpec §12.2). |

## Public API after the change

Только тестовая сборка; в клиенте — internal `HoverUnitId` и `Layout`.

## Implementation steps

1. Скопировать данные снимков, добавить копирование в csproj тестов.
2. Реализовать сцены DesignSpec §12.1. Для `5-outcome` доигрывать бой предложениями ИИ до `Phase == Over` (ожидается ход 20, победа).
3. Рендер: `screen.Render(surface.Canvas, 1920, 1080)` в растровый `SKSurface`.
4. Проверка данных: позы (±0,05), корпус/броня, предложения, цели и шансы, `FireSteps` ходов 1 и 3 из `battle-data-seed1.txt`.
5. Записать в коде тестов таблицы ожиданий DesignSpec §12.2–12.4 (числа и строки переносятся из DesignSpec, не из рендера).
6. Проверки вида оставить `Skip = "Enabled by <ticket>"` по историям; убедиться, что при снятом Skip они выполняются и пишут отчёты (ожидаемо красные).

## Out of scope

Изменение вида экрана.

## Invariants

Харнесс не меняет данные боя и не тратит RNG сверх реальных команд.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `Ep0019ViewTests.Seed1_battle_data_matches_assets`
- `Ep0019ViewTests.Scene_layout_matches_design_spec (7, Skip)`
- `Ep0019ViewTests.Scene_texts_match_design_spec (7, Skip)`
- `Ep0019ViewTests.Scene_probes_pass (7, Skip)`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0019ViewTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us1-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Scenes.cs tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/RasterProbe.cs tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019ViewTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC3, US1-AC4, US1-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Все ожидания — в DesignSpec §12; фикстура уже существует.

## Исполнение

2026-11-08: BLOCKED до реализации харнесса. Обязательный Assets/battle-data-seed1.txt отсутствует в HEAD f9fcbd45, свежем origin/main 50d96441, всей доступной Git-истории и локальных workspace artifacts. В Assets отслеживался только README; шрифты отдельно восстановлены TK-0002 по совпавшим SHA, но независимый oracle боя не восстановлен.

DesignSpec §12.1 и шаги 1/4 этого тикета требуют именно reference с позами (±0.05), приказами, целями/шансами и FireSteps ходов 1 и 3. §12.3 содержит только часть этих величин. Запись текущего Engine output как ожидаемых значений не доказывает совпадение с потерянным reference и не выполняет обязательную сверку данных до вида. Значения и допуски не изменены; фиктивного Seed1_battle_data_matches_assets нет. Нужен исходный reference из сессии планирования либо его независимая восстановленная версия с проверяемым происхождением.

Проверено: source tree/hash и Git history, поиск workspace (включая ignored artifacts), актуальный origin после fetch. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-setup/input-audit.txt. Следующие 18 тикетов зависимы от этого харнесса, поэтому их production-реализация не начата. US-0007 дополнена scope, но не выполнена. См. ExecutionStatus.md.
