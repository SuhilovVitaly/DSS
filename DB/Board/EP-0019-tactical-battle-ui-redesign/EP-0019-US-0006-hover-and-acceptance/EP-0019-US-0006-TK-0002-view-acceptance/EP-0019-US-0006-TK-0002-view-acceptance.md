---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0006-hover-and-acceptance
ticket: EP-0019-US-0006-TK-0002-view-acceptance
title: Приёмка вида семи сцен
stage: done
layer: tests
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0006-TK-0001-hover-hit-test-and-card]
files_touched: 2
serves: [US6-AC2]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Приёмка вида семи сцен

## Why

Критерий D-08: экран выглядит точно по спецификации на любой машине.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Расхождения устраняются в production-коде. Изменение ожиданий DesignSpec, допусков и проб без решения пользователя запрещено.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019ViewTests.cs` | `Skip` на проверках вида | Снять `Skip`; добавить сохранение и сверку регрессионных снимков §12.5. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/*` | — | Исправления расхождений, найденных сравнением. |

## Public API after the change

No public API change.

## Implementation steps

1. Снять `Skip`, прогнать семь сцен, изучить `probes.json` и `layout.json`.
2. Исправить расхождения в коде экрана; повторять до прохождения.
3. Сохранить регрессионные снимки семи сцен в тестовом проекте; включить их сверку (§12.5).
4. Прогнать полный тактический набор Client и сохранить рендеры сцен в artifacts для визуальной приёмки пользователем.
5. Записать в ExecutionStatus результаты проб по каждой сцене.
6. Если доступна вторая ОС из трёх (Windows, macOS, Ubuntu), прогнать проверки вида и сверку регрессионных снимков и там. Если текст выходит за допуск только на одной ОС, перейти на отрисовку текста контурами (DesignSpec §2 «Шрифт», последний пункт) и повторить прогон на всех доступных ОС.

## Out of scope

Новые сцены.

## Invariants

Ожидания DesignSpec не подгоняются под рендер.

## Tests (исходный план; актуальные проверки ниже)

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `Ep0019ViewTests.Scene_layout_matches_design_spec (7)`
- `Ep0019ViewTests.Scene_texts_match_design_spec (7)`
- `Ep0019ViewTests.Scene_probes_pass (7)`
- `Ep0019ViewTests.Scene_matches_regression_snapshot (7)`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0019ViewTests|FullyQualifiedName~TacticalBattle" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us6-tk2" --logger "trx;LogFileName=focused.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Критерий и допуски — US1-AC4 и DesignSpec §12.

## Исполнение

См. проверенное исполнение ниже.

Scope приёмки: независимые ожидания в Ep0019Acceptance.cs, миграция legacy UI tests (удалённые вкладки/флажки заменяются проверками нового контракта, сохраняются timing/session/SaveLoad assertions), обновление native tooling workflow, regression PNG. Снижение alpha disabled отменено в пользу нормативного §8.2/§12.4: Outcome действительно содержит disabled Веру, а пробы требуют Cyan. Не меняются проба или допуск.

## Проверенное исполнение 2026-11-08 (продолжение)

Accept seven scenes with regression snapshots full Client and secondary monitor native checks

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/acceptance-final. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.

## Фактическая привязка проверок

Ep0019ViewTests; tactical Client; full Client; OverlayBoundaryTests; native workflow. Плановые имена тестов заменены реализацией тех же AC в указанных owning classes; результаты и paths — ExecutionStatus.md. Source scope разнесён по partial-компонентам overlay/map/phase для сохранения session ownership; никакой Engine-side реализации не добавлялось.
