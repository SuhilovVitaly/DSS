---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0009-station-shipyard
ticket: EP-0021-US-0009-TK-0004-shipyard-projection
title: Снимок верфи и носителя
stage: approved
layer: engine
depends_on: [EP-0021-US-0009-TK-0003-shipyard-purchase]
files_touched: 3
serves: [AC-02, AC-03]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Снимок верфи и носителя

## Why

Часть [Покупка и установка модулей носителя на верфи](../EP-0021-US-0009-station-shipyard.md). Приёмка этой части покрывает AC-02, AC-03 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | LoadScenarioCore, BuildInstalledModuleProjection, BuildRuntimeModules, ApplyPendingCommands и TryStartNavigationCommand. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Shipyard.cs` | Поверхность, добавленная зависимостью EP-0021-US-0009-TK-0003-shipyard-purchase. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/CarrierOperationsProjectionTests.cs` | Новый файл в рамках тикета. | Именованный класс CarrierOperationsProjectionTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Опубликовать из Engine docked offers, количества, revisions, geometry и причины недоступности. Wing/Pilots/StarterRoute добавляют их истории. Без стыковки живых цен других станций нет.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.TradeExecution.cs:195–276` адресует обычную покупку модулю, а `SimulationEngine.cs:1326–1390` строит установленные модули из сценария. `SimulationEngine.cs:2430–2480` связывает стыковку с навигационным путём.
- Установка из конечного запаса проверяет визит, платёж и корпус. Повтор безопасен. Ангар вмещает одного дрона.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `CarrierOperationsProjectionTests.Ac02` покрывает **AC-02**. Установка из конечного запаса проверяет визит, платёж и корпус. Повтор безопасен. Ангар вмещает одного дрона.
- `CarrierOperationsProjectionTests.Ac03` покрывает **AC-03**. Экран показывает ассортимент, цену, места, причины отказа и обновлённый состав.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0009-TK-0004-shipyard-projection-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.CarrierOperationsProjectionTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
