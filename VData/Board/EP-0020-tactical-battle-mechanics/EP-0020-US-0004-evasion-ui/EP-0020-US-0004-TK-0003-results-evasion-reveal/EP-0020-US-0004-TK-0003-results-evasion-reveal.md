---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0004-evasion-ui
ticket: EP-0020-US-0004-TK-0003-results-evasion-reveal
title: Раскрытие уклонения в залпе и результатах
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0004-TK-0002-fire-panel-evasion]
files_touched: 6
serves: [US4-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Раскрытие уклонения в залпе и результатах

## Why

Игрок узнаёт об уклонении противника только после залпа (D-08).

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Источник — шаги `FireSteps` с `SkipReason.Evading` текущего залпа. Свой уклонившийся дрон в результатах получает значок «уклонение» вместо «не стрелял(а)».

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | :58-82 Project, :84-100 Capture | Множество уклонившихся текущего залпа. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | кольцо из TK-0002 | Рисовать для уклонившихся в полёте залпа и результатах. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs` | EP-0019: карточка наведения | Значок «Уклонение» UiSpec §5 вместо состояния. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs` | :19, :47 SkipText | `TacticalBattle.Skip.Evading`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | проекция результатов | Значок «уклонение» для своего уклонившегося (UiSpec §4). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | — | Раскрытие только после разрешения. |

## Public API after the change

No public API change.

## Implementation steps

1. Проекция уклонившихся.
2. Кольцо, карточка наведения, журнал, карточка пилота.
3. Проверка: в фазе стрельбы противник с будущим уклонением не помечен; в полёте и результатах помечен.

## Out of scope

Новые регрессионные снимки результатов.

## Invariants

Сцены залпа и результатов EP-0019 без уклонения не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `ResultsPhaseTests.Enemy_evasion_is_revealed_only_after_salvo`
- `ResultsPhaseTests.Own_evaded_drone_card_shows_evasion`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ResultsPhaseTests|FullyQualifiedName~SidesAndLogTests|FullyQualifiedName~Ep0019ViewTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us4-tk3" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Источник данных и вид заданы.

## Исполнение

Не начато.
