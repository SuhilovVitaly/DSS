---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0003-outfit-owned-wing
ticket: EP-0021-US-0003-TK-0001-wing-roster
title: Реестр дронов и вместимость ангаров
stage: approved
layer: engine
depends_on: [EP-0021-US-0001-TK-0002-equipment-binding, EP-0021-US-0009-TK-0003-shipyard-purchase]
files_touched: 5
serves: [AC-01]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Реестр дронов и вместимость ангаров

## Why

Часть [Размещение и оснащение собственного крыла](../EP-0021-US-0003-outfit-owned-wing.md). Приёмка этой части покрывает AC-01 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/CarrierWingState.cs` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CarrierWing.cs` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | LoadScenarioCore, BuildInstalledModuleProjection, BuildRuntimeModules, ApplyPendingCommands и TryStartNavigationCommand. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TradeExecution.cs` | Подготовка транзакции и WithCargoDelta, строки 37–110, 195–276, 313–329. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/CarrierHangarOccupancyTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierHangarOccupancyTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Команды DeployDrone/PackDrone переводят одну товарную единицу между грузом и установленным ангаром, закрепляют уникальный DroneId, стоимость, здоровье и массу. Упаковывать только исправный пустой дрон без назначенного пилота. Переиспользовать расчёт грузовой себестоимости, отказ при переполнении и запись идентичности. Снимок проецирует установленные и свободные места, массу и актуальную WingRevision. Полная масса эталонного груза 3 170 кг; после развёртывания она числится за крылом, а не в cargo.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleAssembly.cs:35–59` использует реальные компоненты и массу. `SimulationEngine.TradeExecution.cs:313–329` определяет сохранение basis груза.
- У каждого дрона уникальный ангар и у каждого назначенного пилота свободное место в исправном жилом модуле. Пятая машина при четырёх ангарах не помещается.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierHangarOccupancyTests.Ac01` покрывает **AC-01**. У каждого дрона уникальный ангар и у каждого назначенного пилота свободное место в исправном жилом модуле. Пятая машина при четырёх ангарах не помещается.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0003-TK-0001-wing-roster-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierHangarOccupancyTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
