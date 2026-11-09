---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0006-decoy-resolution
ticket: EP-0020-US-0006-TK-0005-decoy-regression
title: Регрессия боя с ловушками и отчёт
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0006-TK-0004-session-decoy]
files_touched: 3
serves: [US6-AC6]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Регрессия боя с ловушками и отчёт

## Why

Ловушки ИИ меняют ход боёв. Нужно доказать совпадение путей исполнения, осознанно обновить ожидания и показать пользователю, насколько ловушка сильна (ответ «ловушка должна давать очень существенный бонус»).

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Числа исторических боёв не являются оракулом новой механики (`GoldenBattleTests.cs:15`). Отчёт не меняет данные.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs` | :23-29, :32-55, :57-88 | Явный путь использует правило ловушек ИИ; обновить ожидаемые числа с записью причины. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs` | :13-53 | Подписи серий после изменения; прямой = явный. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DecoyReportTests.cs` | нет | Тест с `Trait("Category","Balance")`: 1000 сидов × 5 тактик через `TacticalBattle.Run`; `decoy-report.csv` в `DSS_VALIDATION_ROOT/ep0020-decoy/`. |

## Public API after the change

No API change.

## Implementation steps

1. Прогнать весь `DeepSpaceSaga.Engine.Tests`.
2. Обновить ожидания только там, где изменение объяснено ловушками ИИ; список причин записать.
3. Сохранить подписи серий до и после в artifacts.
4. Прогнать отчёт: пусков за бой, доля поглотивших выстрел, средний шанс поглощённых выстрелов до и после, доля промахов; перенести таблицу в «Исполнение» и в review log эпика как информацию для пользователя.

## Out of scope

Изменение чисел модуля.

## Invariants

Прямой и явный пути совпадают; обычный прогон тестов не запускает отчёт (категория `Balance`).

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `GoldenBattleTests.NewRuleBattlesRepeatAcrossDirectAndExplicitPaths`
- `SeriesParityTests`
- `DecoyReportTests.Report_decoy_effect_over_series`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle|FullyQualifiedName~DecoyReportTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us6-tk5" --logger "trx;LogFileName=engine.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC6 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Затронутые тесты и содержание отчёта перечислены.

## Исполнение

В процессе. Golden signature теперь включает все счётчики/суммы ловушек. Scope расширен на DecoyState M1 hash: исключает только пять новых нулевых полей статистики для сравнения старой формы, проверяя все прежние данные. Before-подписи сохранены из фактического US5/TK2after (7895e0b9), до реализации эффекта ловушек; новые after вычисляются текущим кодом.


### M2: измерение эффекта ловушек

5000 боёв через production Run,1000сидов×5тактик. Параметры модуля1.5/2заряда и порогИИ0.4 не менялись.

| Тактика | Пусков/бой | Сработавших | Шанс до | Шанс после | Промахов |
|---|---|---|---|---|---|
| dogfight | 12.142 | 95.25% | 59.07% | 38.68% | 60.48% |
| raid | 13.172 | 94.47% | 60.44% | 40.79% | 58.95% |
| defence | 12.978 | 96.45% | 58.71% | 38.11% | 61.70% |
| balanced | 13.442 | 96.47% | 58.49% | 37.90% | 62.28% |
| commander | 13.042 | 96.63% | 58.46% | 37.85% | 62.14% |

Всего64776пусков, 62097выстрелов с эффектом; средний шанс59.03%→38.66%, ожидаемая частота попаданий снижена на34.51% на этих выстрелах. Это измерение initial scenario и выбранного AI, не универсальное обещание баланса. «Сработавшая» ловушка означает первый фактический выстрел, даже если он попал. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us6-tk5/report/ep0020-decoy/decoy-report.csv и aggregate.json. Полная механическая регрессия завершена.

Завершено: ordinary Engine2850/2850 unique IDsPASS(main2220+corpus630,36TRX), no duplicates/failed/skipped; series5/5(5000 конфигураций,10000 исполнений) exactPASS; Balance report1/1(5000 Run)PASS. Default категория Balance сохраняет отчёт вне обычной suite. Build/scoped format/diff-checkPASS. Golden explicit AI обновлён TK3; результаты Session/Local oracle обновлены TK4 под фактический контроль игрока. Другие старые ожидания не менялись. Самопроверка всей US6 по AC1…AC6, атомарности, RNG, forecast, identity, журналу и world save boundary выполнена. UI consumption следует US7. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us6-tk5/coverage-audit.json,series-comparison.json,before/,after/,report/tests.trx,format-tests.log.
