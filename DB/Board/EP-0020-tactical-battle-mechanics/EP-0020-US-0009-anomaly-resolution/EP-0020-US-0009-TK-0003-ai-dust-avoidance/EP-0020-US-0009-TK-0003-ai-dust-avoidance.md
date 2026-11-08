---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0009-anomaly-resolution
ticket: EP-0020-US-0009-TK-0003-ai-dust-avoidance
title: ИИ: не заканчивать ход в пыли
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0009-TK-0002-salvo-anomalies]
files_touched: 3
serves: [US9-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# ИИ: не заканчивать ход в пыли

## Why

Без правила ИИ будет случайно «глохнуть» в пыли (D-32).

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-29: сетка `aiDustSearch` — повороты `предложенный ± k·¼·предел` для k = 1…4 (сначала +, затем −), на скорости приказа; затем те же повороты на min и max скорости; первый с концом пути (`TacticalGeometry.Path`) вне пыли. Правило применяется после `TacticalTail.Apply` ко всем дронам ИИ (в сессии — к предложениям обеих сторон, игрок может их изменить).

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalCommander.cs` | :98-124 `TacticalBattleAi.Plan` | После `TacticalTail.Apply`: `TacticalDustAvoidance.Apply(battle, drone, order)`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalDustAvoidance.cs` | нет | Правило A-29. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AnomalyAiTests.cs` | нет | Тесты. |

## Public API after the change

`TacticalDustAvoidance.Apply(TacticalBattle, TacticalDrone, TacticalOrder)`.

## Implementation steps

1. Реализовать правило.
2. Тесты: конец в пыли с кандидатами — заменён; без кандидатов — прежний; нет выхода — прежний; бросков RNG нет.

## Out of scope

Выбор цели.

## Invariants

Без аномалий приказы ИИ побитно прежние.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `AnomalyAiTests.Ai_moves_end_out_of_dust_when_it_would_have_targets`
- `AnomalyAiTests.Ai_keeps_order_without_targets_or_exit`
- `AnomalyAiTests.Dust_avoidance_draws_no_random`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~AnomalyAiTests|FullyQualifiedName~SessionTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us9-tk3" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalCommander.cs src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalDustAvoidance.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Сетка поиска задана.

## Исполнение

Не начато.
