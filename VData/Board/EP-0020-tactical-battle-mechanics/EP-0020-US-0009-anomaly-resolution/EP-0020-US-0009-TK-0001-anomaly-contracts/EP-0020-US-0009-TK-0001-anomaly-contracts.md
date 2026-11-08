---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0009-anomaly-resolution
ticket: EP-0020-US-0009-TK-0001-anomaly-contracts
title: Контракты поглощения и пыли
stage: draft
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0020-US-0008-TK-0003-anomaly-generator]
files_touched: 3
serves: [US9-AC3, US9-AC5]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Контракты поглощения и пыли

## Why

Клиент должен заранее видеть долю поглощения и признак «в пыли», а после залпа — поглощённые выстрелы.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Новые значения перечислений — в конец после значений M1/M2.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :17-21 `TacticalFireSkipReason`, :10 `TacticalBattleLogKind`, :35-56 | `InDust`; `ShotAbsorbed`; поля US9-AC5 (`double Absorption {init;}`, `bool Absorbed {init;}`, `bool InDust {init;}`). |
| `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs` | :17-24 причины отказа | `InDust` в конец. |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | :46-92, :192 | Сериализация, старые нагрузки, id перечислений. |

## Public API after the change

Как в столбце Allowed change.

## Implementation steps

1. Добавить поля и значения.
2. Тесты.

## Out of scope

Логика.

## Invariants

Старые нагрузки и id читаются как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Contracts.Tests`. Именованные проверки:

- `TacticalBattleContractTests.Anomaly_fields_round_trip`
- `TacticalBattleContractTests.OldPayloadsWithoutAnomalyFieldsRemainReadable`
- `TacticalBattleContractTests.OldTacticalEventsAndEnumIdsRemainReadable`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us9-tk1" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --include src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC3, US9-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Типы перечислены.

## Исполнение

Не начато.
