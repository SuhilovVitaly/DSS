---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0011-build-slots
ticket: EP-0020-US-0011-TK-0001-slot-contracts
title: Контракт слотов
stage: done
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0020-US-0009-TK-0005-anomaly-regression]
files_touched: 2
serves: [US11-AC1]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Контракт слотов

## Why

Клиенту нужна структура рамы одним списком.

## Decisions

Применяются [D-34–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Новые перечисления — отдельные типы; числовые id с 0 в порядке US11-AC1.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :30-41 | Типы и поля US11-AC1. |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | :46-92 | Сериализация и чтение старых нагрузок. |

## Public API after the change

Как в US11-AC1.

## Implementation steps

1. Добавить типы и поля.
2. Тесты.

## Out of scope

Заполнение.

## Invariants

Старые нагрузки читаются как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Contracts.Tests`. Именованные проверки:

- `TacticalBattleContractTests.Slots_round_trip`
- `TacticalBattleContractTests.OldPayloadsWithoutSlotsRemainReadable`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us11-tk1" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --include src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US11-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Типы перечислены.

## Исполнение

Завершено. Slots/EngineSlots/ModuleSlots добавлены init defaults[]/0/0, SlotKind0..4 иSlotState0..2 отдельнымиenum; ModuleId/Namenullable, Factor1поумолчанию. Contracts32/32PASS, build/scopedformat/diffcheckPASS. Evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us11-tk1/contracts/tests.trx`. AC1 выполнен; старые нагрузки читаются. Самопроверка совместимости пройдена, заполнение вTK2, Save20не меняется.
