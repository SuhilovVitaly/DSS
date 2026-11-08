---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0002-overlay-layout-shell
ticket: EP-0019-US-0002-TK-0004-localization-strings
title: Строки RU/EN нового интерфейса
stage: done
layer: content-data
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0002-TK-0002-full-screen-map-shell]
files_touched: 3
serves: [US2-AC5]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Строки RU/EN нового интерфейса

## Why

Все тексты DesignSpec должны идти через локализацию; английский — тот же смысл.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-06. Ключи — под `TacticalBattle.Ui.*`. Существующие ключи с тем же смыслом переиспользуются, если их RU-текст совпадает с DesignSpec дословно.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | 200 ключей `TacticalBattle.*` | Добавить ключи для всех строк DesignSpec §5–7 и §9 (теги фаз, заголовки, подписи, кнопки, строки фазы, подсказки, итог); RU — дословно как в DesignSpec. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | то же | EN-значения (таблица ниже). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs, GlyphCoverageTests.cs` | паритет ключей и глифов | Должны пройти без изменения логики. |

## Public API after the change

No API change.

## Implementation steps

1. Выписать все строки DesignSpec §5–7 и §12.3, заменить числа плейсхолдерами `{0}`.
2. Добавить RU и EN значения. EN-ориентир: ДВИЖЕНИЕ→MOVEMENT, СТРЕЛЬБА→FIRE, ЗАЛП→SALVO, РЕЗУЛЬТАТЫ→RESULTS, ИТОГ→OUTCOME, ХОД→TURN, ПЛАН ДВИЖЕНИЯ→MOVE PLAN, ПЛАН ВЫСТРЕЛА→SHOT PLAN, ВЫСТРЕЛ В ПОЛЁТЕ→SHOT IN FLIGHT, РЕЗУЛЬТАТ ВЫСТРЕЛА→SHOT RESULT, ИТОГ БОЯ→BATTLE OUTCOME, Броня→Armor, Корпус→Hull, Сознание→Mind, ПОД КУРСОРОМ · ПРОТИВНИК→UNDER CURSOR · ENEMY, ПИЛОТ→PILOT, Конец пути→Path end.
3. Прогнать покрытие локализации и глифов.

## Out of scope

Перевод остальных экранов.

## Invariants

Существующие ключи, используемые другими экранами, не меняются.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `LocalizationCoverageTests`
- `GlyphCoverageTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalizationCoverageTests|FullyQualifiedName~GlyphCoverageTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us2-tk4" --logger "trx;LogFileName=focused.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Источник строк — DesignSpec.

## Исполнение

Не начато.

## Проверенное исполнение 2026-11-08 (продолжение)

Localize redesigned battle controls and validate RU EN placeholder parity

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us2-tk4. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.
