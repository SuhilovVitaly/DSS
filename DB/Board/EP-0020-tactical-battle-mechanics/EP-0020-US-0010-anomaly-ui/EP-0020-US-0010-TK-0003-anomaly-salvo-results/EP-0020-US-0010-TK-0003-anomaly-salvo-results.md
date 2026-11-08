---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0010-anomaly-ui
ticket: EP-0020-US-0010-TK-0003-anomaly-salvo-results
title: Аномалии в залпе, результатах и карточке наведения
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0010-TK-0002-anomaly-move-fire]
files_touched: 5
serves: [US10-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Аномалии в залпе, результатах и карточке наведения

## Why

Поглощение должно быть объяснено после залпа, а аномалия — по наведению.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Точка поглощения на карте — середина первого интервала `Segments` линии огня (A-31).

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs` | после US-0007 TK-0003 | §19.1, §19.2. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | выноски результатов | §19.3. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs` | после US-0007 TK-0003 | §19.4: наведение на аномалию (курсор внутри зоны и не над юнитом). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs` | журнал | `ShotAbsorbed`, пропуск `InDust`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | — | Поглощённый выстрел. |

## Public API after the change

No public API change.

## Implementation steps

1. Очерёдность огня и результат.
2. Вспышка и выноска.
3. Карточка наведения аномалии.
4. Журнал.
5. Тесты.

## Out of scope

Регрессионные снимки.

## Invariants

Выстрелы без поглощения выглядят как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `ResultsPhaseTests.Absorbed_shot_result_and_callout`
- `ResultsPhaseTests.Queue_shows_effective_chance`
- `HoverCardTests.Anomaly_hover_card`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ResultsPhaseTests|FullyQualifiedName~HoverCardTests|FullyQualifiedName~SidesAndLogTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us10-tk3" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US10-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Вид задан UiSpec §19.

## Исполнение

Не начато.
