---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0001-consciousness
ticket: EP-0020-US-0001-TK-0002-consciousness-state
title: Сознание в состоянии боя и снимках
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0001-TK-0001-rules-content]
files_touched: 7
serves: [US1-AC2, US1-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Сознание в состоянии боя и снимках

## Why

Сознание — состояние пилота на время боя. Его нужно завести, восстанавливать каждый ход и показывать клиенту.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-06. Восстановление происходит в начале хода (в `Planning` при `Turn > 1`), до движения и стрельбы этого хода. Сознание есть у дронов обеих сторон.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs` | :36-66 `TacticalDrone` | `public int Consciousness { get; internal set; }`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs` | :83-89 материализация инициативы | После инициативы: `drone.Consciousness = catalog.Rules.Consciousness.Start` для всех дронов. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs` | :14-23 `Planning` | После `Turn++`: при `Turn > 1` для каждого активного дрона `Consciousness = Math.Min(Max, Consciousness + RecoveryPerTurn)`. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :35-41 `TacticalUnitSnapshot { int? Initiative {init;} }` | `public int? Consciousness { get; init; }` по образцу `Initiative`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | :28-33 `Units` | Заполнять `Consciousness` для дронов. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConsciousnessTests.cs` | нет | Новые тесты. |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | :73-92 совместимость `Initiative` | Такой же тест для `Consciousness`. |

## Public API after the change

`TacticalDrone.Consciousness`; `TacticalUnitSnapshot.Consciousness`.

## Implementation steps

1. Добавить поле и материализацию после инициативы (без RNG).
2. Восстановление в `Planning` при `Turn > 1`; выбывшие дроны не восстанавливаются.
3. Снимок и контрактный тест старой нагрузки.
4. Тесты: старт 100 у всех дронов; искусственно сниженное до 80 сознание через ход = 85, с 98 — 100; сессия и `Run` дают одинаковые значения на seed 1; число вызовов RNG до и после правки одинаково.

## Out of scope

Расход сознания.

## Invariants

`RandomCalls` и подписи серий `SeriesParityTests` не меняются от этого тикета.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `ConsciousnessTests.Every_drone_starts_battle_with_rules_start`
- `ConsciousnessTests.Recovery_adds_per_turn_up_to_max`
- `ConsciousnessTests.Session_and_direct_paths_agree_and_draw_no_random`
- `TacticalBattleContractTests.OldUnitPayloadWithoutConsciousnessRemainsReadable`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~ConsciousnessTests|FullyQualifiedName~SeriesParityTests|FullyQualifiedName~SessionTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us1-tk2" --logger "trx;LogFileName=engine.trx"
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us1-tk2" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --include src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC2, US1-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Места вставки и образец (`Initiative`) указаны.

## Исполнение

Не начато.
