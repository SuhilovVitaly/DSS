---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0006-restore-wing
ticket: EP-0021-US-0006-TK-0001-station-services
title: Котировки ремонта и перезарядки
stage: approved
layer: engine
depends_on: [EP-0021-US-0005-TK-0002-settlement-engine, EP-0021-US-0009-TK-0003-shipyard-purchase, EP-0021-US-0011-TK-0002-carrier-world-continuity]
files_touched: 4
serves: [AC-01]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Котировки ремонта и перезарядки

## Why

Часть [Ремонт, заряды и замена после вылета](../EP-0021-US-0006-restore-wing.md). Приёмка этой части покрывает AC-01 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CarrierWing.cs` | Поверхность, добавленная зависимостью EP-0021-US-0005-TK-0002-settlement-engine. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Shipyard.cs` | Поверхность, добавленная зависимостью EP-0021-US-0009-TK-0003-shipyard-purchase. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/CarrierWingState.cs` | Поверхность из Grounding истории. `TacticalBattleSession.cs:118–147` фиксирует расход зарядов. Существующий мир использует реальные Tokens и квитанции (`SimulationEngine.TradeExecution.cs:94–110`). | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/WingRestorationTests.cs` | Новый файл в рамках тикета. | Именованный класс WingRestorationTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Добавить действия Quote/Confirm RepairDrone и ReloadDecoy через станционный механизм. Ремонт восстанавливает hull, armor и исправность, заряд покупается отдельно. Проектные формулы в эпике, округление вверх. Проверять visit, world/wing revision, повтор receipt, доступность станции и деньги.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `TacticalBattleSession.cs:118–147` фиксирует расход зарядов. Существующий мир использует реальные Tokens и квитанции (`SimulationEngine.TradeExecution.cs:94–110`).
- Станционная услуга восстанавливает hull, боеспособность и купленные заряды по авторитетной котировке. Недостаток средств не меняет дрона, повтор не списывает снова.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `WingRestorationTests.Ac01` покрывает **AC-01**. Станционная услуга восстанавливает hull, боеспособность и купленные заряды по авторитетной котировке. Недостаток средств не меняет дрона, повтор не списывает снова.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0006-TK-0001-station-services-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.WingRestorationTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
