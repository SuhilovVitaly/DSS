---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0008-anomaly-field
ticket: EP-0020-US-0008-TK-0001-anomaly-rules-content
title: Правила аномалий в данных
stage: draft
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

Не начато.
