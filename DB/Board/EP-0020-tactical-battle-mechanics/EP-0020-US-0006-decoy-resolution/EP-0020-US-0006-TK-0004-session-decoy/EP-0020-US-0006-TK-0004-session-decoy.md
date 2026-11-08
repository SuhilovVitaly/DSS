---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0006-decoy-resolution
ticket: EP-0020-US-0006-TK-0004-session-decoy
title: Сессия: приказ ловушки и скрытие до результатов
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0006-TK-0003-salvo-decoy]
files_touched: 4
serves: [US6-AC1, US6-AC5]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Сессия: приказ ловушки и скрытие до результатов

## Why

Игрок пускает ловушку через команду Fire; ловушки противника раскрываются только после залпа (D-19).

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-03 M1 сохраняется: дрон, пропущенный в запросе, не стреляет, не уклоняется и не пускает ловушку.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | `Fire`, `ResolveFire` после US-0002 TK-0004 | Валидация `Decoy` по US6-AC1; план игрока `(TargetId, Evade, Decoy)`; ловушки противника по правилу ИИ; журнал `DecoyLaunched` после фиксации планов. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :10 `TacticalBattleLogKind` | `DecoyLaunched` в конец. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SessionTests.cs` | — | Новые сценарии. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/JournalEventsTests.cs` | — | Событие пуска. |

## Public API after the change

`FireTacticalBattleRequest` принимает `Decoy`; причина отказа `DecoyUnavailable`; событие `DecoyLaunched`.

## Implementation steps

1. Валидация до мутаций.
2. Планы и правило ИИ в `ResolveFire`.
3. Журнал пусков (свои и противника — после разрешения залпа).
4. Тесты сессии: отказы, пуск игрока, скрытие ловушек противника в снимке фазы планирования, раскрытие после залпа.

## Out of scope

Клиент.

## Invariants

Запрос без `Decoy` обрабатывается как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `SessionTests.Decoy_order_is_validated_atomically`
- `SessionTests.Decoy_with_evade_is_rejected`
- `SessionTests.Enemy_decoys_are_hidden_until_salvo_resolves`
- `JournalEventsTests.Decoy_launch_is_logged`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~SessionTests|FullyQualifiedName~JournalEventsTests|FullyQualifiedName~LocalSessionBattleTests|FullyQualifiedName~ConnectionApiTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us6-tk4" --logger "trx;LogFileName=engine.trx"
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us6-tk4" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC1, US6-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Места правок указаны относительно результата US-0002 TK-0004.

## Исполнение

Не начато.
