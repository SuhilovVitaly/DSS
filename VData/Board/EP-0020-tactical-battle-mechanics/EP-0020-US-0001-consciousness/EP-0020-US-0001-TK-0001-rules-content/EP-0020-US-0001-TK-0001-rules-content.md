---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0001-consciousness
ticket: EP-0020-US-0001-TK-0001-rules-content
title: Правила сознания и уклонения в данных
stage: done
layer: content-data
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: []
files_touched: 6
serves: [US1-AC1]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Правила сознания и уклонения в данных

## Why

Все числа механики живут в данных, а не в коде (`rules.json`), и синхронны с таблицей V4 Data, иначе `ContentSyncTests` и экспортёр упадут.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-08. Порог ИИ хранится в `rules.json` (блок `evasion`), а не в `ai.json`: так все числа механики лежат в одном блоке, а загрузчик `ai.json` с белым списком из 9 секций не меняется.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/TacticalBattle/rules.json` | блоки hit…systems, `seriesTurnCap` | Добавить перед `seriesTurnCap`: `"consciousness": {"start": 100, "max": 100, "recoveryPerTurn": 5}`, `"evasion": {"hitPenalty": 20, "cost": 10, "minConsciousness": 10, "aiHullFraction": 0.5}`. |
| `VData/Planning/V4.0/Data/README.md` | :49-76 описание и таблица `rules` | Строки `consciousness.*`, `evasion.*` в том же порядке и пояснения (ссылка на EP-0020). |
| `tools/tactical-battle/Export-Content.ps1` | :53-126 генерация `rules`/`ai` из таблиц | Проверить, что новые строки экспортируются без правки; править только при необходимости. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs` | :5-10 `TacticalRules`, `HitRules` | Новые records `ConsciousnessRules(int Start, int Max, int RecoveryPerTurn)`, `EvasionRules(double HitPenalty, int Cost, int MinConsciousness, double AiHullFraction)`; параметры `TacticalRules` после `Systems`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs` | :205-221 `Validate` | Проверки диапазонов US1-AC1. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentLoaderTests.cs` | — | Отказы загрузчика на каждое нарушение. |

## Public API after the change

`TacticalRules(..., SystemRules Systems, ConsciousnessRules Consciousness, EvasionRules Evasion, int SeriesTurnCap)`.

## Implementation steps

1. Добавить блоки в `rules.json` и строки в таблицу V4 Data (значения D-05, D-07, D-12).
2. Добавить records и параметры `TacticalRules`; `ValidateShape` требует оба блока.
3. Добавить проверки диапазонов и тесты отказов.
4. Запустить `tools/tactical-battle/Export-Content.ps1 -Check` и `ContentSyncTests`.

## Out of scope

Использование правил в бою.

## Invariants

Остальные значения `rules.json` и `ai.json` не меняются; порядок листьев JSON совпадает с порядком строк таблицы.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `ContentLoaderTests.Evasion_and_consciousness_rules_load_from_shipped_data`
- `ContentLoaderTests.Invalid_evasion_or_consciousness_values_are_rejected`
- `ContentSyncTests.EveryMarkdownValueAndOrderedCatalogMatchesGameJson`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~ContentLoaderTests|FullyQualifiedName~ContentSyncTests|FullyQualifiedName~DefinitionsTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us1-tk1" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs --verify-no-changes
pwsh -NoProfile -File tools/tactical-battle/Export-Content.ps1 -Check
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Значения заданы решениями пользователя; таблица и JSON меняются вместе.

## Исполнение

2026-11-08: реализованы оба обязательных блока, records и диапазоны; 12 отрицательных случаев и проверка поставленных значений. US1-AC1 покрыт. Экспортёр 29/29 JSON PASS, ContentLoaderTests/ContentSyncTests/DefinitionsTests 118/118 PASS (build включён). Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us1-tk1/engine.trx и tests.log. Scoped format: после нормализации CRLF исправлена существовавшая whitespace-строка loader (без смены поведения). Self-review выполнен: неизменённые числа/каталог, обязательность/типизация полей, атомарный отказ загрузки; Save20/RNG не затронуты. git diff --check PASS. Публикация сверяется по Git после commit.
