---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0008-anomaly-field
ticket: EP-0020-US-0008-TK-0001-anomaly-rules-content
title: Правила аномалий в данных
stage: done
layer: content-data
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0005-TK-0001-decoy-content]
files_touched: 6
serves: [US8-AC1]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Правила аномалий в данных

## Why

Все числа генерации и правил аномалий — данные V4, синхронные с JSON игры.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-23: числа US8-AC1 взяты из макета, где проверены визуально; их можно уточнять без нового решения пользователя.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `VData/Planning/V4.0/Data/README.md` | таблица `rules` | Строки `anomalies.*` после блока `decoy` и описание полей (ссылка на EP-0020). |
| `tools/tactical-battle/Export-Content.ps1` | :53-126 генерация `rules` | Проверить экспорт массивов-диапазонов; править только при необходимости. |
| `src/DeepSpaceSaga.Client/Data/TacticalBattle/rules.json` | генерируется | Только через экспортёр. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs` | :5-7 `TacticalRules` | `AnomalyRules(...)` по US8-AC1, параметр `TacticalRules.Anomalies` после `Decoy`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs` | `Validate` | Проверки US8-AC1. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentLoaderTests.cs` | — | Загрузка и отказы. |

## Public API after the change

`AnomalyRules`, `TacticalRules.Anomalies`.

## Implementation steps

1. Строки таблицы и экспорт.
2. Record и проверки загрузчика.
3. Тесты; `Export-Content.ps1 -Check`.

## Out of scope

Генератор и математика.

## Invariants

Прочие значения данных не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `ContentLoaderTests.Anomaly_rules_load_from_shipped_data`
- `ContentLoaderTests.Invalid_anomaly_rules_are_rejected`
- `ContentSyncTests.EveryMarkdownValueAndOrderedCatalogMatchesGameJson`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~ContentLoaderTests|FullyQualifiedName~ContentSyncTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us8-tk1" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs --verify-no-changes
pwsh -NoProfile -File tools/tactical-battle/Export-Content.ps1 -Check
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US8-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Значения заданы US8-AC1.

## Исполнение

В процессе. Техническое представление неуточнённого aiDustSearch: целое4, число равных шагов предела поворота; по A-29 это k=1…4 с шагом¼, порядок скоростей order/min/max фиксирован правилом. CenterSpread — вектор двух неотрицательных долей, не диапазон (его заданный порядок0.45,0.35 сохраняется). Spread/Sigma строго положительны для определённого Gaussian поля; counts/amplitude/clearance/maxAttempts допускают0. Параметры пользователя не меняются.

Первый прогон173/174: loader casesPASS, sync helper ранее поддерживал только int-массивы. Scope расширен на ContentSyncTests.Cell: сохраняется строгий GetInt32 для целых элементов, добавляется точное сравнение double для вещественных. Экспортёр массивы уже поддерживает; production exporter не меняется.

Завершено: Content174/174PASS, Export-Content -Check30JSONPASS, Engine build/scoped-format/tests-format/diff-checkPASS. Все15полей US8-AC1 сверены с литералами;46invalid cases покрывают missing/null, размер/порядок/знак диапазонов, integer shape, positive geometry, vector, fractions, counts иnonfinite. CenterSpread0.45/0.35 не сортируется. Прочие значения JSON не менялись; генерации/боевого эффекта ещё нет. Самопроверка data ownership/единиц/shape выполнена. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us8-tk1/final/tests.trx,export-check.log,format-engine.log,format-tests.log.
