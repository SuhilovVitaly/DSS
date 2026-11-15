---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0002-evasion-resolution
ticket: EP-0020-US-0002-TK-0001-evasion-contracts
title: Контракты уклонения
stage: done
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0020-US-0001-TK-0002-consciousness-state]
files_touched: 3
serves: [US2-AC1, US2-AC6]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Контракты уклонения

## Why

Клиент и движок обмениваются выбором уклонения, его отказом, причиной пропуска шага и прогнозом шанса.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Новые значения перечислений добавляются в конец, чтобы старые числовые id не менялись (`TacticalBattleContractTests.cs:58`).

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs` | :13 `TacticalFireTarget`, :17-24 причины отказа | `bool Evade = false` последним параметром; причина `EvasionUnavailable` в конец. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :17-21 `TacticalFireSkipReason`, :46 `TacticalIncomingShotSnapshot`, :48-53 `TacticalShotSnapshot`, :54-56 правила | `Evading` в конец перечисления; `double? ChanceIfEvading {init;}`; `bool TargetEvading {init;}`; `TacticalEvasionRulesSnapshot(double HitPenalty, int Cost, int MinConsciousness, int RecoveryPerTurn, int MaxConsciousness)` и `TacticalForecastRules.Evasion {init;}`. |
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

- `TacticalBattleContractTests.Evade_order_and_skip_reason_round_trip`
- `TacticalBattleContractTests.OldPayloadsWithoutEvasionFieldsRemainReadable`
- `TacticalBattleContractTests.OldTacticalEventsAndEnumIdsRemainReadable`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us2-tk1" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --include src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC1, US2-AC6 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все типы перечислены с номерами строк.

## Исполнение

2026-11-08: добавлены Evade, EvasionUnavailable=23, Evading=8, прогноз и метка фактического выстрела, nullable Evasion rules. 25/25 TacticalBattleContractTests PASS, build и scoped format PASS; git diff --check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us2-tk1/tests.trx. Self-review: старые ID сохранены, defaults читаются, RNG/Save20 не затронуты. US2-AC1/AC6: транспортные поля готовы; production resolution следует в TK-0003/0004.
