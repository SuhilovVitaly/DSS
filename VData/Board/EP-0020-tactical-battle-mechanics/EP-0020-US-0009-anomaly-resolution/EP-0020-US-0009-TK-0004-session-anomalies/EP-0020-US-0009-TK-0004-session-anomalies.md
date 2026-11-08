---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0009-anomaly-resolution
ticket: EP-0020-US-0009-TK-0004-session-anomalies
title: Сессия: отказ в пыли и журнал
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0009-TK-0003-ai-dust-avoidance]
files_touched: 3
serves: [US9-AC3, US9-AC5]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Сессия: отказ в пыли и журнал

## Why

Приказ огня дрону в пыли отвергается атомарно; поглощения видны в журнале.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Фаза стрельбы открывается по прежнему правилу M1 (D-10); дрон в пыли не считается имеющим цели.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | `Fire`, `ResolveFire` после US-0006 TK-0004 | Отказ `InDust` для приказа с целью; журнал `ShotAbsorbed`. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SessionTests.cs` | — | Сценарии. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/JournalEventsTests.cs` | — | Событие поглощения. |

## Public API after the change

Причина отказа `InDust`.

## Implementation steps

1. Валидация.
2. Журнал.
3. Тесты: отказ, уклонение и ловушка из пыли разрешены, журнал.

## Out of scope

Клиент.

## Invariants

Запросы без целей у дронов в пыли обрабатываются как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `SessionTests.Fire_order_from_dust_is_rejected`
- `SessionTests.Dust_drone_may_evade_or_launch_decoy`
- `JournalEventsTests.Absorbed_shot_is_logged`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~SessionTests|FullyQualifiedName~JournalEventsTests|FullyQualifiedName~LocalSessionBattleTests|FullyQualifiedName~ConnectionApiTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us9-tk4" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC3, US9-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Места правок указаны.

## Исполнение

Не начато.
