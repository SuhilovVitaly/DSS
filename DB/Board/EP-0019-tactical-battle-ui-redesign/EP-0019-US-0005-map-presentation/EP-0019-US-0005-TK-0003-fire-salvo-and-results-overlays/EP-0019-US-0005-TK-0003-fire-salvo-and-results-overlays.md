---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0005-map-presentation
ticket: EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays
title: Слои стрельбы, залпа и результатов
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0005-TK-0002-movement-overlays]
files_touched: 3
serves: [US5-AC4]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Слои стрельбы, залпа и результатов

## Why

Стрельба показывает план выбранного стрелка, залп — полёт снаряда, результаты — потери.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Вспышки попадания/промаха и X обломка — прежняя логика допуска, оформление токенами (`Yellow`/`Red`, толщины как у стрелок).

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | :86-193 DrawSalvo/DrawFire/DrawTargetArrow, :247-267 DrawTrails | Заменить по DesignSpec §8.6–8.8. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | :84 Capture | Источник позиции снаряда. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs, BattleMapRenderTests.cs` | — | Растровые проверки. |

## Public API after the change

No public API change.

## Implementation steps

1. Следы `Paths` пунктиром, сектор выбранного стрелка, стрелки и шанс (зарезервировать прямоугольник шанса).
2. Снаряд: пунктир плана, градиентный след, свечение и ядро по доле полёта.
3. Выноска `−N брони` (подбор стороны без маркеров) и кольцо-ромб.
4. Тесты: пробы карты сцен `2-fire`, `3-salvo-flight`, `4-salvo-results` (§12.4) и положение выноски в модели раскладки.

## Out of scope

Камера, панели.

## Invariants

Расписание залпа и допуск результатов — `TacticalBattleResults`.

## Tests (исходный план; актуальные проверки ниже)

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleMapRenderTests.Fire_overlays_follow_design_spec`
- `ResultsPhaseTests.Projectile_at_62_percent_follows_design_spec`
- `ResultsPhaseTests.Results_callout_follows_design_spec`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleMapRenderTests|FullyQualifiedName~ResultsPhaseTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us5-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Геометрия — DesignSpec §8.6–8.8.

## Исполнение

См. проверенное исполнение ниже.

## Проверенное исполнение 2026-11-08 (продолжение)

Draw selected fire plans projectile flight and admitted damage callouts

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us5-tk3. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.

## Фактическая привязка проверок

MapFireTests; ResultsPhaseTests. Плановые имена тестов заменены реализацией тех же AC в указанных owning classes; результаты и paths — ExecutionStatus.md. Source scope разнесён по partial-компонентам overlay/map/phase для сохранения session ownership; никакой Engine-side реализации не добавлялось.
