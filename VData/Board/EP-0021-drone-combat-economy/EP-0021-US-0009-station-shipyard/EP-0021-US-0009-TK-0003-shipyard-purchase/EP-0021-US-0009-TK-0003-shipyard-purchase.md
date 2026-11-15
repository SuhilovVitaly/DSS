---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0009-station-shipyard
ticket: EP-0021-US-0009-TK-0003-shipyard-purchase
title: Атомарная покупка в корпус
stage: approved
layer: engine
depends_on: [EP-0021-US-0009-TK-0001-shipyard-content, EP-0021-US-0009-TK-0002-carrier-action-contract]
files_touched: 5
serves: [AC-01, AC-02]
created: 2026-11-09T11:46:33Z
revision: 1
---

# Атомарная покупка в корпус

## Why

Часть [Покупка и установка модулей носителя на верфи](../EP-0021-US-0009-station-shipyard.md). Приёмка этой части покрывает AC-01, AC-02 на поверхности указанного слоя.

## Decisions

Применить [D-01/D-02 эпика](../../Documentation.md#требования-пользователя-и-рабочие-решения) в пределах истории. Пользователь поручил подготовить все тикеты одним набором без промежуточных подтверждений. `approved` здесь означает автоматический статус карты, а продуктовый просмотр отложен пользователем.

## Assumptions

Применить соответствующие A-01…A-04 и предварительные числа эпика. Проверять свой шаг на контролируемой фикстуре до сквозного прогона US-0007.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Shipyard.cs` | Новый файл в рамках тикета. | Добавить только локальный тип или данные, описанные в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | LoadScenarioCore, BuildInstalledModuleProjection, BuildRuntimeModules, ApplyPendingCommands и TryStartNavigationCommand. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TradeExecution.cs` | Подготовка транзакции и WithCargoDelta, строки 37–110, 195–276, 313–329. | Изменить только интеграцию, указанную в Implementation steps. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TradeQuotes.cs` | ComputeQuoteTerms и ResolveTargetStock, строки 151–253. | Изменить только интеграцию, указанную в Implementation steps. |
| `tests/DeepSpaceSaga.Engine.Tests/ShipyardPurchaseTests.cs` | Новый файл в рамках тикета. | Именованный класс ShipyardPurchaseTests и его данные. |

## Public API after the change

Использовать контракты из зависимых тикетов. Новые локальные типы, проекции или действия ограничены Implementation steps.

## Implementation steps

Направить CarrierAction из очереди до проверки moduleId. Quote привязывает актуальную цену существующей кривой для 1 единицы, visit, revision и клетки. Confirm проверяет доступность, баланс и footprint, подготавливает замену корабля/станции, cash, market revision и receipt до commit. Cargo не предусловие. Валидировать ship SKU→type и shape перед выставлением оффера и при commit; контент-тест проверяет соответствия до поставки.

## Out of scope

Соседние слои, продуктовые функции вне критериев и правки за пределами Code context. Для соседней интеграции использовать именованные зависимости.

## Invariants

- `SimulationEngine.TradeExecution.cs:195–276` адресует обычную покупку модулю, а `SimulationEngine.cs:1326–1390` строит установленные модули из сценария. `SimulationEngine.cs:2430–2480` связывает стыковку с навигационным путём.
- Верфь продаёт грузовой модуль, мостик, ангар и жилой модуль за Tokens. Первый модуль устанавливается без контейнера.
- Отказы и повторы проверяются в точках изменения имущества, рынка, визита и результата, применимых к этому тикету.

## Tests

Проект проверок `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Имена ниже задают будущие методы. У каждого процесса тайм-аут 300000 ms с остановкой всего дерева. Длительные серии запускать по одному точному имени метода.

- `ShipyardPurchaseTests.Ac01` покрывает **AC-01**. Верфь продаёт грузовой модуль, мостик, ангар и жилой модуль за Tokens. Первый модуль устанавливается без контейнера.
- `ShipyardPurchaseTests.Ac02` покрывает **AC-02**. Установка из конечного запаса проверяет визит, платёж и корпус. Повтор безопасен. Ангар вмещает одного дрона.

Из `D:/DeepSpaceSaga/DSS` в PowerShell:

```powershell
$carrierRun = "D:/DeepSpaceSaga/artifacts/validation/ep-0021/EP-0021-US-0009-TK-0003-shipyard-purchase-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $carrierRun -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --configuration Release --filter "FullyQualifiedName~.ShipyardPurchaseTests." --results-directory "$carrierRun" --logger "trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --configuration Release
git diff --check
```

## Definition of Done

- Изменения ограничены Code context. Каждое действие даёт наблюдаемый результат на своём слое.
- Каждый указанный критерий покрыт именованной проверкой, а результат focused test и build записан с путём артефакта. Исходное падение отделено от новой регрессии.
- Инварианты и совместимость входов проверены. Сверены зависимости, diff и отсутствие лишних файлов.

## Self-containment check

Эпик содержит эталон, цены, маршрут и правила последствий. Здесь заданы границы слоя, входы, ожидаемый результат, зависимости и точка проверки. Расширение allowlist оформляется в Board, если текущая поверхность окажется недостаточной.
