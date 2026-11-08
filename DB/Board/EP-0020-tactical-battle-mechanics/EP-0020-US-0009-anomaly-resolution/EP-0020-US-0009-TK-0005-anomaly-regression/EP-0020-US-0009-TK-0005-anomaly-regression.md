---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0009-anomaly-resolution
ticket: EP-0020-US-0009-TK-0005-anomaly-regression
title: Регрессия боя с аномалиями и отчёт
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0009-TK-0004-session-anomalies]
files_touched: 3
serves: [US9-AC6]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Регрессия боя с аномалиями и отчёт

## Why

Аномалии меняют все бои. Нужно доказать совпадение путей, осознанно обновить ожидания и показать влияние аномалий.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Числа исторических боёв не являются оракулом новой механики. Отчёт не меняет данные.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs` | :23-88 | Обновить ожидаемые числа с записью причины. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs` | :13-53 | Подписи серий; прямой = явный. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AnomalyReportTests.cs` | нет | `Trait("Category","Balance")`: отчёт US9-AC6 в `DSS_VALIDATION_ROOT/ep0020-anomalies/`. |

## Public API after the change

No API change.

## Implementation steps

1. Прогнать `DeepSpaceSaga.Engine.Tests`.
2. Обновить ожидания только по причинам «аномалии»; список записать.
3. Подписи серий до и после в artifacts.
4. Отчёт; таблицу — в «Исполнение» и review log эпика.

## Out of scope

Изменение чисел.

## Invariants

Прямой и явный пути совпадают.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `GoldenBattleTests.NewRuleBattlesRepeatAcrossDirectAndExplicitPaths`
- `SeriesParityTests`
- `AnomalyReportTests.Report_anomaly_effect_over_series`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle|FullyQualifiedName~AnomalyReportTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us9-tk5" --logger "trx;LogFileName=engine.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC6 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Содержание отчёта задано.

## Исполнение

В процессе. Scope включает legacy fixtures AssemblyTests.WithoutModules/DecoyStateTests и AnomalyGeneratorTests: после подключения эффекта поля прежние трассы сравниваются на пустом списке аномалий, а не на новой механике; literal hashes сохраняются. Новый M2 no-anomaly oracle берётся из реально сохранённых US8/TK3 before signatures, удаляется только добавленное DTO InDust=false. Golden Signature расширяется Absorbed/DustSkips; для старых хешей удаляются лишь эти новые metadata. Длинные серии и отчёт делятся на5тестовых процессов по тактике, каждый с cap300000ms.

### Отчёт5000боёв (1000сидов×5тактик)

| tactic | Heat/Dust mean | crossing shots | mean absorption (all/crossing) | absorbed shots | dust A/B | mean turns | W/L/Draw/Timeout |
|---|---|---|---|---|---|---|---|
| dogfight | 2.041/1.531 | 10.617% | 4.045%/38.099% | 4.034% | 1.941%/2.566% | 23.097 | 954/45/0/1 |
| raid | 2.041/1.531 | 10.939% | 4.058%/37.098% | 4.105% | 2.245%/2.052% | 23.524 | 740/260/0/0 |
| defence | 2.041/1.531 | 7.567% | 2.721%/35.961% | 2.748% | 1.006%/2.121% | 30.046 | 906/90/0/4 |
| balanced | 2.041/1.531 | 9.997% | 3.753%/37.541% | 3.714% | 1.512%/2.007% | 28.488 | 910/87/0/3 |
| commander | 2.041/1.531 | 9.219% | 3.310%/35.903% | 3.341% | 1.518%/2.216% | 28.651 | 956/40/0/4 |

Доля crossing/absorbed — от всех фактических выстрелов; mean absorption отдельно от всех/crossing. Dust — активные дроно-ходы на позиции фиксации огня после движения по предложениям ИИ; A — сторона игрока (предложения), B — ИИ. Это измерение геометрии, не только DustSkips: EV в пыли тоже входит. Данные и баланс не менялись. Evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us9-tk5/anomaly-report.json`, индивидуальные report-*/ep0020-anomalies JSON/CSV.

Review расширение scope: полный Motion1282PASS/1FAIL выявил общий ForbiddenCalls scanner. База601ab уже содержит15попаданий в7файлах (read-only source evidence, не baseline test run). Новые арифметические участки эпика приводятся к explicit-double и PyMath, membership-проверки snapshot используют list/array; прежние UI находки не маскируются. После эквивалентных правок повторяются затронутая математика/Engine/серии; исходные успешные отчёты остаются как история. Evidence preexisting-motion-guard.json и motion/tests.trx.

Завершено. Engine2924/2924 uniquePASS: main2294послеreview+35corpus×18=630,36TRX,no failed/skipped/duplicate; coverage-audit.json. Corpus geography production/tests не затронуты review-правками боевой арифметики. Golden90+ручной1 входят в main; Series5000direct/explicitPASS, report5000PASS. Послеreview5000подписей побайтово совпали с pre-review (series-review-comparison.json), поэтому цифры отчёта сохранены. Legacy M1/M2 literal hashes всех5тактик PASS при пустых полях; правила/параметры не менялись. Contracts197/197PASS. ПолныйMotion1282PASS/1FAIL широкого ForbiddenCalls sourceguard (review-motion/tests.trx): новые Core операции исправлены; прежние UI находки остаются открытыми, общий Motion PASS не заявляется. Генератор1000seed проходит без DSS_VALIDATION_ROOT (no-evidence-env/tests.trx); evidence пишется только при явно заданном каталоге. Scopedformat/diffcheckPASS. Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us9-tk5/`. Самопроверка AC1–6/RNG/атомарности/Save20 завершена; независимая/human/native приёмка не подменяется.
