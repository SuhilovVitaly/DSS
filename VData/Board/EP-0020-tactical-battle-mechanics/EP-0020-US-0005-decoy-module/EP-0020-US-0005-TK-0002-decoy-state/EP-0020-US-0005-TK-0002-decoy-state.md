---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0005-decoy-module
ticket: EP-0020-US-0005-TK-0002-decoy-state
title: Модуль и заряды в состоянии боя, масса, снимки
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0005-TK-0001-decoy-content, EP-0020-US-0002-TK-0005-evasion-regression]
files_touched: 9
serves: [US5-AC3, US5-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Модуль и заряды в состоянии боя, масса, снимки

## Why

Дрон должен нести модуль и счётчик зарядов, а модуль — весить, как решил пользователь (D-15).

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-15: масса модуля прибавляется к массе дрона так же, как масса щита; это меняет пределы скорости и поворота, а значит предложения ИИ и исходы боёв. Порядок вызовов RNG при сборке не меняется.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs` | :36-66 `TacticalDrone`, `Limits` | Параметр `BattleDecoy? decoy`; `public int DecoyCharges { get; internal set; }`; `moduleMass: Decoy?.MassKg ?? 0` в `Limits`. |
| `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalLimits.cs` | :9-10 `Of` | `double moduleMass = 0` последним параметром; прибавить к массе дрона. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs` | :48-58 сборка дрона | Найти модуль компоновки; `DecoyCharges = Charges`; масса в `TacticalLimits.Of`. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :33 `TacticalBuildSnapshot`, :35-41 `TacticalUnitSnapshot` | `string? DecoyId {init;}`, `string? DecoyName {init;}`; `int? DecoyCharges {init;}`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | :25 сборка, :28-33 `Units` | Заполнять новые поля для дронов. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DecoyStateTests.cs` | нет | Новые тесты. |
| `tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/LimitsAndChanceTests.cs` | пределы движения | Масса модуля в пределах. |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | :73-92 совместимость | Чтение старых нагрузок без новых полей. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs, tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs, tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SessionTests.cs` | исторические ожидания | Обновить числа, изменённые массой модуля, с записью причины. |

## Public API after the change

`TacticalDrone.Decoy`, `TacticalDrone.DecoyCharges`; `TacticalLimits.Of(..., double moduleMass = 0)`; поля снимков US5-AC4.

## Implementation steps

1. Добавить модуль и заряды в дрон и сборку.
2. Добавить `moduleMass` в `TacticalLimits.Of` и передавать его везде, где строятся пределы дрона.
3. Снимки и контрактный тест старой нагрузки.
4. Тесты: 2 заряда у каждого дрона seed 1, у носителя `null`; пределы с модулем равны пределам с массой щита +10 кг; число вызовов RNG сборки прежнее.
5. Прогнать весь `DeepSpaceSaga.Engine.Tests`; обновить только ожидания, изменённые массой модуля; список изменённых тестов и причину записать в «Исполнение»; сохранить подписи серий до и после в artifacts.

## Out of scope

Пуск ловушек.

## Invariants

Без модуля (`module: null`) пределы и исходы побитно прежние.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `DecoyStateTests.Every_drone_starts_with_module_charges`
- `DecoyStateTests.Carrier_has_no_decoy`
- `DecoyStateTests.Assembly_draws_same_random_calls`
- `LimitsAndChanceTests.Module_mass_enters_limits_like_shield_mass`
- `TacticalBattleContractTests.OldPayloadsWithoutDecoyFieldsRemainReadable`
- `GoldenBattleTests`
- `SeriesParityTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us5-tk2" --logger "trx;LogFileName=engine.trx"
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~LimitsAndChanceTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us5-tk2" --logger "trx;LogFileName=motion.trx"
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us5-tk2" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore --include src/DeepSpaceSaga.Motion/TacticalBattle/TacticalLimits.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --include src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC3, US5-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Места вставки и образец (щит) указаны.

## Исполнение

Не начато.
