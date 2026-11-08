---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0006-decoy-resolution
ticket: EP-0020-US-0006-TK-0001-decoy-contracts
title: Контракты ловушки
stage: done
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0020-US-0005-TK-0002-decoy-state, EP-0020-US-0002-TK-0001-evasion-contracts]
files_touched: 3
serves: [US6-AC1, US6-AC5]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Контракты ловушки

## Why

Клиент и движок обмениваются пуском ловушки, его отказом, прогнозом шанса и раскрытием поглощённого выстрела.

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Новые значения перечислений добавляются в конец после значений M1, чтобы числовые id не менялись.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs` | :13 `TacticalFireTarget`, :17-24 причины отказа | `bool Decoy = false` последним параметром; причина `DecoyUnavailable` в конец. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :46 `TacticalIncomingShotSnapshot`, :48-53 `TacticalShotSnapshot`, :54-56 правила, :79-85 `TacticalBattleSnapshot` | `double? ChanceIfDecoy {init;}`; `bool Decoyed {init;}`, `double? ChanceWithoutDecoy {init;}`; `TacticalDecoyRulesSnapshot(double AiThreatChance)` и `TacticalForecastRules.Decoy {init;}`; `ImmutableArray<string> DecoysLaunched {init;}` (по умолчанию пустой). |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | :46-92, :192 | Сериализация новых полей, чтение старых нагрузок, id перечислений. |

## Public API after the change

Как в столбце Allowed change.

## Implementation steps

1. Добавить поля и значения.
2. Тесты круговой сериализации и совместимости.

## Out of scope

Логика движка.

## Invariants

Старые нагрузки и числовые id читаются как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Contracts.Tests`. Именованные проверки:

- `TacticalBattleContractTests.Decoy_order_and_shot_fields_round_trip`
- `TacticalBattleContractTests.OldPayloadsWithoutDecoyFieldsRemainReadable`
- `TacticalBattleContractTests.OldTacticalEventsAndEnumIdsRemainReadable`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us6-tk1" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --include src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC1, US6-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все типы перечислены с номерами строк.

## Исполнение

Выполнено: приказ Decoy(default false), DecoyUnavailable24, nullable forecast/shot/rules поля и пустой DecoysLaunched. Contracts27/27PASS; build/scoped-format/diff-checkPASS. Удалённые из реального JSON новые поля читаются с прежними defaults; новые значения round-trip без потери. Старые enum IDs сохранены. Самопроверка совместимости выполнена, US6-AC1 контрактная часть покрыта. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us6-tk1/contracts/tests.trx, format-contracts.log, format-tests.log. Engine/Client consumption следует зависимыми тикетами.
